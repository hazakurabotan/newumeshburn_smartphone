using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class ImpactRunnerController : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference alternateMoveAction; // Move2 用
    public InputActionReference jumpAction;
    public InputActionReference attackAction;
    public InputActionReference dashAction;

    [Header("Movement Mode")]
    [Tooltip("ONならプレイヤー自身が自動前進。今回はOFF推奨")]
    public bool autoForwardBySelf = false;
    [Min(0f)] public float autoForwardSpeed = 6f;

    [Header("Player Movement")]
    public bool allowBackwardMove = true;
    [Min(0f)] public float manualMoveSpeed = 5f;
    [Min(0f)] public float acceleration = 16f;
    [Min(0f)] public float deceleration = 20f;
    [Range(0f, 1f)] public float airControl = 0.7f;
    [Min(0f)] public float jumpForce = 11f;
    [Min(0f)] public float maxFallSpeed = 18f;

    [Header("Ground Check")]
    public Transform groundCheck;
    [Min(0.01f)] public float groundCheckRadius = 0.18f;
    public LayerMask groundLayer;

    [Header("Attack")]
    [Min(1)] public int attackDamage = 1;
    [Min(0f)] public float attackCooldown = 0.25f;
    [Min(0f)] public float attackActiveTime = 0.08f;
    public Vector2 attackBoxSize = new Vector2(1.2f, 0.8f);
    public Vector2 attackBoxOffset = new Vector2(1.0f, 0.1f);

    [Header("Dash")]
    [Min(0f)] public float dashSpeed = 12f;
    [Min(0f)] public float dashDuration = 0.18f;
    [Min(0f)] public float dashCooldown = 0.7f;

    [Header("Health")]
    [Min(1)] public int maxHP = 5;
    [Min(0f)] public float invincibleTime = 0.8f;
    [Min(0f)] public float knockbackX = 5f;
    [Min(0f)] public float knockbackY = 3.5f;

    [Header("Optional References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    private Rigidbody2D rb;

    private int currentHP;
    private bool grounded;
    private bool jumpQueued;
    private bool inputEnabled = true;
    private bool stopped;
    private bool isAttacking;
    private bool isDashing;
    private bool dead;

    private float nextAttackTime;
    private float nextDashTime;
    private float invincibleUntil;

    // 後ろの壁から押されている時の最低速度
    private float activePusherSpeed;
    private float pusherKeepUntil;

    private static readonly int AnimRun = Animator.StringToHash("Run");
    private static readonly int AnimGrounded = Animator.StringToHash("Grounded");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");
    private static readonly int AnimDash = Animator.StringToHash("Dash");
    private static readonly int AnimDead = Animator.StringToHash("Dead");
    private static readonly int AnimSpeedY = Animator.StringToHash("SpeedY");

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsGrounded => grounded;
    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        currentHP = maxHP;
    }

    private void OnEnable()
    {
        EnableAction(moveAction);
        EnableAction(alternateMoveAction);
        EnableAction(jumpAction);
        EnableAction(attackAction);
        EnableAction(dashAction);
    }

    private void OnDisable()
    {
        DisableAction(moveAction);
        DisableAction(alternateMoveAction);
        DisableAction(jumpAction);
        DisableAction(attackAction);
        DisableAction(dashAction);
    }

    private void Start()
    {
        ImpactRunGameManager.Instance?.RegisterPlayer(this);
        UpdateAnimator();
    }

    private void Update()
    {
        if (Time.time > pusherKeepUntil)
            activePusherSpeed = 0f;

        if (dead || stopped)
        {
            UpdateAnimator();
            return;
        }

        grounded = CheckGrounded();

        if (inputEnabled)
        {
            if (jumpAction != null && jumpAction.action != null && jumpAction.action.WasPressedThisFrame())
                jumpQueued = true;

            if (attackAction != null && attackAction.action != null && attackAction.action.WasPressedThisFrame())
                TryAttack();

            if (dashAction != null && dashAction.action != null && dashAction.action.WasPressedThisFrame())
                TryDash();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (dead || stopped)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        float inputX = 0f;
        if (inputEnabled)
            inputX = ReadHorizontalInput();

        if (!allowBackwardMove)
            inputX = Mathf.Clamp01(inputX);
        else
            inputX = Mathf.Clamp(inputX, -1f, 1f);

        float control = grounded ? 1f : airControl;

        float desiredX = 0f;

        if (autoForwardBySelf)
            desiredX += autoForwardSpeed;

        desiredX += inputX * manualMoveSpeed;

        // 後ろの壁に押されている間は、その速度未満にならない
        desiredX = Mathf.Max(desiredX, activePusherSpeed);

        if (isDashing)
            desiredX = Mathf.Max(desiredX, dashSpeed);

        float lerpRate = Mathf.Abs(desiredX) > Mathf.Abs(rb.velocity.x) ? acceleration : deceleration;
        float newX = Mathf.Lerp(rb.velocity.x, desiredX, lerpRate * control * Time.fixedDeltaTime);

        float newY = rb.velocity.y;
        if (newY < -maxFallSpeed)
            newY = -maxFallSpeed;

        rb.velocity = new Vector2(newX, newY);

        if (jumpQueued && grounded)
        {
            jumpQueued = false;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            grounded = false;
        }
        else if (jumpQueued && !grounded)
        {
            jumpQueued = false;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    public void StopRunner()
    {
        stopped = true;
        inputEnabled = false;
        isDashing = false;
        isAttacking = false;

        if (rb != null)
            rb.velocity = Vector2.zero;

        UpdateAnimator();
    }

    public void ResumeRunner()
    {
        stopped = false;
        inputEnabled = true;
    }

    // 後ろの壁から呼ぶ
    public void SetPusherSpeed(float speed)
    {
        if (speed <= 0f)
            return;

        activePusherSpeed = Mathf.Max(activePusherSpeed, speed);
        pusherKeepUntil = Time.time + 0.12f;
    }

    public void TakeDamage(int amount, Vector2 hitPoint)
    {
        if (dead)
            return;

        if (Time.time < invincibleUntil)
            return;

        currentHP = Mathf.Max(0, currentHP - amount);
        invincibleUntil = Time.time + invincibleTime;

        Vector2 dir = ((Vector2)transform.position - hitPoint);
        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector2.left;
        dir.Normalize();

        rb.velocity = new Vector2(dir.x * knockbackX, knockbackY);

        ImpactRunGameManager.Instance?.SetPlayerHP(currentHP, maxHP);

        if (currentHP <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(DamageFlashRoutine());
        }
    }

    private void Die()
    {
        dead = true;
        stopped = true;
        inputEnabled = false;

        if (animator != null)
            animator.SetBool(AnimDead, true);

        ImpactRunGameManager.Instance?.PlayerDied();
    }

    private float ReadHorizontalInput()
    {
        float value = 0f;

        if (moveAction != null && moveAction.action != null)
        {
            Vector2 v = moveAction.action.ReadValue<Vector2>();
            if (Mathf.Abs(v.x) > Mathf.Abs(value))
                value = v.x;
        }

        if (alternateMoveAction != null && alternateMoveAction.action != null)
        {
            Vector2 v = alternateMoveAction.action.ReadValue<Vector2>();
            if (Mathf.Abs(v.x) > Mathf.Abs(value))
                value = v.x;
        }

        return value;
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime)
            return;

        if (isAttacking)
            return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        if (animator != null)
            animator.SetTrigger(AnimAttack);

        PerformAttackHit();

        yield return new WaitForSeconds(attackActiveTime);

        isAttacking = false;
    }

    private void PerformAttackHit()
    {
        Vector2 center = (Vector2)transform.position + attackBoxOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f);

        HashSet<Obstacle> hitObstacles = new HashSet<Obstacle>();

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
                continue;

            Obstacle obstacle = hits[i].GetComponentInParent<Obstacle>();
            if (obstacle == null)
                continue;

            if (hitObstacles.Contains(obstacle))
                continue;

            hitObstacles.Add(obstacle);
            obstacle.TakeHit(attackDamage, transform.position, this);
        }
    }

    private void TryDash()
    {
        if (Time.time < nextDashTime)
            return;

        if (isDashing)
            return;

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        nextDashTime = Time.time + dashCooldown;

        if (animator != null)
            animator.SetTrigger(AnimDash);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null)
            return false;

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        float endTime = Time.time + invincibleTime;

        while (Time.time < endTime)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(0.06f);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(0.06f);
        }

        spriteRenderer.enabled = true;
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetBool(AnimRun, !stopped && Mathf.Abs(rb.velocity.x) > 0.1f);
        animator.SetBool(AnimGrounded, grounded);
        animator.SetFloat(AnimSpeedY, rb.velocity.y);
    }

    private void EnableAction(InputActionReference actionRef)
    {
        if (actionRef != null && actionRef.action != null)
            actionRef.action.Enable();
    }

    private void DisableAction(InputActionReference actionRef)
    {
        if (actionRef != null && actionRef.action != null)
            actionRef.action.Disable();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 attackCenter = transform.position + (Vector3)attackBoxOffset;
        Gizmos.DrawWireCube(attackCenter, attackBoxSize);

        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}