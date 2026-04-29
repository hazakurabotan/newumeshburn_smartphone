using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BossIntroDialogueCutscene : MonoBehaviour
{
    [Serializable]
    public class Line
    {
        public string speakerName;
        public Sprite portrait;

        [Header("Voice (optional)")]
        public AudioClip voice;
        [Range(0f, 1f)] public float voiceVolume = 1f;

        [TextArea(2, 4)] public string text;
    }

    public enum Starter
    {
        AutoByTrigger, // トリガーに入ったキャラで自動
        Player,
        Mawaru
    }

    [Header("Trigger")]
    [Tooltip("一度だけ再生したいならON")]
    public bool playOnce = true;
    bool played = false;

    [Header("Which dialogue to use")]
    [Tooltip("基本は AutoByTrigger 推奨")]
    public Starter starter = Starter.AutoByTrigger;

    [Tooltip("従来の共通会話（どちらでも同じでいい時）")]
    public Line[] linesDefault;

    [Tooltip("プレイヤー（めぐる等）を選んだ時の会話")]
    public Line[] linesForPlayer;

    [Tooltip("mawaru13 を選んだ時の会話")]
    public Line[] linesForMawaru;

    [Header("UI References")]
    public GameObject dialogueRoot;
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Boss UI (show after intro)")]
    public GameObject bossPanel;

    [Header("Audio (optional)")]
    [Tooltip("セリフ音声を鳴らすAudioSource（UI側でもOK）")]
    public AudioSource voiceSource;

    [Tooltip("会話開始時に切り替えるBGM（未設定ならBGM変更しない）")]
    public AudioClip dialogueBgm;

    [Tooltip("BGMを鳴らしているAudioSource（BGMPlayerなど）")]
    public AudioSource bgmSource;

    [Header("BGM Behavior")]
    [Tooltip("ON: 会話で切り替えたBGMを、会話終了後もそのまま流し続ける（あなたの希望）")]
    public bool keepDialogueBgmAfterCutscene = true;

    [Tooltip("ON: 会話開始時にBGMを dialogueBgm に切り替える（dialogueBgm がある時だけ）")]
    public bool switchBgmOnStart = true;

    [Header("Audio Safety")]
    [Tooltip("voiceSource が未設定なら自動でこのオブジェクトから拾う/追加する")]
    public bool autoCreateVoiceSourceIfMissing = true;

    [Tooltip("会話中だけ voiceSource を 2D(SpatialBlend=0) に固定する")]
    public bool forceVoice2D = true;

    [Tooltip("会話中だけ bgmSource を 2D(SpatialBlend=0) に固定する")]
    public bool forceBgm2D = true;

    [Header("Input Source (any PlayerInput in scene)")]
    [Tooltip("ここは『どれでもOK』。このスクリプトは PlayerInput を Switchしません。ActionMap を手動で Enable/Disable します。")]
    public PlayerInput inputSource;

    [Header("Action Map / Advance Action")]
    [Tooltip("会話用ActionMap名。あなたの project では Dialog を推奨。")]
    public string dialogActionMapName = "Dialog";
    [Tooltip("会話を進めるAction名。例：Next / Submit / South など")]
    public string advanceActionName = "Next";
    [Tooltip("advanceActionName が見つからない時の保険")]
    public string fallbackAdvanceName = "Submit";

    [Header("Lock Targets (disable these scripts during dialogue)")]
    public MonoBehaviour[] lockBehaviours;

    [Header("Freeze Bodies (stay still)")]
    public Rigidbody2D[] freezeBodies;

    int index = 0;
    bool running = false;

    Line[] activeLines;
    InputAction advanceAction = null;

    // ActionMap restore
    UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputActionMap> maps;
    bool[] mapWasEnabled;

    // BGM restore（keep=false の時だけ使う）
    AudioClip prevBgmClip;
    float prevBgmTime;
    bool prevBgmLoop;
    bool prevBgmWasPlaying;

    struct BodyBackup
    {
        public RigidbodyConstraints2D constraints;
        public float gravity;
        public bool simulated;
    }
    BodyBackup[] bodyBackups;

    Starter startedBy = Starter.AutoByTrigger;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Awake()
    {
        if (dialogueRoot) dialogueRoot.SetActive(false);
        if (bossPanel) bossPanel.SetActive(false);

        if (!bgmSource)
        {
            var bgmGo = GameObject.Find("BGMPlayer");
            if (bgmGo) bgmSource = bgmGo.GetComponent<AudioSource>();
        }
    }

    void OnDisable()
    {
        if (running) EndCutscene();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (running) return;
        if (playOnce && played) return;

        bool isPlayer = other.GetComponentInParent<PlayerController>() != null;
        bool isMawaru = other.GetComponentInParent<MawaruController>() != null;
        if (!isPlayer && !isMawaru) return;

        // 誰で開始したかを保存
        if (starter == Starter.AutoByTrigger)
            startedBy = isMawaru ? Starter.Mawaru : Starter.Player;
        else
            startedBy = starter;

        StartCutscene();
    }

    public void StartCutscene()
    {
        if (running) return;

        // InputSourceが未設定なら、シーン内のどれかを拾う
        if (!inputSource)
        {
            inputSource = FindObjectOfType<PlayerInput>();
            if (!inputSource)
            {
                Debug.LogWarning("[Dialogue] inputSource(PlayerInput) が見つかりません。シーンに PlayerInput が必要です。");
                return;
            }
        }

        // 会話配列の選択
        activeLines = PickLines();
        if (activeLines == null || activeLines.Length == 0)
        {
            Debug.LogWarning("[Dialogue] lines が空です（linesDefault / linesForPlayer / linesForMawaru を確認）");
            return;
        }

        if (!dialogueRoot || !dialogueText || !portraitImage)
        {
            Debug.LogWarning("[Dialogue] UI参照が足りません（dialogueRoot/dialogueText/portraitImage）");
            return;
        }

        // VoiceSource 保険
        EnsureVoiceSource();

        played = true;
        running = true;
        index = 0;

        // ---- BGM切替（会話BGMへ） ----
        if (switchBgmOnStart && bgmSource && dialogueBgm)
        {
            // keep=false の時だけ「元に戻す情報」を取る
            if (!keepDialogueBgmAfterCutscene)
            {
                prevBgmClip = bgmSource.clip;
                prevBgmTime = bgmSource.time;
                prevBgmLoop = bgmSource.loop;
                prevBgmWasPlaying = bgmSource.isPlaying;
            }

            if (forceBgm2D) bgmSource.spatialBlend = 0f;

            bgmSource.clip = dialogueBgm;
            bgmSource.loop = true;
            bgmSource.time = 0f;
            bgmSource.Play();
        }

        // 操作停止（スクリプト）
        if (lockBehaviours != null)
        {
            foreach (var b in lockBehaviours)
                if (b) b.enabled = false;
        }

        // その場固定（Rigidbody）
        if (freezeBodies != null)
        {
            bodyBackups = new BodyBackup[freezeBodies.Length];
            for (int i = 0; i < freezeBodies.Length; i++)
            {
                var rb = freezeBodies[i];
                if (!rb) continue;

                bodyBackups[i] = new BodyBackup
                {
                    constraints = rb.constraints,
                    gravity = rb.gravityScale,
                    simulated = rb.simulated
                };

                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
                rb.simulated = true;
            }
        }

        // ★重要：PlayerInputの ActivateInput / SwitchCurrentActionMap は使わない
        // 代わりに ActionMap の Enable/Disable を手動で行う（CharacterSwitchで無効でも落ちない）
        BackupAndEnableDialogMap();

        // 進めるActionを購読
        advanceAction = FindAdvanceAction();
        if (advanceAction != null)
        {
            advanceAction.performed += OnAdvance;
            advanceAction.Enable();
        }
        else
        {
            Debug.LogWarning("[Dialogue] Advance action が見つかりません。Dialog/Next か Dialog/Submit を確認してね。");
        }

        dialogueRoot.SetActive(true);
        ShowLine(index);
    }

    void EnsureVoiceSource()
    {
        if (!voiceSource && autoCreateVoiceSourceIfMissing)
        {
            voiceSource = GetComponent<AudioSource>();
            if (!voiceSource) voiceSource = gameObject.AddComponent<AudioSource>();
        }

        if (voiceSource)
        {
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            if (forceVoice2D) voiceSource.spatialBlend = 0f; // 3Dで距離減衰して聞こえない事故を潰す
        }
    }

    Line[] PickLines()
    {
        if (startedBy == Starter.Player && linesForPlayer != null && linesForPlayer.Length > 0) return linesForPlayer;
        if (startedBy == Starter.Mawaru && linesForMawaru != null && linesForMawaru.Length > 0) return linesForMawaru;

        if (linesDefault != null && linesDefault.Length > 0) return linesDefault;

        if (linesForPlayer != null && linesForPlayer.Length > 0) return linesForPlayer;
        if (linesForMawaru != null && linesForMawaru.Length > 0) return linesForMawaru;

        return null;
    }

    void BackupAndEnableDialogMap()
    {
        if (inputSource == null || inputSource.actions == null) return;

        maps = inputSource.actions.actionMaps;
        mapWasEnabled = new bool[maps.Count];

        for (int i = 0; i < maps.Count; i++)
        {
            mapWasEnabled[i] = maps[i].enabled;
            maps[i].Disable();
        }

        var dialogMap = inputSource.actions.FindActionMap(dialogActionMapName, throwIfNotFound: false);
        if (dialogMap != null) dialogMap.Enable();
        else Debug.LogWarning($"[Dialogue] ActionMap '{dialogActionMapName}' が見つかりません。InputActionsを確認してね。");
    }

    InputAction FindAdvanceAction()
    {
        if (inputSource == null || inputSource.actions == null) return null;

        var map = inputSource.actions.FindActionMap(dialogActionMapName, throwIfNotFound: false);
        if (map != null)
        {
            var a = map.FindAction(advanceActionName, throwIfNotFound: false);
            if (a != null) return a;

            var fb = map.FindAction(fallbackAdvanceName, throwIfNotFound: false);
            if (fb != null) return fb;
        }

        // 保険：全体から探す
        var any = inputSource.actions.FindAction(advanceActionName, throwIfNotFound: false);
        if (any != null) return any;

        return inputSource.actions.FindAction(fallbackAdvanceName, throwIfNotFound: false);
    }

    void OnAdvance(InputAction.CallbackContext ctx)
    {
        if (!running) return;
        Next();
    }

    void Next()
    {
        index++;
        if (index >= activeLines.Length)
        {
            EndCutscene();
            return;
        }
        ShowLine(index);
    }

    void ShowLine(int i)
    {
        if (activeLines == null) return;
        if (i < 0 || i >= activeLines.Length) return;

        var line = activeLines[i];

        dialogueText.text = line.text ?? "";
        portraitImage.sprite = line.portrait;
        portraitImage.enabled = (line.portrait != null);
        if (nameText) nameText.text = line.speakerName ?? "";

        // セリフ音声
        if (voiceSource != null)
        {
            if (forceVoice2D) voiceSource.spatialBlend = 0f;

            voiceSource.Stop();
            if (line.voice != null)
                voiceSource.PlayOneShot(line.voice, Mathf.Clamp01(line.voiceVolume));
        }
    }

    void EndCutscene()
    {
        if (advanceAction != null)
        {
            advanceAction.performed -= OnAdvance;
            advanceAction.Disable();
            advanceAction = null;
        }

        if (dialogueRoot) dialogueRoot.SetActive(false);
        if (voiceSource) voiceSource.Stop();

        // ActionMap restore
        if (maps.Count > 0 && mapWasEnabled != null)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (mapWasEnabled[i]) maps[i].Enable();
                else maps[i].Disable();
            }
        }

        // Rigidbody restore
        if (freezeBodies != null && bodyBackups != null)
        {
            for (int i = 0; i < freezeBodies.Length; i++)
            {
                var rb = freezeBodies[i];
                if (!rb) continue;

                rb.constraints = bodyBackups[i].constraints;
                rb.gravityScale = bodyBackups[i].gravity;
                rb.simulated = bodyBackups[i].simulated;
            }
        }

        // Script restore
        if (lockBehaviours != null)
        {
            foreach (var b in lockBehaviours)
                if (b) b.enabled = true;
        }

        running = false;

        if (bossPanel) bossPanel.SetActive(true);

        // ★ここが今回の要点：
        // keepDialogueBgmAfterCutscene = true の時は「元BGMに戻さない」
        if (!keepDialogueBgmAfterCutscene)
        {
            if (bgmSource && dialogueBgm)
            {
                bgmSource.Stop();
                bgmSource.clip = prevBgmClip;
                bgmSource.loop = prevBgmLoop;

                if (prevBgmClip != null)
                {
                    float len = prevBgmClip.length;
                    bgmSource.time = (len > 0f) ? Mathf.Clamp(prevBgmTime, 0f, len - 0.01f) : 0f;
                    if (prevBgmWasPlaying) bgmSource.Play();
                }
            }
        }
        // keep=true の場合：何もしない（会話で切り替えたBGMがそのまま続く）
    }
}