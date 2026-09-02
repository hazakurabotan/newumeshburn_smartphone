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

        [Header("Voice optional")]
        public AudioClip voice;
        [Range(0f, 1f)] public float voiceVolume = 1f;

        [TextArea(2, 4)] public string text;
    }

    public enum Starter
    {
        AutoByTrigger,
        Player,
        Mawaru
    }

    [Header("Trigger")]
    public bool playOnce = true;

    [Tooltip("ONならTriggerに入った時に自動で会話開始。撃破後会話用に使う場合はOFFにする")]
    public bool allowTriggerStart = true;

    bool played = false;

    [Header("Which dialogue to use")]
    public Starter starter = Starter.AutoByTrigger;

    public Line[] linesDefault;
    public Line[] linesForPlayer;
    public Line[] linesForMawaru;

    [Header("UI References")]
    public GameObject dialogueRoot;
    public Image portraitImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Boss UI")]
    public GameObject bossPanel;

    [Tooltip("会話開始時にBossPanelを隠す")]
    public bool hideBossPanelWhenCutsceneStarts = true;

    [Tooltip("会話終了時にBossPanelを表示する。撃破後会話ではOFF推奨")]
    public bool showBossPanelWhenCutsceneEnds = true;

    [Header("Audio optional")]
    public AudioSource voiceSource;
    public AudioClip dialogueBgm;
    public AudioSource bgmSource;

    [Header("BGM Behavior")]
    public bool keepDialogueBgmAfterCutscene = true;
    public bool switchBgmOnStart = true;

    [Header("Audio Safety")]
    public bool autoCreateVoiceSourceIfMissing = true;
    public bool forceVoice2D = true;
    public bool forceBgm2D = true;

    [Header("Input Source")]
    public PlayerInput inputSource;

    [Header("Action Map / Advance Action")]
    public string dialogActionMapName = "Dialog";
    public string advanceActionName = "Next";
    public string fallbackAdvanceName = "Submit";

    [Header("Lock Targets")]
    public MonoBehaviour[] lockBehaviours;

    [Header("Freeze Bodies")]
    public Rigidbody2D[] freezeBodies;

    public event Action CutsceneFinished;

    int index = 0;
    bool running = false;

    Line[] activeLines;
    InputAction advanceAction = null;

    UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputActionMap> maps;
    bool[] mapWasEnabled;

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

    public bool IsRunning => running;
    public bool HasPlayed => played;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void Awake()
    {
        if (dialogueRoot) dialogueRoot.SetActive(false);

        if (bossPanel && hideBossPanelWhenCutsceneStarts)
            bossPanel.SetActive(false);

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
        if (!allowTriggerStart) return;
        if (running) return;
        if (playOnce && played) return;

        bool isPlayer = other.GetComponentInParent<PlayerController>() != null;
        bool isMawaru = other.GetComponentInParent<MawaruController>() != null;
        if (!isPlayer && !isMawaru) return;

        if (starter == Starter.AutoByTrigger)
            startedBy = isMawaru ? Starter.Mawaru : Starter.Player;
        else
            startedBy = starter;

        StartCutscene();
    }

    public void StartCutsceneFrom(Starter forcedStarter)
    {
        if (forcedStarter == Starter.Player || forcedStarter == Starter.Mawaru)
        {
            startedBy = forcedStarter;
        }
        else
        {
            startedBy = starter;
        }

        StartCutscene();
    }

    public void StartCutscene()
    {
        if (running) return;

        if (!inputSource)
        {
            inputSource = FindObjectOfType<PlayerInput>();
            if (!inputSource)
            {
                Debug.LogWarning("[Dialogue] inputSource(PlayerInput) が見つかりません。");
                return;
            }
        }

        activeLines = PickLines();
        if (activeLines == null || activeLines.Length == 0)
        {
            Debug.LogWarning("[Dialogue] lines が空です。linesDefault / linesForPlayer / linesForMawaru を確認してください。");
            return;
        }

        if (!dialogueRoot || !dialogueText || !portraitImage)
        {
            Debug.LogWarning("[Dialogue] UI参照が足りません。dialogueRoot / dialogueText / portraitImage を確認してください。");
            return;
        }

        EnsureVoiceSource();

        played = true;
        running = true;
        index = 0;

        if (bossPanel && hideBossPanelWhenCutsceneStarts)
            bossPanel.SetActive(false);

        if (switchBgmOnStart && bgmSource && dialogueBgm)
        {
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

        if (lockBehaviours != null)
        {
            foreach (var b in lockBehaviours)
            {
                if (b) b.enabled = false;
            }
        }

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

        BackupAndEnableDialogMap();

        advanceAction = FindAdvanceAction();
        if (advanceAction != null)
        {
            advanceAction.performed += OnAdvance;
            advanceAction.Enable();
        }
        else
        {
            Debug.LogWarning("[Dialogue] Advance action が見つかりません。Dialog/Next または Dialog/Submit を確認してください。");
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
            if (forceVoice2D) voiceSource.spatialBlend = 0f;
        }
    }

    Line[] PickLines()
    {
        if (startedBy == Starter.Player && linesForPlayer != null && linesForPlayer.Length > 0)
            return linesForPlayer;

        if (startedBy == Starter.Mawaru && linesForMawaru != null && linesForMawaru.Length > 0)
            return linesForMawaru;

        if (linesDefault != null && linesDefault.Length > 0)
            return linesDefault;

        if (linesForPlayer != null && linesForPlayer.Length > 0)
            return linesForPlayer;

        if (linesForMawaru != null && linesForMawaru.Length > 0)
            return linesForMawaru;

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
        if (dialogMap != null)
        {
            dialogMap.Enable();
        }
        else
        {
            Debug.LogWarning("[Dialogue] ActionMap '" + dialogActionMapName + "' が見つかりません。");
        }
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
        portraitImage.enabled = line.portrait != null;

        if (nameText)
            nameText.text = line.speakerName ?? "";

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

        if (mapWasEnabled != null && maps.Count > 0)
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (mapWasEnabled[i]) maps[i].Enable();
                else maps[i].Disable();
            }
        }

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

        if (lockBehaviours != null)
        {
            foreach (var b in lockBehaviours)
            {
                if (b) b.enabled = true;
            }
        }

        running = false;

        if (bossPanel && showBossPanelWhenCutsceneEnds)
            bossPanel.SetActive(true);

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
                    bgmSource.time = len > 0f ? Mathf.Clamp(prevBgmTime, 0f, len - 0.01f) : 0f;

                    if (prevBgmWasPlaying)
                        bgmSource.Play();
                }
            }
        }

        CutsceneFinished?.Invoke();
    }
}