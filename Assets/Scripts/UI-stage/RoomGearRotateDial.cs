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
    Vector2 lastMove;

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
        if (other == null) return;

        // Mawaru の RopeHead でラッチ
        if (other.CompareTag("RopeHead"))
        {
            latched = true;
            BindToCurrentPlayer();
            Debug.Log("[RoomGear] latched!");
            return;
        }

        // Player のボムが当たったら、右に1ステップ（45度）回す
        var bomb = other.GetComponent<PlayerBombProjectile>();
        if (bomb == null) bomb = other.GetComponentInParent<PlayerBombProjectile>();
        if (bomb == null) return;

        TryRotateByBomb();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;
        if (!other.CompareTag("RopeHead")) return;

        latched = false;
        Unbind();
        Debug.Log("[RoomGear] unlatched");
    }

    void TryRotateByBomb()
    {
        if (!rotRoom) return;
        if (rotRoom.IsRotating) return;
        if (Time.time < nextOkTime) return;

        if (rotRoom.RequestRotateStep(+1))
        {
            nextOkTime = Time.time + rotateCooldown;
            Debug.Log("[RoomGear] rotated by bomb (+45)");
        }
    }

    void BindToCurrentPlayer()
    {
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (!playerInput || playerInput.actions == null)
        {
            Debug.LogError("[RoomGear] PlayerInput/actions が見つかりません");
            return;
        }

        if (playerInput.currentActionMap == null || playerInput.currentActionMap.name != actionMapName)
            playerInput.SwitchCurrentActionMap(actionMapName);

        moveAction = playerInput.actions.FindActionMap(actionMapName, true).FindAction(moveActionName, true);

        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;
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