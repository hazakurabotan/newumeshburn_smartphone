using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class GearHangAndRotate : MonoBehaviour
{
    [Header("Refs")]
    public RotRoomController rotRoom;

    [Header("Input (Controller)")]
    [Tooltip("ActionMap 名（あなたの設定だと Mawaru）")]
    public string actionMapName = "Mawaru";

    [Tooltip("Action 名（あなたの設定だと Move）")]
    public string moveActionName = "Move";

    public float deadZone = 0.5f;
    public float rotateCooldown = 0.2f;

    float nextOkTime = 0f;

    MawaruController currentPlayer;
    PlayerInput currentPlayerInput;

    InputActionMap cachedMap;
    InputAction moveAction;

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Awake()
    {
        if (!rotRoom) rotRoom = GetComponentInParent<RotRoomController>();
    }

    void Update()
    {
        if (!currentPlayer) return;
        if (!rotRoom) return;
        if (!currentPlayer.IsHangingNow) return;
        if (Time.time < nextOkTime) return;
        if (rotRoom.IsRotating) return;

        // 毎フレーム安全に保証（切替直後にMapが変わる事があるため）
        EnsureAction();

        if (moveAction == null) return;

        Vector2 v = moveAction.ReadValue<Vector2>();
        float x = v.x;

        string mapNow = (currentPlayerInput != null && currentPlayerInput.currentActionMap != null)
            ? currentPlayerInput.currentActionMap.name
            : "null";

        Debug.Log($"[Gear] map={mapNow} usingMap={actionMapName} moveEnabled={moveAction.enabled} axisX={x}");

        if (x > deadZone)
        {
            if (rotRoom.RequestRotate(+1))
                nextOkTime = Time.time + rotateCooldown;
        }
        else if (x < -deadZone)
        {
            if (rotRoom.RequestRotate(-1))
                nextOkTime = Time.time + rotateCooldown;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<MawaruController>();
        if (p == null) return;

        currentPlayer = p;
        currentPlayerInput = currentPlayer.GetComponent<PlayerInput>();

        if (currentPlayerInput == null)
        {
            Debug.LogError("[Gear] Mawaru に PlayerInput が見つかりません（Mawaru と同じ GameObject に必要）");
            return;
        }

        // ギア中は必ず Mawaru Map に寄せる（Switch直後のズレ対策）
        if (currentPlayerInput.currentActionMap == null || currentPlayerInput.currentActionMap.name != actionMapName)
        {
            currentPlayerInput.SwitchCurrentActionMap(actionMapName);
        }

        CacheActions();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var p = other.GetComponentInParent<MawaruController>();
        if (p != null && p == currentPlayer)
        {
            currentPlayer = null;
            currentPlayerInput = null;
            cachedMap = null;
            moveAction = null;
        }
    }

    void EnsureAction()
    {
        if (currentPlayerInput == null) return;

        // Mapが切り替わったら取り直す
        if (currentPlayerInput.currentActionMap == null || currentPlayerInput.currentActionMap.name != actionMapName)
        {
            currentPlayerInput.SwitchCurrentActionMap(actionMapName);
            CacheActions();
            return;
        }

        if (cachedMap == null || moveAction == null)
        {
            CacheActions();
            return;
        }

        if (!moveAction.enabled)
            moveAction.Enable();
    }

    void CacheActions()
    {
        cachedMap = null;
        moveAction = null;

        if (currentPlayerInput == null || currentPlayerInput.actions == null)
        {
            Debug.LogError("[Gear] PlayerInput.actions が null です（InputActionAsset 割り当て確認）");
            return;
        }

        cachedMap = currentPlayerInput.actions.FindActionMap(actionMapName, true);
        if (cachedMap == null)
        {
            Debug.LogError($"[Gear] ActionMap '{actionMapName}' が見つかりません。InputActions を確認してください。");
            return;
        }

        moveAction = cachedMap.FindAction(moveActionName, true);
        if (moveAction == null)
        {
            Debug.LogError($"[Gear] Action '{moveActionName}' が '{actionMapName}' 内に見つかりません。InputActions を確認してください。");
            return;
        }

        if (!moveAction.enabled) moveAction.Enable();

        // デバッグ：どのBindingを掴んだか確認できる
        Debug.Log($"[Gear] Cached '{actionMapName}/{moveActionName}' bindings={moveAction.bindings.Count}");
    }
}