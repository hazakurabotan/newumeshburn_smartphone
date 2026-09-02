using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class LadderClimber2D : MonoBehaviour
{
    [Header("入力")]
    [SerializeField] private InputActionReference moveAction;

    [Header("はしご設定")]
    [SerializeField] private string ladderTag = "Ladder";
    [SerializeField] private float climbSpeed = 3.0f;
    [SerializeField] private bool stopGravityWhileClimbing = true;

    [Header("アニメーション")]
    [SerializeField] private Animator animator;
    [SerializeField] private string climbingBoolName = "IsClimbing";
    [SerializeField] private string climbSpeedFloatName = "ClimbSpeed";

    [Header("上端に着いたらシーン移動する場合")]
    [SerializeField] private bool changeSceneAtTop = false;
    [SerializeField] private string nextSceneName = "Stage2";
    [SerializeField] private float topYPosition = 10.0f;

    private Rigidbody2D rb;

    private bool isTouchingLadder;
    private bool isClimbing;
    private float originalGravityScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Disable();
        }
    }

    private void Update()
    {
        Vector2 moveInput = ReadMoveInput();

        if (isTouchingLadder && Mathf.Abs(moveInput.y) > 0.1f)
        {
            StartClimbing();
        }

        if (isClimbing && !isTouchingLadder)
        {
            StopClimbing();
        }

        UpdateAnimator(moveInput);

        if (changeSceneAtTop && isClimbing && transform.position.y >= topYPosition)
        {
            ChangeScene();
        }
    }

    private void FixedUpdate()
    {
        if (!isClimbing)
        {
            return;
        }

        Vector2 moveInput = ReadMoveInput();

        rb.velocity = new Vector2(
            rb.velocity.x,
            moveInput.y * climbSpeed
        );
    }

    private Vector2 ReadMoveInput()
    {
        if (moveAction == null || moveAction.action == null)
        {
            return Vector2.zero;
        }

        return moveAction.action.ReadValue<Vector2>();
    }

    private void StartClimbing()
    {
        if (isClimbing)
        {
            return;
        }

        isClimbing = true;

        if (stopGravityWhileClimbing)
        {
            rb.gravityScale = 0f;
        }

        rb.velocity = Vector2.zero;
    }

    private void StopClimbing()
    {
        if (!isClimbing)
        {
            return;
        }

        isClimbing = false;
        rb.gravityScale = originalGravityScale;
    }

    private void UpdateAnimator(Vector2 moveInput)
    {
        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(climbingBoolName))
        {
            animator.SetBool(climbingBoolName, isClimbing);
        }

        if (!string.IsNullOrEmpty(climbSpeedFloatName))
        {
            animator.SetFloat(climbSpeedFloatName, Mathf.Abs(moveInput.y));
        }
    }

    private void ChangeScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            return;
        }

        rb.velocity = Vector2.zero;
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(ladderTag))
        {
            isTouchingLadder = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(ladderTag))
        {
            isTouchingLadder = false;
            StopClimbing();
        }
    }
}