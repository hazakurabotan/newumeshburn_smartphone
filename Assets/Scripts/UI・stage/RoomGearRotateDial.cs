using UnityEngine;
using UnityEngine.InputSystem;

public class RoomGearRotateDial : MonoBehaviour
{
    [Header("Refs")]
    public RotRoomController2 rotRoom;

    [Header("Input")]
    public string actionMapName = "Mawaru";
    public string moveActionName = "Move";
    public float deadZone = 0.5f;
    public float rotateCooldown = 0.25f;

    float nextOkTime;
    bool latched;

    PlayerInput playerInput;
    InputAction moveAction;
    Vector2 lastMove; // ★ここに値を保持

    void Awake()
    {
        if (!rotRoom) rotRoom = GetComponentInParent<RotRoomController2>();
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void Update()
    {
        if (!latched) return;
        if (!rotRoom || rotRoom.IsRotating) return;
        if (Time.time < nextOkTime) return;
        if (moveAction == null) return;

        float x = lastMove.x;

        if (x > deadZone)
        {
            if (rotRoom.RequestRotateStep(+1))
                nextOkTime = Time.time + rotateCooldown;
        }
        else if (x < -deadZone)
        {
            if (rotRoom.RequestRotateStep(-1))
                nextOkTime = Time.time + rotateCooldown;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // RopeHead / メカアーム先端が当たった時だけ latch したいなら tag を使うのが一番安全
        // 例：RopeHead に Tag "RopeHead" を付ける
        if (!other.CompareTag("RopeHead")) return;

        latched = true;
        BindToCurrentPlayer();
        Debug.Log("[RoomGear] latched!");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("RopeHead")) return;

        latched = false;
        Unbind();
        Debug.Log("[RoomGear] unlatched");
    }

    void BindToCurrentPlayer()
    {
        // Mawaru本体に PlayerInput が付いてる想定（あなたの構成に合わせる）
        // 取れない場合は MawaruController 側から PlayerInput を辿る形にしてもOK
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (!playerInput || playerInput.actions == null)
        {
            Debug.LogError("[RoomGear] PlayerInput/actions が見つかりません");
            return;
        }

        // 必ずこのMapに揃える（切替でズレても復帰する）
        if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != actionMapName)
            playerInput.SwitchCurrentActionMap(actionMapName);

        moveAction = playerInput.actions.FindActionMap(actionMapName, true).FindAction(moveActionName, true);

        // ★performed/canceledで値を保持（ReadValueが0固定になる事故を回避）
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        moveAction.Enable();
    }

    void Unbind()
    {
        if (moveAction != null)
        {
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
        }
        moveAction = null;
        playerInput = null;
        lastMove = Vector2.zero;
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        lastMove = ctx.ReadValue<Vector2>();
    }
}