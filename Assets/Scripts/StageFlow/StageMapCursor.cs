using UnityEngine;
using UnityEngine.InputSystem;

public class StageMapCursor : MonoBehaviour
{
    [Header("開始地点")]
    public StageNode current;

    [Header("参照（あなたのStageSelectManagerを入れる）")]
    public StageSelectManager stageSelectManager;

    [Header("移動の気持ちよさ調整")]
    public float moveSpeed = 10f;
    public float snapDistance = 0.5f;
    public float inputCooldown = 0.12f;

    [Header("InputActions（PlayerControls.inputactions を入れる）")]
    public InputActionAsset actionsAsset;
    public string actionMapName = "UI";
    public string navigateActionName = "Navigate";
    public string submitActionName = "Submit";

    InputAction _navigate;
    InputAction _submit;

    StageNode _target;
    bool _moving;
    float _cooldown;

    [Header("見た目位置オフセット（ノード中心からのズレ）")]
    public Vector3 cursorOffset = new Vector3(-120f, 0f, 0f);

    [Header("向き（開始時は右向き）")]
    public bool startFacingRight = true;

    void Awake()
    {
        if (actionsAsset != null)
        {
            var map = actionsAsset.FindActionMap(actionMapName, true);
            _navigate = map.FindAction(navigateActionName, true);
            _submit = map.FindAction(submitActionName, true);
        }
    }

    void OnEnable()
    {
        _navigate?.Enable();
        _submit?.Enable();
    }

    void OnDisable()
    {
        _navigate?.Disable();
        _submit?.Disable();
    }

    void Start()
    {
        SetFacing(startFacingRight);

        if (current != null)
            transform.position = current.transform.position + cursorOffset;
    }

    void Update()
    {
        if (current == null || stageSelectManager == null) return;

        if (_cooldown > 0f) _cooldown -= Time.unscaledDeltaTime;

        // 移動中
        if (_moving)
        {
            MoveToTarget();
            return;
        }

        // 決定（South / A）
        if (_submit != null && _submit.WasPressedThisFrame())
        {
            if (current.unlocked && !string.IsNullOrEmpty(current.stageSceneName))
            {
                stageSelectManager.ChooseNode(current);
            }
            return;
        }

        if (_cooldown > 0f) return;

        // 方向入力（Dpad / LeftStick）
        Vector2 raw = _navigate != null ? _navigate.ReadValue<Vector2>() : Vector2.zero;
        if (raw.sqrMagnitude < 0.5f) return;

        Vector2 dir = ToCardinal(raw);

        // ★左右移動のときだけ向きを変える
        if (dir.x < 0) SetFacing(false);   // 左向き
        else if (dir.x > 0) SetFacing(true); // 右向き

        StageNode next = GetNext(dir);
        if (next == null) return;
        if (!next.unlocked) return;

        _target = next;
        _moving = true;
        _cooldown = inputCooldown;
    }

    Vector2 ToCardinal(Vector2 v)
    {
        // 斜め入力は強い方に丸める（上下左右だけ）
        if (Mathf.Abs(v.x) >= Mathf.Abs(v.y))
            return new Vector2(Mathf.Sign(v.x), 0);
        else
            return new Vector2(0, Mathf.Sign(v.y));
    }

    StageNode GetNext(Vector2 dir)
    {
        if (dir.y > 0) return current.up;
        if (dir.y < 0) return current.down;
        if (dir.x < 0) return current.left;
        if (dir.x > 0) return current.right;
        return null;
    }

    void MoveToTarget()
    {
        Vector3 dst = _target.transform.position + cursorOffset;
        transform.position = Vector3.MoveTowards(transform.position, dst, moveSpeed * Time.unscaledDeltaTime);

        if ((transform.position - dst).sqrMagnitude <= snapDistance * snapDistance)
        {
            transform.position = dst;
            current = _target;
            _target = null;
            _moving = false;
        }
    }

    void SetFacing(bool faceRight)
    {
        var s = transform.localScale;
        float absX = Mathf.Abs(s.x);
        s.x = faceRight ? absX : -absX;
        transform.localScale = s;
    }
}