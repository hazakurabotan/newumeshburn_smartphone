using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GearRopeDial : MonoBehaviour
{
    [Header("Latch behavior")]
    public bool requireLatch = true;           // ラッチしている間だけ回せる

    [Header("Input")]
    [Range(0f, 1f)] public float deadzone = 0.25f;
    [Tooltip("1段階に必要な累計角度（度）。例: 720=2周")]
    public float degreesPerStep = 720f;

    [Header("Bridge objects (どれか1つだけ表示)")]
    public GameObject hashi01_Down; // 完了（道）
    public GameObject hashi02_Mid;  // 中間
    public GameObject hashi03_Up;   // 初期

    [Header("Options")]
    public bool lockWhenDown = true;
    public bool debugLog = true;

#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions (任意)")]
    public InputActionReference moveAction;    // Player/Move(Left Stick)
    public InputActionReference dpadAction;    // Player/Move(D-Pad)
#endif

    // ---- 内部状態 ----
    RopeHead latchedHead;          // つかまっている先端（無くなったら解除）
    float lastAngle;
    bool angleInit;
    float accumAbsDegrees;
    int stepNow = 0;
    int lastDirIndex = -1;         // D-pad 0:→,1:↓,2:←,3:↑

    void Start() => ApplyStep(0, true);

    void Update()
    {
        // 粘着ラッチ：RopeHead が居る限り true
        bool isLatched = latchedHead != null;
        if (requireLatch && !isLatched) return;

        Vector2 v = ReadAnalogVector();     // スティック等の連続入力

        if (v.magnitude > deadzone)
        {
            float a = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (!angleInit) { lastAngle = a; angleInit = true; }
            float delta = Mathf.DeltaAngle(lastAngle, a);
            accumAbsDegrees += Mathf.Abs(delta);
            lastAngle = a;
            lastDirIndex = -1; // アナログを使ったらD-pad状態リセット
        }
        else
        {
            // D-pad順送り（→↓←↑ or 逆回りで+90°）
            int idx = ReadDpadIndex();
            if (idx != -1)
            {
                if (lastDirIndex == -1) lastDirIndex = idx;
                if (idx != lastDirIndex)
                {
                    int diff = (idx - lastDirIndex + 4) % 4;
                    if (diff == 1 || diff == 3) accumAbsDegrees += 90f;
                    lastDirIndex = idx;
                }
            }
        }

        int newStep = Mathf.Clamp((int)(accumAbsDegrees / degreesPerStep), 0, 2);
        if (newStep != stepNow) ApplyStep(newStep, false);

        if (debugLog && isLatched)
            Debug.Log($"[Gear] accum={accumAbsDegrees:F1} step={stepNow}");
    }

    void ApplyStep(int s, bool force)
    {
        stepNow = s;
        SetActiveWithColliders(hashi03_Up, s == 0);
        SetActiveWithColliders(hashi02_Mid, s == 1);
        SetActiveWithColliders(hashi01_Down, s == 2);
        if (lockWhenDown && stepNow >= 2) enabled = false;
        if (debugLog) Debug.Log($"[Gear] STEP -> {stepNow}");
    }

    void SetActiveWithColliders(GameObject go, bool on)
    {
        if (!go) return;
        go.SetActive(on);
        foreach (var c in go.GetComponentsInChildren<Collider2D>(true)) c.enabled = on;
    }

    // —— 入力（優先: InputActions → Gamepad → 旧Input → 矢印キー）
    Vector2 ReadAnalogVector()
    {
#if ENABLE_INPUT_SYSTEM
        if (moveAction && moveAction.action != null)
        {
            try { var v = moveAction.action.ReadValue<Vector2>(); if (v != Vector2.zero) return v; } catch { }
        }
        var gp = Gamepad.current;
        if (gp != null)
        {
            var v = gp.leftStick.ReadValue();
            if (v != Vector2.zero) return v;
        }
#endif
        // 旧Input
        float x = 0f, y = 0f;
        try { x = Input.GetAxisRaw("Horizontal"); y = Input.GetAxisRaw("Vertical"); } catch { }
        if (x != 0f || y != 0f) return new Vector2(x, y);

        // 矢印キー
        x = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        y = (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
        return new Vector2(x, y);
    }

    int ReadDpadIndex()
    {
#if ENABLE_INPUT_SYSTEM
        if (dpadAction && dpadAction.action != null)
        {
            try
            {
                Vector2 d = dpadAction.action.ReadValue<Vector2>();
                if (d == Vector2.zero) return -1;
                if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x > 0 ? 0 : 2;
                else return d.y > 0 ? 3 : 1;
            }
            catch { }
        }
        var gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 d = gp.dpad.ReadValue();
            if (d == Vector2.zero) return -1;
            if (Mathf.Abs(d.x) > Mathf.Abs(d.y)) return d.x > 0 ? 0 : 2;
            else return d.y > 0 ? 3 : 1;
        }
#endif
        if (Input.GetKey(KeyCode.RightArrow)) return 0;
        if (Input.GetKey(KeyCode.DownArrow)) return 1;
        if (Input.GetKey(KeyCode.LeftArrow)) return 2;
        if (Input.GetKey(KeyCode.UpArrow)) return 3;
        return -1;
    }

    // —— ラッチ処理（Exitでは解除しない。RopeHeadが消えたら自動で解除）
    bool IsRopeHead(Collider2D other) => other && other.GetComponent<RopeHead>() != null;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsRopeHead(other))
        {
            latchedHead = other.GetComponent<RopeHead>();
            angleInit = false;
            accumAbsDegrees = 0f;
            lastDirIndex = -1;
            if (debugLog) Debug.Log("[Gear] RopeHead latched!");
        }
    }
    void OnTriggerExit2D(Collider2D other)
    {
        // ここでは解除しない（境界でブレても継続させる）
        if (IsRopeHead(other) && debugLog) Debug.Log("[Gear] RopeHead exit (ignored)");
    }

    // Rope側から解除したい時に呼ぶ用（任意）
    public void ForceRelease()
    {
        latchedHead = null;
        angleInit = false;
        lastDirIndex = -1;
        if (debugLog) Debug.Log("[Gear] ForceRelease");
    }
}
