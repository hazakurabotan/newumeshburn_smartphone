using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class MawaruController : MonoBehaviour
{
    public Vector2 MoveAxis => new Vector2(axisH, axisV);
    public bool IsHangingNow => isHanging;

    // ===== Move / Jump =====
    [Header("Move / Jump")]
    public float speed = 3f;

    [Header("Dash (Double Tap L/R)")]
    public float dashSpeed = 11f;
    public float dashKeepTime = 0.35f;
    public float doubleTapWindow = 0.25f;

    float dashTimer;
    float lastTapTime = -999f;
    int lastTapDir = 0;
    float prevAxisH = 0f;

    [Header("Slide (Double Tap Down) - Ground Only")]
    [Tooltip("↓↓でスライド。空中では発動しません。")]
    public float slideSpeed = 10f;
    public float slideKeepTime = 0.35f;
    public float slideDoubleTapWindow = 0.25f;
    public float slideCooldown = 0.10f;

    float slideTimer;
    float slideCooldownTimer;
    float lastDownTapTime = -999f;
    float prevAxisV = 0f;
    bool isSliding = false;
    int slideDir = 1;

    public float jumpForce = 9f;
    public Transform groundCheck;
    public float groundRadius = 0.1f;
    public LayerMask groundLayer;

    // ===== Hanging =====
    [Header("Hanging Tuning")]
    public float hangSwingForce = 25f;
    public float hangMaxSpeed = 6f;
    public float hangDrag = 0.5f;
    public float hangJumpForce = 9f;

    [Header("Auto Swing")]
    [Tooltip("ぶら下がり中に自動で往復する最大角度")]
    public float autoSwingMaxAngle = 55f;
    [Tooltip("ぶら下がり開始直後に付ける初速")]
    public float autoSwingStartKick = 2.5f;
    [Tooltip("ジャンプ離脱時に現在速度へ上乗せする量")]
    public float hangReleaseExtraSpeed = 1.5f;

    // ===== Double Jump (Carry Only) =====
    [Header("Double Jump (Carry Only)")]
    public bool enableCarryDoubleJump = true;
    public float secondJumpForce = 9f;
    public int carryMaxJumpCount = 2;

    [Header("Juice Motion Unlock")]
    public bool unlockDashByJuice = false;
    public bool unlockSlideByJuice = false;
    public bool unlockUpPunchByJuice = false;
    public bool unlockDoubleJumpByJuice = false;
    public bool unlockRocketPunchByJuice = false;

    int jumpCountUsed = 0;
    bool wasGrounded;

    // ===== Punch =====
    [Header("Punch - Hitbox (Animator Sync)")]
    public GameObject punchHitbox;
    public float punchCooldown = 0.20f;

    [Header("Punch Reach (追加で伸ばす量)")]
    public float jabExtend = 0.25f;
    public float straightExtend = 0.85f;
    public float upExtend = 0.75f;
    public float upForward = 0.25f;

    [Header("Punch Collider Size")]
    public Vector2 jabBoxSize = new Vector2(0.9f, 0.6f);
    public Vector2 straightBoxSize = new Vector2(1.5f, 0.6f);
    public Vector2 upBoxSize = new Vector2(0.6f, 1.3f);

    [Header("Punch Extend Curve (0-1 inside window)")]
    [Range(0f, 1f)] public float punchOutEnd = 0.12f;
    [Range(0f, 1f)] public float punchHoldEnd = 0.92f;

    enum PunchKind { Jab, Straight, Up }
    PunchKind currentPunchKind = PunchKind.Jab;

    BoxCollider2D punchBox;
    float punchNormT;
    HitWindow01 punchCurrentWindow;

    [Header("Punch Voice")]
    public AudioSource punchVoiceSource;
    public AudioClip[] punchVoices;
    [Range(0f, 1f)] public float punchVoiceVolume = 1f;

    [Header("Voice")]
    public AudioSource voiceSource;
    public AudioClip damageVoice;
    [Range(0f, 1f)] public float voiceVolume = 1f;

    public float punchOffsetX = 0.6f;

    [Serializable]
    public struct HitWindow01
    {
        [Range(0f, 1f)] public float start;
        [Range(0f, 1f)] public float end;
    }

    [Header("Hit windows (normalizedTime 0-1)")]
    public HitWindow01 punch1Window = new HitWindow01 { start = 0.05f, end = 0.85f };
    public HitWindow01 punch2Window = new HitWindow01 { start = 0.05f, end = 0.85f };
    public HitWindow01 punch3Window = new HitWindow01 { start = 0.05f, end = 0.90f };

    float punchTimer;
    Collider2D punchCol;
    bool punchVisible;
    Vector3 punchLocalDefault;
    int comboStep = 0;
    bool wasInPunch;

    // 押下中の二重反応防止
    bool punchPressLatched = false;
    // 上+パンチを押下時に使ったら、離した時の通常パンチを無効化
    bool consumedPunchOnStarted = false;

    // ===== Rocket Punch (Charge) =====
    [Header("Rocket Punch (Charge)")]
    public bool enableRocketPunch = true;
    public float rocketChargeSeconds = 3f;

    public GameObject rocketPunchPrefab;
    public Transform rocketSpawn;

    public float rocketOutDistance = 2.5f;
    public float rocketOutSpeed = 14f;
    public float rocketReturnSpeed = 18f;
    public float rocketCooldown = 0.3f;

    bool punchHeld;
    float punchHoldStart;
    bool rocketTriggered;
    bool rocketActive;
    float rocketCooldownTimer;
    Coroutine rocketCo;

    // ===== Rope =====
    [Header("Rope")]
    public Transform firePoint;
    public GameObject ropeHeadPrefab;
    public float ropeSpeed = 15f;
    public float ropeLength = 2f;
    public float hangGravity = 10f;

    GameObject currentRopeHead;
    bool ropeShot = false;
    bool isHanging = false;
    float normalGravity;
    float normalDrag;
    bool hangJumpRequested = false;
    int hangSwingDir = 1;
    bool hangStartKickPending = false;

    // ===== Wall =====
    [Header("Wall")]
    public Transform wallCheckLeft, wallCheckRight;
    public float wallCheckDistance = 0.12f;
    public float wallSlideSpeed = 2f;
    public float wallDashPower = 10f;
    public LayerMask wallLayer;
    bool isTouchingWallLeft, isTouchingWallRight, isWallSliding;
    float wallDashLock;

    // ===== HP / Damage =====
    [Header("HP / Damage / Invincible")]
    public int maxHP = 15;
    public int currentHP = 15;
    public event Action<int, int> OnHpChanged;
    public HpBarController hpBar;

    public float invincibleTime = 1.0f;
    float invincibleTimer;
    public Vector2 knockbackVelocity = new Vector2(5f, 4f);
    public float knockbackDuration = 0.2f;
    float knockbackTimer;

    [Header("Contact Damage")]
    [SerializeField] LayerMask contactDamageLayers;
    [SerializeField] int contactDamage = 1;

    // ===== Refs / Input =====
    Rigidbody2D rb;
    Animator animator;
    SpriteRenderer sr;
    float axisH, axisV;
    bool jumpPressed, punchPressed, ropePressed;

    MawaruEquipment equipment;
    bool hasIsAttackingParam;
    bool hasDashParam;

    // Animator hashes
    readonly int HashSpeed = Animator.StringToHash("Speed");
    readonly int HashGrounded = Animator.StringToHash("Grounded");
    readonly int HashPunch = Animator.StringToHash("Punch");
    readonly int HashPunch2 = Animator.StringToHash("Punch2");
    readonly int HashPunch3 = Animator.StringToHash("Punch3");
    readonly int HashDamage = Animator.StringToHash("Damage");
    readonly int HashIsAttacking = Animator.StringToHash("IsAttacking");
    readonly int HashJump2 = Animator.StringToHash("Jump2");
    readonly int HashSlide = Animator.StringToHash("Slide");
    readonly int HashDash = Animator.StringToHash("Dash");

    // State hashes（パンチ判定用）
    readonly int StatePunch1 = Animator.StringToHash("Mawaru_Punch");
    readonly int StatePunch2 = Animator.StringToHash("Mawaru_Punch2");
    readonly int StatePunch3 = Animator.StringToHash("Mawaru_Punch3");

    static bool HasParam(Animator a, int hash)
    {
        foreach (var p in a.parameters)
            if (p.nameHash == hash) return true;
        return false;
    }

    bool IsCarryMode() => unlockDoubleJumpByJuice && enableCarryDoubleJump && equipment != null && equipment.IsCarryActive;

    bool CanUseDash() => unlockDashByJuice;
    bool CanUseSlide() => unlockSlideByJuice;
    bool CanUseUpPunch() => unlockUpPunchByJuice;
    bool CanUseRocketPunch() => enableRocketPunch && unlockRocketPunchByJuice;

    int AllowedMaxJumpCount()
    {
        if (!IsCarryMode()) return 1;
        return Mathf.Max(1, carryMaxJumpCount);
    }

    bool IsDashAnimating()
    {
        return dashTimer > 0f
            && !isSliding
            && !isHanging
            && Mathf.Abs(axisH) >= 0.1f;
    }

    void SetDashAnimatorBool()
    {
        if (hasDashParam)
            animator.SetBool(HashDash, IsDashAnimating());
    }

    bool IsUpButtonHeldNow()
    {
        if (Gamepad.current != null && Gamepad.current.dpad.up.isPressed)
            return true;

        if (Keyboard.current != null &&
            (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed))
            return true;

        return false;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        normalGravity = rb.gravityScale;
        normalDrag = rb.drag;

        equipment = GetComponent<MawaruEquipment>();
        hasIsAttackingParam = HasParam(animator, HashIsAttacking);
        hasDashParam = HasParam(animator, HashDash);

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        if (!hpBar) hpBar = FindObjectOfType<HpBarController>();
        hpBar?.SetHp(currentHP, maxHP);
        OnHpChanged?.Invoke(currentHP, maxHP);

        wasGrounded = IsGrounded();
        jumpCountUsed = 0;

        if (punchHitbox)
        {
            punchBox = punchHitbox.GetComponent<BoxCollider2D>();
            if (punchBox) punchBox.size = jabBoxSize;

            punchCol = punchHitbox.GetComponent<Collider2D>();
            if (punchCol) punchCol.enabled = false;

            var punchHitboxRenderer = punchHitbox.GetComponent<SpriteRenderer>();
            if (punchHitboxRenderer)
                punchHitboxRenderer.enabled = false;

            punchLocalDefault = punchHitbox.transform.localPosition;
        }

        SetDashAnimatorBool();
    }

    void Update()
    {
        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        if (wallDashLock > 0f) wallDashLock -= Time.deltaTime;

        if (slideCooldownTimer > 0f) slideCooldownTimer -= Time.deltaTime;

        if (dashTimer > 0f) dashTimer -= Time.deltaTime;
        if (slideTimer > 0f) slideTimer -= Time.deltaTime;

        if (dashTimer < 0f) dashTimer = 0f;
        if (slideTimer < 0f) slideTimer = 0f;

        if (isSliding && slideTimer <= 0f)
        {
            isSliding = false;
            animator.SetBool(HashSlide, false);
        }

        bool grounded = IsGrounded();

        if (grounded && !wasGrounded)
        {
            jumpCountUsed = 0;
            animator.ResetTrigger(HashJump2);
        }
        wasGrounded = grounded;

        animator.SetBool(HashGrounded, grounded);
        animator.SetFloat(HashSpeed, Mathf.Abs(rb.velocity.x));
        SetDashAnimatorBool();

        if (punchTimer > 0f) punchTimer -= Time.deltaTime;

        SyncPunchWindow();

        if (ropePressed)
        {
            ropePressed = false;

            if (currentRopeHead)
            {
                var rh = currentRopeHead.GetComponent<RopeHead>();
                if (rh != null && rh.IsGrabbing)
                {
                    Vector2 dir = GetRopeDirection();
                    if (dir == Vector2.zero) dir = (sr.flipX ? Vector2.left : Vector2.right);
                    if (rh.TryThrowGrabbed(dir)) return;
                }
            }

            if (!ropeShot && !isHanging)
            {
                FireRope();
            }
            else if (ropeShot && !isHanging)
            {
                CancelRopeShot();
            }
        }

        bool pressLeft = axisH < -0.1f;
        bool pressRight = axisH > 0.1f;

        isTouchingWallLeft = wallCheckLeft &&
            Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckDistance, wallLayer);
        isTouchingWallRight = wallCheckRight &&
            Physics2D.OverlapCircle(wallCheckRight.position, wallCheckDistance, wallLayer);

        isWallSliding = false;
        if ((isTouchingWallLeft || isTouchingWallRight)
            && !grounded
            && (pressLeft || pressRight)
            && wallDashLock <= 0f
            && !isHanging
            && !isSliding)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
        }

        if (punchCol) punchCol.enabled = punchVisible;
        UpdatePunchHitboxTransform();

        if (CanUseRocketPunch())
        {
            if (rocketCooldownTimer > 0f) rocketCooldownTimer -= Time.deltaTime;

            if (punchHeld && !rocketTriggered && !rocketActive && rocketCooldownTimer <= 0f)
            {
                if (Time.time - punchHoldStart >= rocketChargeSeconds)
                {
                    rocketTriggered = true;
                    StartRocketPunch();
                }
            }
        }

        if (punchPressed)
        {
            punchPressed = false;
            TryPunch(false);
        }
    }

    void FixedUpdate()
    {
        if (knockbackTimer > 0f)
        {
            rb.velocity = knockbackVelocity;
            knockbackTimer -= Time.fixedDeltaTime;
            return;
        }

        if (isHanging)
        {
            ApplyAutoSwing();

            if (hangJumpRequested)
            {
                hangJumpRequested = false;
                JumpFromHang();
            }
            return;
        }

        if (isSliding)
        {
            if (!IsGrounded())
            {
                isSliding = false;
                slideTimer = 0f;
                animator.SetBool(HashSlide, false);
            }
            else
            {
                float vxSlide = slideDir * slideSpeed;
                rb.velocity = new Vector2(vxSlide, rb.velocity.y);
                sr.flipX = vxSlide < 0f;
                return;
            }
        }

        float targetSpeed = (dashTimer > 0f) ? dashSpeed : speed;
        float vx = axisH * targetSpeed;

        if (isWallSliding)
            rb.velocity = new Vector2(vx, Mathf.Max(rb.velocity.y, -wallSlideSpeed));
        else
            rb.velocity = new Vector2(vx, rb.velocity.y);

        if (jumpPressed)
        {
            jumpPressed = false;

            bool grounded = IsGrounded();

            if (grounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpCountUsed = 1;
                animator.ResetTrigger(HashJump2);
            }
            else if (isWallSliding || isTouchingWallLeft || isTouchingWallRight)
            {
                float dir = 0f;
                if (isTouchingWallRight && !isTouchingWallLeft) dir = -1f;
                else if (isTouchingWallLeft && !isTouchingWallRight) dir = 1f;
                else dir = (sr.flipX ? -1f : 1f);

                rb.velocity = Vector2.zero;
                rb.AddForce(new Vector2(dir * wallDashPower, jumpForce), ForceMode2D.Impulse);
                wallDashLock = 0.2f;

                if (jumpCountUsed < 1) jumpCountUsed = 1;
            }
            else
            {
                int allowed = AllowedMaxJumpCount();
                if (IsCarryMode() && jumpCountUsed >= 1 && jumpCountUsed < allowed)
                {
                    rb.velocity = new Vector2(rb.velocity.x, 0f);
                    rb.AddForce(Vector2.up * secondJumpForce, ForceMode2D.Impulse);
                    animator.SetTrigger(HashJump2);
                    jumpCountUsed++;
                }
            }
        }

        if (Mathf.Abs(rb.velocity.x) > 0.01f) sr.flipX = rb.velocity.x < 0f;
    }

    // ===== Input =====
    public void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 v = ctx.ReadValue<Vector2>();
        axisH = v.x;
        axisV = v.y;

        bool edgeH = Mathf.Abs(prevAxisH) < 0.2f && Mathf.Abs(v.x) >= 0.8f;
        if (edgeH)
        {
            int dir = (v.x > 0f) ? 1 : -1;
            float now = Time.unscaledTime;

            if (CanUseDash() && dir == lastTapDir && (now - lastTapTime) <= doubleTapWindow)
            {
                dashTimer = dashKeepTime;
                SetDashAnimatorBool();
            }

            lastTapDir = dir;
            lastTapTime = now;
        }
        prevAxisH = v.x;

        bool edgeDown = Mathf.Abs(prevAxisV) < 0.2f && v.y <= -0.8f;
        if (edgeDown)
        {
            float now = Time.unscaledTime;

            if (CanUseSlide() && IsGrounded() && slideCooldownTimer <= 0f && !isHanging)
            {
                if ((now - lastDownTapTime) <= slideDoubleTapWindow)
                {
                    StartSlide();
                    lastDownTapTime = -999f;
                }
                else
                {
                    lastDownTapTime = now;
                }
            }
        }
        prevAxisV = v.y;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        if (isHanging)
        {
            hangJumpRequested = true;
            return;
        }

        if (isSliding) return;
        jumpPressed = true;
    }

    public void OnRope(InputAction.CallbackContext ctx)
    {
        RopeHead rh = null;
        bool isGrabbingEnemy = false;

        if (currentRopeHead)
        {
            rh = currentRopeHead.GetComponent<RopeHead>();
            isGrabbingEnemy = rh != null && rh.IsGrabbing;
        }

        // 敵を掴んでいる時だけ、ボタンを離した瞬間に投げる（旧仕様を維持）
        if (ctx.canceled)
        {
            if (isGrabbingEnemy)
                ropePressed = true;
            return;
        }

        if (!ctx.started && !ctx.performed) return;
        if (isHanging) return;

        // ぶら下がりは維持したまま、敵投げだけ旧来どおり「離した時」発動にする
        if (isGrabbingEnemy) return;

        ropePressed = true;
    }

    public void OnSwitch(InputAction.CallbackContext ctx)
    {
        if (ctx.performed) FindObjectOfType<CharacterSwitchManager>()?.ToggleControl();
    }

    public void OnWeapon(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (equipment) equipment.ToggleWeapon();
    }

    public void OnPunch(InputAction.CallbackContext ctx)
    {
        if (isSliding) return;

        // 押した瞬間を started / performed の両方に対応させる
        if ((ctx.started || ctx.performed) && !punchPressLatched)
        {
            punchPressLatched = true;
            consumedPunchOnStarted = false;

            // 上パンチ解禁後のみ、上ボタンを押している間にパンチを押した瞬間だけ Punch3
            if (CanUseUpPunch() && IsUpButtonHeldNow())
            {
                punchHeld = false;
                rocketTriggered = false;
                consumedPunchOnStarted = TryPunch(true);
                return;
            }

            punchHeld = true;
            punchHoldStart = Time.time;
            rocketTriggered = false;
            return;
        }

        if (ctx.canceled)
        {
            punchPressLatched = false;

            if (consumedPunchOnStarted)
            {
                consumedPunchOnStarted = false;
                punchHeld = false;
                return;
            }

            punchHeld = false;
            if (!rocketTriggered)
                punchPressed = true;
        }
    }

    public void OnMenu(InputAction.CallbackContext ctx) { }

    // ===== Slide =====
    void StartSlide()
    {
        if (!CanUseSlide()) return;
        if (!IsGrounded()) return;
        if (slideCooldownTimer > 0f) return;
        if (isSliding) return;

        isSliding = true;
        slideTimer = slideKeepTime;
        slideCooldownTimer = slideCooldown;

        if (axisH > 0.2f) slideDir = 1;
        else if (axisH < -0.2f) slideDir = -1;
        else slideDir = sr.flipX ? -1 : 1;

        animator.SetBool(HashSlide, true);
        dashTimer = 0f;
        SetDashAnimatorBool();
    }

    // ===== Punch helpers =====
    bool TryPunch(bool forcePunch3)
    {
        if (punchTimer > 0f) return false;
        if (isHanging) return false;
        if (rocketActive) return false;
        if (isSliding) return false;

        punchTimer = punchCooldown;

        if (forcePunch3)
        {
            comboStep = 0;
            currentPunchKind = PunchKind.Up;

            // Triggerではなく直接再生して、Idle/Walkから確実に入れる
            animator.ResetTrigger(HashPunch);
            animator.ResetTrigger(HashPunch2);
            animator.ResetTrigger(HashPunch3);
            animator.Play("Mawaru_Punch3", 0, 0f);
        }
        else
        {
            if (!wasInPunch) comboStep = 0;

            comboStep = Mathf.Clamp(comboStep + 1, 1, 2);
            currentPunchKind = PunchKind.Jab;

            if (comboStep == 1) animator.SetTrigger(HashPunch);
            else animator.SetTrigger(HashPunch2);
        }

        if (hasIsAttackingParam) animator.SetBool(HashIsAttacking, true);

        if (punchVoiceSource && punchVoices != null && punchVoices.Length > 0)
        {
            var clip = punchVoices[UnityEngine.Random.Range(0, punchVoices.Length)];
            if (clip) punchVoiceSource.PlayOneShot(clip, punchVoiceVolume);
        }

        return true;
    }

    void SyncPunchWindow()
    {
        if (!punchHitbox || animator == null)
        {
            punchVisible = false;
            wasInPunch = false;
            return;
        }

        var st = animator.GetCurrentAnimatorStateInfo(0);
        bool inPunch = st.shortNameHash == StatePunch1 || st.shortNameHash == StatePunch2 || st.shortNameHash == StatePunch3;

        if (!inPunch)
        {
            punchVisible = false;
            wasInPunch = false;
            if (hasIsAttackingParam) animator.SetBool(HashIsAttacking, false);
            return;
        }

        float t = st.normalizedTime % 1f;
        HitWindow01 w = punch1Window;
        if (st.shortNameHash == StatePunch2) w = punch2Window;
        else if (st.shortNameHash == StatePunch3) w = punch3Window;

        punchVisible = t >= w.start && t <= w.end;
        wasInPunch = true;

        punchNormT = t;
        punchCurrentWindow = w;
    }

    void UpdatePunchHitboxTransform()
    {
        if (!punchHitbox) return;

        if (!punchVisible)
        {
            if (punchBox) punchBox.size = jabBoxSize;
            punchHitbox.transform.localPosition = punchLocalDefault;
            return;
        }

        float facing = sr.flipX ? -1f : 1f;
        float p = Mathf.InverseLerp(punchCurrentWindow.start, punchCurrentWindow.end, punchNormT);
        p = Mathf.Clamp01(p);
        float e = EvalExtend01(p);

        Vector3 add = Vector3.zero;
        Vector2 boxSize = jabBoxSize;

        if (currentPunchKind == PunchKind.Straight)
        {
            boxSize = straightBoxSize;
            add = new Vector3(facing * (Mathf.Abs(punchOffsetX) + straightExtend * e), 0f, 0f);
        }
        else if (currentPunchKind == PunchKind.Up)
        {
            boxSize = upBoxSize;
            add = new Vector3(facing * upForward, upExtend * e, 0f);
        }
        else
        {
            boxSize = jabBoxSize;
            add = new Vector3(facing * (Mathf.Abs(punchOffsetX) + jabExtend * e), 0f, 0f);
        }

        if (punchBox) punchBox.size = boxSize;
        punchHitbox.transform.localPosition = punchLocalDefault + add;
    }

    // ===== Rope =====
    Vector2 GetRopeDirection()
    {
        Vector2 dir = new Vector2(axisH, axisV);

        if (axisV > 0.2f)
        {
            if (axisH > 0.2f) dir = new Vector2(1f, 1f);
            else if (axisH < -0.2f) dir = new Vector2(-1f, 1f);
            else dir = Vector2.up;
        }
        else
        {
            if (axisH > 0.2f) dir = Vector2.right;
            else if (axisH < -0.2f) dir = Vector2.left;
            else dir = Vector2.zero;
        }

        return dir.normalized;
    }

    void FireRope()
    {
        if (!ropeHeadPrefab || ropeShot) return;
        if (isSliding) return;

        ropeShot = true;

        Vector2 shootDir = GetRopeDirection();
        if (shootDir == Vector2.zero) shootDir = (sr.flipX ? Vector2.left : Vector2.right);

        Vector3 spawnPos = (firePoint ? firePoint.position : transform.position) + (Vector3)(shootDir * 0.5f);
        var head = Instantiate(ropeHeadPrefab, spawnPos, Quaternion.identity);
        currentRopeHead = head;

        foreach (var rc in head.GetComponentsInChildren<Collider2D>())
            foreach (var mc in GetComponentsInChildren<Collider2D>())
                if (rc && mc) Physics2D.IgnoreCollision(rc, mc, true);

        var hrb = head.GetComponent<Rigidbody2D>();
        if (hrb) hrb.velocity = shootDir * ropeSpeed;

        head.transform.rotation = Quaternion.FromToRotation(Vector3.right, shootDir);

        var rh = head.GetComponent<RopeHead>();
        if (rh)
        {
            rh.ropeLength = ropeLength;
            rh.Init(GetComponent<Rigidbody2D>(), this);
        }
    }

    public void SetHanging(bool h)
    {
        isHanging = h;
        rb.gravityScale = isHanging ? hangGravity : normalGravity;
        rb.drag = isHanging ? hangDrag : normalDrag;

        if (isHanging)
        {
            hangSwingDir = sr.flipX ? -1 : 1;
            hangStartKickPending = true;
            jumpCountUsed = 1;
        }
        else
        {
            hangStartKickPending = false;
        }

        SetDashAnimatorBool();
    }

    public void OnRopeReturned()
    {
        ropeShot = false;
        currentRopeHead = null;
        isHanging = false;
        hangJumpRequested = false;
        hangStartKickPending = false;
        rb.gravityScale = normalGravity;
        rb.drag = normalDrag;
        SetDashAnimatorBool();
    }

    public void ReleaseHang()
    {
        var sj = GetComponent<SpringJoint2D>();
        if (sj) Destroy(sj);

        var dj = GetComponent<DistanceJoint2D>();
        if (dj) Destroy(dj);

        if (currentRopeHead) Destroy(currentRopeHead);
        OnRopeReturned();
    }

    void CancelRopeShot()
    {
        if (currentRopeHead) Destroy(currentRopeHead);
        OnRopeReturned();
    }

    void ApplyAutoSwing()
    {
        rb.gravityScale = hangGravity;
        rb.drag = hangDrag;

        var dj = GetComponent<DistanceJoint2D>();
        if (!dj)
            return;

        Vector2 anchor = dj.connectedAnchor;
        Vector2 radius = rb.position - anchor;
        if (radius.sqrMagnitude <= 0.0001f)
            return;

        float angle = Vector2.SignedAngle(Vector2.down, radius);
        if (angle >= autoSwingMaxAngle) hangSwingDir = -1;
        else if (angle <= -autoSwingMaxAngle) hangSwingDir = 1;

        Vector2 tangent = GetSwingTangent(radius, hangSwingDir);
        float tangentSpeed = Vector2.Dot(rb.velocity, tangent);

        if (hangStartKickPending)
        {
            hangStartKickPending = false;
            rb.velocity += tangent * autoSwingStartKick;
            tangentSpeed = Vector2.Dot(rb.velocity, tangent);
        }

        if (Mathf.Abs(tangentSpeed) < hangMaxSpeed)
            rb.AddForce(tangent * hangSwingForce, ForceMode2D.Force);

        float speed = rb.velocity.magnitude;
        if (speed > hangMaxSpeed)
            rb.velocity = rb.velocity.normalized * hangMaxSpeed;

        if (Mathf.Abs(rb.velocity.x) > 0.05f)
            sr.flipX = rb.velocity.x < 0f;
    }

    Vector2 GetSwingTangent(Vector2 radius, int dir)
    {
        radius.Normalize();
        Vector2 ccwTangent = new Vector2(-radius.y, radius.x);
        return (dir >= 0 ? ccwTangent : -ccwTangent).normalized;
    }

    void JumpFromHang()
    {
        Vector2 launchVelocity = rb.velocity;
        var dj = GetComponent<DistanceJoint2D>();

        if (dj)
        {
            Vector2 anchor = dj.connectedAnchor;
            Vector2 radius = rb.position - anchor;
            if (radius.sqrMagnitude > 0.0001f)
            {
                Vector2 tangent = GetSwingTangent(radius, hangSwingDir);
                float tangentSpeed = Vector2.Dot(launchVelocity, tangent);

                if (Mathf.Abs(tangentSpeed) > 0.05f)
                    tangent *= Mathf.Sign(tangentSpeed);

                float desiredSpeed = Mathf.Max(hangJumpForce, Mathf.Abs(tangentSpeed) + hangReleaseExtraSpeed);
                launchVelocity = tangent * desiredSpeed;
            }
        }

        ReleaseHang();
        rb.velocity = launchVelocity;

        if (Mathf.Abs(rb.velocity.x) > 0.05f)
            sr.flipX = rb.velocity.x < 0f;
    }

    // ===== Ground =====
    bool IsGrounded()
    {
        if (!groundCheck) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
    }

    // ===== Damage =====
    public void TakeDamage(int dmg, int knockDir = 0)
    {
        if (currentRopeHead)
        {
            var rh = currentRopeHead.GetComponent<RopeHead>();
            if (rh != null && rh.IsGrabbing) return;
        }

        if (invincibleTimer > 0f) return;
        if (currentHP <= 0) return;

        isSliding = false;
        slideTimer = 0f;
        dashTimer = 0f;
        animator.SetBool(HashSlide, false);
        SetDashAnimatorBool();

        currentHP = Mathf.Clamp(currentHP - dmg, 0, maxHP);
        animator.SetTrigger(HashDamage);

        if (voiceSource && damageVoice) voiceSource.PlayOneShot(damageVoice, voiceVolume);

        hpBar?.SetHp(currentHP, maxHP);
        OnHpChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
        {
            rb.velocity = Vector2.zero;
            SceneManager.LoadScene("Resuit");
            return;
        }

        if (knockDir == 0) knockDir = sr.flipX ? 1 : -1;
        knockbackTimer = knockbackDuration;
        knockbackVelocity = new Vector2(
            Mathf.Sign(knockDir) * Mathf.Abs(knockbackVelocity.x),
            knockbackVelocity.y
        );
        invincibleTimer = invincibleTime;
    }

    // ===== Rocket Punch =====
    void StartRocketPunch()
    {
        if (!rocketPunchPrefab)
        {
            rocketTriggered = false;
            return;
        }
        if (rocketActive) return;

        rocketActive = true;

        if (rocketCo != null) StopCoroutine(rocketCo);
        rocketCo = StartCoroutine(RocketPunchRoutine());
    }

    IEnumerator RocketPunchRoutine()
    {
        Vector3 spawnPos = rocketSpawn ? rocketSpawn.position : transform.position;
        GameObject hand = Instantiate(rocketPunchPrefab, spawnPos, Quaternion.identity);

        float outT = 0f;
        Vector3 start = spawnPos;
        Vector3 end = spawnPos + (sr.flipX ? Vector3.left : Vector3.right) * rocketOutDistance;

        while (outT < 1f)
        {
            outT += Time.deltaTime * rocketOutSpeed / Mathf.Max(0.0001f, rocketOutDistance);
            if (hand) hand.transform.position = Vector3.Lerp(start, end, outT);
            yield return null;
        }

        float retT = 0f;
        Vector3 retStart = hand ? hand.transform.position : end;
        Vector3 retEnd = rocketSpawn ? rocketSpawn.position : transform.position;

        while (retT < 1f)
        {
            retT += Time.deltaTime * rocketReturnSpeed / Mathf.Max(0.0001f, rocketOutDistance);
            if (hand) hand.transform.position = Vector3.Lerp(retStart, retEnd, retT);
            yield return null;
        }

        if (hand) Destroy(hand);

        rocketActive = false;
        rocketCooldownTimer = rocketCooldown;
    }

    // ===== Contact Damage =====
    bool IsDamageTarget(GameObject other)
    {
        if (contactDamageLayers.value != 0)
        {
            int mask = 1 << other.layer;
            if ((contactDamageLayers.value & mask) != 0) return true;
        }
        if (other.CompareTag("Enemy") || other.CompareTag("Boss")) return true;
        return false;
    }

    int GetKnockDir(Collider2D other)
    {
        float dx = other.bounds.center.x - transform.position.x;
        return (dx >= 0f) ? -1 : 1;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsDamageTarget(other.gameObject)) return;
        TakeDamage(contactDamage, GetKnockDir(other));
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsDamageTarget(col.collider.gameObject)) return;
        TakeDamage(contactDamage, GetKnockDir(col.collider));
    }

    float EvalExtend01(float p)
    {
        float outEnd = Mathf.Clamp01(punchOutEnd);
        float holdEnd = Mathf.Clamp01(punchHoldEnd);
        if (holdEnd < outEnd) holdEnd = outEnd;

        if (p < outEnd) return (outEnd <= 0.0001f) ? 1f : (p / outEnd);
        if (p < holdEnd) return 1f;
        return 1f - (p - holdEnd) / Mathf.Max(0.0001f, 1f - holdEnd);
    }


    public int RecoverHP(int amount)
    {
        if (amount <= 0) return 0;
        if (currentHP <= 0) return 0;

        int before = currentHP;
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        int healed = currentHP - before;

        hpBar?.SetHp(currentHP, maxHP);
        OnHpChanged?.Invoke(currentHP, maxHP);
        return healed;
    }

    public void SetDashUnlocked(bool unlocked)
    {
        unlockDashByJuice = unlocked;
        if (!unlockDashByJuice)
        {
            dashTimer = 0f;
            SetDashAnimatorBool();
        }
    }

    public void SetSlideUnlocked(bool unlocked)
    {
        unlockSlideByJuice = unlocked;
        if (!unlockSlideByJuice)
        {
            isSliding = false;
            slideTimer = 0f;
            slideCooldownTimer = 0f;
            animator.SetBool(HashSlide, false);
        }
    }

    public void SetUpPunchUnlocked(bool unlocked)
    {
        unlockUpPunchByJuice = unlocked;
    }

    public void SetDoubleJumpUnlocked(bool unlocked)
    {
        unlockDoubleJumpByJuice = unlocked;
        if (!unlockDoubleJumpByJuice)
        {
            jumpCountUsed = Mathf.Min(jumpCountUsed, 1);
        }
    }

    public void SetRocketPunchUnlocked(bool unlocked)
    {
        unlockRocketPunchByJuice = unlocked;

        if (!CanUseRocketPunch())
        {
            punchHeld = false;
            rocketTriggered = false;
            rocketCooldownTimer = 0f;

            if (rocketCo != null)
            {
                StopCoroutine(rocketCo);
                rocketCo = null;
            }

            rocketActive = false;
        }
    }

    public void ApplyAllJuiceUnlocks(bool dashUnlocked, bool upPunchUnlocked, bool slideUnlocked, bool doubleJumpUnlocked, bool rocketPunchUnlocked)
    {
        SetDashUnlocked(dashUnlocked);
        SetUpPunchUnlocked(upPunchUnlocked);
        SetSlideUnlocked(slideUnlocked);
        SetDoubleJumpUnlocked(doubleJumpUnlocked);
        SetRocketPunchUnlocked(rocketPunchUnlocked);
    }

    public void GrantIFrames(float seconds)
    {
        if (seconds <= 0f) return;
        invincibleTimer = Mathf.Max(invincibleTimer, seconds);
    }
}