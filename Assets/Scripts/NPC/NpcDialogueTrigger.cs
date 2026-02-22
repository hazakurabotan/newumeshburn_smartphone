using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class NpcDialogueTrigger : MonoBehaviour
{
    [Serializable]
    public class Line
    {
        public string speakerName;
        public Sprite portrait;
        [TextArea(2, 4)] public string text;
    }

    [Header("NPC Dialogue Lines (repeatable)")]
    public Line[] lines;

    [Header("UI References")]
    public GameObject dialogueRoot;      // 会話パネルの親
    public Image portraitImage;          // 顔アイコン
    public TextMeshProUGUI nameText;     // 名前（不要なら null でもOK）
    public TextMeshProUGUI dialogueText; // セリフ本文

    [Header("Input / ActionMaps")]
    public string dialogMapName = "Dialog";        // Dialog ActionMap
    public string dialogNextActionName = "Next";   // Dialog/Next (South)
    public string interactActionName = "Interact"; // Player/Mawaru/Interact (Hold South)

    [Header("Lock while talking (optional)")]
    public bool disableMovementScripts = true;     // 会話中に操作停止したい
    public bool freezeRigidbody2D = true;          // 会話中にその場で固定したい

    // ---- runtime ----
    bool inRange = false;
    bool talking = false;
    int index = 0;

    PlayerInput activeInput;             // いま会話している側（Player or Mawaru）
    MonoBehaviour[] cachedLocks;          // 止めたいスクリプト
    Rigidbody2D[] cachedBodies;           // 固定したいRB

    string prevMapName;
    InputAction nextAction;              // Dialog/Next

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
        if (col) col.isTrigger = true;
    }

    void Awake()
    {
        if (dialogueRoot) dialogueRoot.SetActive(false);
    }

    void Update()
    {
        if (!inRange || talking) return;
        if (lines == null || lines.Length == 0) return;

        // 近くにいるキャラの PlayerInput を見て「Interact(Hold)」が押されたら開始
        // ※activeInput は OnTriggerStay/Enter で候補を掴む
        if (activeInput == null) return;

        var map = activeInput.currentActionMap;
        if (map == null) return;

        var interact = map.FindAction(interactActionName, throwIfNotFound: false);
        if (interact == null) return;

        if (interact.WasPerformedThisFrame()) // Hold が成立したフレーム
        {
            StartDialogue(activeInput);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TrySetActiveTalker(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // 入り直しや入れ替えにも強くする
        if (!talking) TrySetActiveTalker(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // 抜けたのが今の話者なら解除
        var pi = other.GetComponentInParent<PlayerInput>();
        if (pi != null && pi == activeInput)
        {
            inRange = false;
            activeInput = null;
        }
    }

    void TrySetActiveTalker(Collider2D other)
    {
        // PlayerController or MawaruController を持つ相手だけ対象
        var pc = other.GetComponentInParent<PlayerController>();
        var mc = other.GetComponentInParent<MawaruController>();
        if (pc == null && mc == null) return;

        var pi = other.GetComponentInParent<PlayerInput>();
        if (pi == null) return;

        activeInput = pi;
        inRange = true;

        // 会話中に止めたいものをキャッシュ（開始時に使う）
        if (disableMovementScripts)
        {
            if (pc != null)
            {
                cachedLocks = new MonoBehaviour[] { pc };
            }
            else
            {
                cachedLocks = new MonoBehaviour[] { mc };
            }
        }

        if (freezeRigidbody2D)
        {
            var rb = other.GetComponentInParent<Rigidbody2D>();
            cachedBodies = rb ? new Rigidbody2D[] { rb } : null;
        }
    }

    void StartDialogue(PlayerInput pi)
    {
        if (talking) return;
        if (!dialogueRoot || !dialogueText)
        {
            Debug.LogWarning("[NpcDialogueTrigger] UI参照(dialogueRoot/dialogueText)が未設定です");
            return;
        }

        talking = true;
        index = 0;

        // 操作停止（任意）
        if (disableMovementScripts && cachedLocks != null)
        {
            foreach (var b in cachedLocks) if (b) b.enabled = false;
        }

        // その場固定（任意）
        if (freezeRigidbody2D && cachedBodies != null)
        {
            bodyBackups = new BodyBackup[cachedBodies.Length];
            for (int i = 0; i < cachedBodies.Length; i++)
            {
                var rb = cachedBodies[i];
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

        // ActionMap を Dialog に切替
        prevMapName = pi.currentActionMap != null ? pi.currentActionMap.name : null;
        try { pi.SwitchCurrentActionMap(dialogMapName); }
        catch { /* 無視 */ }

        // Dialog/Next を購読
        var map = pi.actions.FindActionMap(dialogMapName, throwIfNotFound: false);
        nextAction = map != null ? map.FindAction(dialogNextActionName, throwIfNotFound: false) : null;

        if (nextAction != null)
        {
            nextAction.performed += OnNext;
            nextAction.Enable();
        }
        else
        {
            Debug.LogWarning("[NpcDialogueTrigger] Dialog/Next が見つかりません。Dialog ActionMap と Next Action 名を確認してね。");
        }

        dialogueRoot.SetActive(true);
        ShowLine(index);
    }

    void OnNext(InputAction.CallbackContext ctx)
    {
        if (!talking) return;

        index++;
        if (index >= lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowLine(index);
    }

    void ShowLine(int i)
    {
        if (i < 0 || i >= lines.Length) return;

        var line = lines[i];
        if (dialogueText) dialogueText.text = line.text ?? "";

        if (portraitImage)
        {
            portraitImage.sprite = line.portrait;
            portraitImage.enabled = (line.portrait != null);
        }

        if (nameText) nameText.text = line.speakerName ?? "";
    }

    void EndDialogue()
    {
        // 入力解除
        if (nextAction != null)
        {
            nextAction.performed -= OnNext;
            nextAction = null;
        }

        if (dialogueRoot) dialogueRoot.SetActive(false);

        // ActionMap を元に戻す
        if (activeInput != null && !string.IsNullOrEmpty(prevMapName))
        {
            try { activeInput.SwitchCurrentActionMap(prevMapName); }
            catch { /* 無視 */ }
        }

        // Rigidbody 戻す
        if (freezeRigidbody2D && cachedBodies != null && bodyBackups != null)
        {
            for (int i = 0; i < cachedBodies.Length; i++)
            {
                var rb = cachedBodies[i];
                if (!rb) continue;

                rb.constraints = bodyBackups[i].constraints;
                rb.gravityScale = bodyBackups[i].gravity;
                rb.simulated = bodyBackups[i].simulated;
            }
        }

        // 操作を戻す
        if (disableMovementScripts && cachedLocks != null)
        {
            foreach (var b in cachedLocks) if (b) b.enabled = true;
        }

        talking = false;
        index = 0;
    }
}