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

    [Header("Trigger")]
    [Tooltip("一度だけ再生したいならON")]
    public bool playOnce = true;
    bool played = false;

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

    [Header("Dialogue Lines (Player/Mawaru13 starts)")]
    public Line[] lines;

    [Header("Input (PlayerInput)")]
    public PlayerInput inputSource;
    public string uiActionMapName = "UI";
    public string advanceActionName = "South";
    public string fallbackAdvanceName = "Submit";

    [Header("Lock Targets (disable these scripts during dialogue)")]
    public MonoBehaviour[] lockBehaviours;

    [Header("Freeze Bodies (stay still)")]
    public Rigidbody2D[] freezeBodies;

    int index = 0;
    bool running = false;

    string prevActionMapName = null;
    bool prevInputEnabled = true;
    InputAction advanceAction = null;

    // BGM restore
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

        if (other.GetComponentInParent<PlayerController>() != null ||
            other.GetComponentInParent<MawaruController>() != null)
        {
            StartCutscene();
        }
    }

    public void StartCutscene()
    {
        if (running) return;

        if (lines == null || lines.Length == 0)
        {
            Debug.LogWarning("[Dialogue] lines が空です");
            return;
        }
        if (!dialogueRoot || !dialogueText || !portraitImage)
        {
            Debug.LogWarning("[Dialogue] UI参照が足りません（dialogueRoot/dialogueText/portraitImage）");
            return;
        }
        if (!inputSource)
        {
            Debug.LogWarning("[Dialogue] inputSource(PlayerInput) が未設定です");
            return;
        }

        played = true;
        running = true;
        index = 0;

        // 会話BGMへ切り替え（指定がある時だけ）
        if (bgmSource && dialogueBgm)
        {
            prevBgmClip = bgmSource.clip;
            prevBgmTime = bgmSource.time;
            prevBgmLoop = bgmSource.loop;
            prevBgmWasPlaying = bgmSource.isPlaying;

            bgmSource.clip = dialogueBgm;
            bgmSource.loop = true;
            bgmSource.time = 0f;
            bgmSource.Play();
        }

        // 操作停止
        if (lockBehaviours != null)
        {
            foreach (var b in lockBehaviours)
                if (b) b.enabled = false;
        }

        // その場固定
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

        // 入力切替
        prevActionMapName = inputSource.currentActionMap != null ? inputSource.currentActionMap.name : null;
        prevInputEnabled = inputSource.enabled;

        if (!inputSource.enabled) inputSource.enabled = true;
        inputSource.ActivateInput();

        if (!string.IsNullOrEmpty(uiActionMapName))
        {
            try { inputSource.SwitchCurrentActionMap(uiActionMapName); } catch { }
        }

        advanceAction = FindAdvanceAction();
        if (advanceAction != null)
        {
            advanceAction.performed += OnAdvance;
            advanceAction.Enable();
        }
        else
        {
            Debug.LogWarning("[Dialogue] Advance action が見つかりません。UI/South か UI/Submit を確認してね。");
        }

        dialogueRoot.SetActive(true);
        ShowLine(index);
    }

    InputAction FindAdvanceAction()
    {
        var map = inputSource.actions.FindActionMap(uiActionMapName, throwIfNotFound: false);
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
        if (index >= lines.Length)
        {
            EndCutscene();
            return;
        }
        ShowLine(index);
    }

    void ShowLine(int i)
    {
        if (i < 0 || i >= lines.Length) return;

        var line = lines[i];

        dialogueText.text = line.text ?? "";
        portraitImage.sprite = line.portrait;
        portraitImage.enabled = (line.portrait != null);
        if (nameText) nameText.text = line.speakerName ?? "";

        // セリフ音声
        if (voiceSource != null)
        {
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
            advanceAction = null;
        }

        if (dialogueRoot) dialogueRoot.SetActive(false);
        if (voiceSource) voiceSource.Stop();

        if (inputSource && !string.IsNullOrEmpty(prevActionMapName))
        {
            try { inputSource.SwitchCurrentActionMap(prevActionMapName); } catch { }
        }

        if (inputSource)
        {
            inputSource.enabled = prevInputEnabled;
            if (prevInputEnabled) inputSource.ActivateInput();
            else inputSource.DeactivateInput();
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
                if (b) b.enabled = true;
        }

        running = false;

        if (bossPanel) bossPanel.SetActive(true);

        // BGMを元に戻す（切り替えていた時だけ）
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
}