using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Rigidbody2D))]
public class ControllerLadderClimber2D : MonoBehaviour
{
    [Header("はしご判定")]
    [SerializeField] private string ladderTag = "Ladder";

    [Header("登る速さ")]
    [SerializeField] private float climbSpeed = 3.0f;

    [Header("入力")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private float inputDeadZone = 0.15f;

    [Header("はしご中の挙動")]
    [SerializeField] private bool freezeXWhileClimbing = true;
    [SerializeField] private bool startClimbOnlyWhenPressingUpDown = true;

    [Header("ジャンプで はしご解除")]
    [SerializeField] private bool exitLadderOnJump = true;
    [SerializeField] private float jumpOffForce = 7.0f;
    [SerializeField] private float ladderRegrabDelay = 0.25f;

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = false;

    private Rigidbody2D rb;
    private PlayerInput playerInput;

    private float normalGravityScale;
    private float normalDrag;

    private int ladderTouchCount = 0;
    private bool isClimbing = false;

    private InputAction moveAction;
    private InputAction jumpAction;

    private bool jumpRequested;
    private float ignoreLadderUntilTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();

        normalGravityScale = rb.gravityScale;
        normalDrag = rb.drag;

        CacheInputActions();
    }

    private void OnEnable()
    {
        CacheInputActions();
    }

    private void Start()
    {
        CacheInputActions();
    }

    private void OnDisable()
    {
        StopClimbing();
        jumpRequested = false;
    }

    private void Update()
    {
        if (exitLadderOnJump && IsJumpPressedThisFrame())
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (ladderTouchCount <= 0)
        {
            StopClimbing();
            jumpRequested = false;
            return;
        }

        if (Time.time < ignoreLadderUntilTime)
        {
            StopClimbing();
            jumpRequested = false;
            return;
        }

        Vector2 move = ReadMoveInput();

        if (isClimbing && jumpRequested)
        {
            jumpRequested = false;
            JumpOffLadder();
            return;
        }

        jumpRequested = false;

        if (!isClimbing)
        {
            if (startClimbOnlyWhenPressingUpDown)
            {
                if (Mathf.Abs(move.y) <= inputDeadZone)
                {
                    return;
                }
            }

            StartClimbing();
        }

        float vertical = Mathf.Abs(move.y) > inputDeadZone ? move.y : 0f;
        float horizontal = freezeXWhileClimbing ? 0f : rb.velocity.x;

        rb.gravityScale = 0f;
        rb.drag = normalDrag;

        rb.velocity = new Vector2(horizontal, vertical * climbSpeed);
    }

    private void CacheInputActions()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        moveAction = playerInput.actions.FindAction(moveActionName, false);
        jumpAction = playerInput.actions.FindAction(jumpActionName, false);
    }

    private Vector2 ReadMoveInput()
    {
        if (moveAction == null)
        {
            CacheInputActions();
        }

        if (moveAction != null)
        {
            return moveAction.ReadValue<Vector2>();
        }

        Vector2 value = Vector2.zero;

        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            Vector2 dpad = Gamepad.current.dpad.ReadValue();

            if (Mathf.Abs(dpad.x) > Mathf.Abs(stick.x) || Mathf.Abs(dpad.y) > Mathf.Abs(stick.y))
            {
                value = dpad;
            }
            else
            {
                value = stick;
            }
        }

        if (Keyboard.current != null)
        {
            float x = 0f;
            float y = 0f;

            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
            {
                x -= 1f;
            }

            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
            {
                x += 1f;
            }

            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed)
            {
                y += 1f;
            }

            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed)
            {
                y -= 1f;
            }

            if (Mathf.Abs(x) > 0f || Mathf.Abs(y) > 0f)
            {
                value = new Vector2(x, y);
            }
        }

        return value;
    }

    private bool IsJumpPressedThisFrame()
    {
        if (jumpAction == null)
        {
            CacheInputActions();
        }

        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            return true;
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                return true;
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.zKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }

    private void StartClimbing()
    {
        if (isClimbing)
        {
            return;
        }

        isClimbing = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        if (debugLog)
        {
            Debug.Log($"[{name}] はしご開始");
        }
    }

    private void StopClimbing()
    {
        if (!isClimbing)
        {
            return;
        }

        isClimbing = false;
        rb.gravityScale = normalGravityScale;
        rb.drag = normalDrag;

        if (debugLog)
        {
            Debug.Log($"[{name}] はしご終了");
        }
    }

    private void JumpOffLadder()
    {
        StopClimbing();

        ignoreLadderUntilTime = Time.time + ladderRegrabDelay;

        rb.gravityScale = normalGravityScale;
        rb.drag = normalDrag;

        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpOffForce, ForceMode2D.Impulse);

        if (debugLog)
        {
            Debug.Log($"[{name}] はしごジャンプ解除");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(ladderTag))
        {
            return;
        }

        ladderTouchCount++;

        if (debugLog)
        {
            Debug.Log($"[{name}] Ladder Enter : {other.name}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(ladderTag))
        {
            return;
        }

        ladderTouchCount = Mathf.Max(0, ladderTouchCount - 1);

        if (ladderTouchCount <= 0)
        {
            StopClimbing();
        }

        if (debugLog)
        {
            Debug.Log($"[{name}] Ladder Exit : {other.name}");
        }
    }
}