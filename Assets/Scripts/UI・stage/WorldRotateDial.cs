// WorldRotateDial.cs
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class WorldRotateDial : MonoBehaviour
{
    [Header("Latch")]
    public bool requireLatch = true;
    public bool stickyLatch = true;           // Exitでも離さない（推奨）

    [Header("Input")]
    [Range(0f, 1f)] public float deadzone = 0.25f;
    [Tooltip("1回トリガーに必要な累計角度（例:720=2周）")]
    public float degreesPerTurn = 720f;

#if ENABLE_INPUT_SYSTEM
    [Header("Input Actions (任意)")]
    public InputActionReference moveAction;   // Player/Move(Left Stick)
    public InputActionReference dpadAction;   // Player/Move(D-Pad)
#endif

    [Header("Trigger")]
    public UnityEvent onTurn;                 // しきい到達ごとに発火
    public WorldRotator rotator;              // ここに入っていれば onTurn 未設定でも直接呼ぶ
    public bool clockwise = true;             // 右回りか左回りか
    public bool repeatable = true;            // 何度でも回せるか
    public bool debugLog = false;

    // --- 内部 ---
    RopeHead latchedHead;
    float lastAngle; bool angleInit;
    float accumAbsDegrees;
    int lastDirIndex = -1;                    // D-Pad: 0→,1↓,2←,3↑
    int turnsFired = 0;

    void Update()
    {
        if (requireLatch && latchedHead == null) return;

        Vector2 v = ReadAnalog();
        if (v.magnitude > deadzone)
        {
            float a = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (!angleInit) { lastAngle = a; angleInit = true; }
            float delta = Mathf.DeltaAngle(lastAngle, a);
            accumAbsDegrees += Mathf.Abs(delta);
            lastAngle = a;
            lastDirIndex = -1;
        }
        else
        {
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

        float threshold = degreesPerTurn * (turnsFired + 1);
        if (accumAbsDegrees >= threshold)
        {
            // 1回分越えたら発火
            FireTurn();
            turnsFired++;
            if (!repeatable) enabled = false;
        }

        if (debugLog && latchedHead) Debug.Log($"[WorldDial] accum={accumAbsDegrees:F1} fired={turnsFired}");
    }

    void FireTurn()
    {
        if (onTurn != null && onTurn.GetPersistentEventCount() > 0)
            onTurn.Invoke();
        else if (rotator != null)
        {
            if (clockwise) rotator.RotateBy90CW();
            else rotator.RotateBy90CCW();
        }

        if (debugLog) Debug.Log("[WorldDial] TURN!");
    }

    // ---- 入力 ----
    Vector2 ReadAnalog()
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
        // 旧Input/キーボードの代替
        float x = 0, y = 0;
        try { x = Input.GetAxisRaw("Horizontal"); y = Input.GetAxisRaw("Vertical"); } catch { }
        if (x != 0 || y != 0) return new Vector2(x, y);
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

    // ---- ラッチ ----
    bool IsRopeHead(Collider2D c) => c && c.GetComponent<RopeHead>() != null;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsRopeHead(other))
        {
            latchedHead = other.GetComponent<RopeHead>();
            angleInit = false;
            accumAbsDegrees = 0f;
            lastDirIndex = -1;
            if (debugLog) Debug.Log("[WorldDial] latched");
        }
    }
    void OnTriggerExit2D(Collider2D other)
    {
        if (!stickyLatch && IsRopeHead(other))
        {
            latchedHead = null;
            angleInit = false;
            lastDirIndex = -1;
            if (debugLog) Debug.Log("[WorldDial] released");
        }
    }
}
