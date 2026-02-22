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

    // ===== Double Jump (Carry Only) =====
    [Header("Double Jump (Carry Only)")]
    public bool enableCarryDoubleJump = true;
    public float secondJumpForce = 9f;
    public int carryMaxJumpCount = 2;

    int jumpCountUsed = 0;
    bool wasGrounded;

    // ===== Punch =====
    [Header("Punch - Hitbox (Animator Sync)")]
    public GameObject punchHitbox;
    public float punchCooldown = 0.20f;

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
    public HitWindow01 punch1Window = new HitWindow01 { start = 0.10f, end = 0.55f };
    public HitWindow01 punch2Window = new HitWindow01 { start = 0.10f, end = 0.60f };
    public HitWindow01 punch3Window = new HitWindow01 { start = 0.10f, end = 0.70f };

    float punchTimer;
    Collider2D punchCol;
    SpriteRenderer punchSR;
    bool punchVisible;
    Vector3 punchLocalDefault;
    int comboStep = 0;
    bool wasInPunch;

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

    // Animator hashes
    readonly int HashSpeed = Animator.StringToHash("Speed");        // float
    readonly int HashGrounded = Animator.StringToHash("Grounded");  // bool
    readonly int HashPunch = Animator.StringToHash("Punch");
    readonly int HashPunch2 = Animator.StringToHash("Punch2");
    readonly int HashPunch3 = Animator.StringToHash("Punch3");
    readonly int HashDamage = Animator.StringToHash("Damage");
    readonly int HashIsAttacking = Animator.StringToHash("IsAttacking");
    readonly int HashJump2 = Animator.StringToHash("Jump2");
    readonly int HashSlide = Animator.StringToHash("Slide"); // あなたのAnimatorにあるbool想定

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

    bool IsCarryMode() => enableCarryDoubleJump && equipment != null && equipment.IsCarryActive;

    int AllowedMaxJumpCount()
    {
        if (!IsCarryMode()) return 1;
        return Mathf.Max(1, carryMaxJumpCount);
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

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        if (!hpBar) hpBar = FindObjectOfType<HpBarController>();
        hpBar?.SetHp(currentHP, maxHP);
        OnHpChanged?.Invoke(currentHP, maxHP);

        wasGrounded = IsGrounded();
        jumpCountUsed = 0;

        if (punchHitbox)
        {
            punchCol = punchHitbox.GetComponent<Collider2D>();
            if (punchCol) punchCol.enabled = false;

            punchSR = punchHitbox.GetComponent<SpriteRenderer>();
            if (punchSR)
            {
                punchSR.enabled = false;
                punchSR.sortingLayerID = sr.sortingLayerID;
                punchSR.sortingOrder = sr.sortingOrder + 1;
            }

            punchLocalDefault = punchHitbox.transform.localPosition;
        }
    }

    void Update()
    {
        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;
        if (wallDashLock > 0f) wallDashLock -= Time.deltaTime;

        if (slideCooldownTimer > 0f) slideCooldownTimer -= Time.deltaTime;

        // タイマー減算
        if (dashTimer > 0f) dashTimer -= Time.deltaTime;
        if (slideTimer > 0f) slideTimer -= Time.deltaTime;

        // Slide終了
        if (isSliding && slideTimer <= 0f)
        {
            isSliding = false;
            animator.SetBool(HashSlide, false);
        }

        bool grounded = IsGrounded();

        // 着地でジャンプ回数リセット
        if (grounded && !wasGrounded)
        {
            jumpCountUsed = 0;
            animator.ResetTrigger(HashJump2);
        }
        wasGrounded = grounded;

        // ---- Animatorへ反映 ----
        animator.SetBool(HashGrounded, grounded);

        // ★重要：Speed は「入力」じゃなく「実速度」を入れる（Dash条件 Speed>6 を満たすため）
        animator.SetFloat(HashSpeed, Mathf.Abs(rb.velocity.x));

        // Punch timer
        if (punchTimer > 0f) punchTimer -= Time.deltaTime;

        SyncPunchWindow();

        // Rope input
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

            if (!ropeShot && !isHanging) FireRope();
            else ReleaseHang();
        }

        // Wall slide detect（Slide中は入らない）
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

        if (punchSR) punchSR.enabled = punchVisible;
        if (punchCol) punchCol.enabled = punchVisible;

        if (punchHitbox)
        {
            var lp = punchLocalDefault;
            lp.x = Mathf.Abs(punchOffsetX) * (sr.flipX ? -1f : 1f);
            punchHitbox.transform.localPosition = lp;
        }

        // Rocket Punch (charge)
        if (enableRocketPunch)
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
            TryPunch();
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

        // Hanging
        if (isHanging)
        {
            rb.gravityScale = hangGravity;

            if (Mathf.Abs(axisH) > 0.01f)
                rb.AddForce(new Vector2(axisH * hangSwingForce, 0f), ForceMode2D.Force);

            var v = rb.velocity;
            v.x = Mathf.Clamp(v.x, -hangMaxSpeed, hangMaxSpeed);
            rb.velocity = v;

            if (hangJumpRequested)
            {
                hangJumpRequested = false;
                ReleaseHang();
                rb.velocity = new Vector2(rb.velocity.x, 0f);
                rb.AddForce(Vector2.up * hangJumpForce, ForceMode2D.Impulse);
            }
            return;
        }

        // Slide中（地上のみ）
        if (isSliding)
        {
            // 崖で落ちたら即終了（空中スライド禁止の保証）
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

        // 通常/ダッシュ移動
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

        // ---- Dash（左右2回） ----
        bool edgeH = Mathf.Abs(prevAxisH) < 0.2f && Mathf.Abs(v.x) >= 0.8f;
        if (edgeH)
        {
            int dir = (v.x > 0f) ? 1 : -1;
            float now = Time.unscaledTime;
            if (dir == lastTapDir && (now - lastTapTime) <= doubleTapWindow)
                dashTimer = dashKeepTime;

            lastTapDir = dir;
            lastTapTime = now;
        }
        prevAxisH = v.x;

        // ---- Slide（↓を2回：地上のみ） ----
        bool edgeDown = Mathf.Abs(prevAxisV) < 0.2f && v.y <= -0.8f;
        if (edgeDown)
        {
            float now = Time.unscaledTime;

            // ★空中ではカウントすらしない（空中禁止を安定化）
            if (IsGrounded() && slideCooldownTimer <= 0f && !isHanging)
            {
                if ((now - lastDownTapTime) <= slideDoubleTapWindow)
                {
                    StartSlide();
                    lastDownTapTime = -999f; // 消費
                }
                else
                {
                    lastDownTapTime = now;   // 1回目
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

        if (isSliding) return; // スライド中ジャンプ禁止（必要なら外してOK）
        jumpPressed = true;
    }

    public void OnRope(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled)
        {
            ropePressed = true;
            return;
        }

        if (ctx.performed)
        {
            if (currentRopeHead)
            {
                var rh = currentRopeHead.GetComponent<RopeHead>();
                if (rh != null && rh.IsGrabbing) return;
            }
            ropePressed = true;
        }
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

        if (ctx.started)
        {
            punchHeld = true;
            punchHoldStart = Time.time;
            rocketTriggered = false;
            return;
        }

        if (ctx.canceled)
        {
            punchHeld = false;
            if (!rocketTriggered)
                punchPressed = true;
        }
    }

    public void OnMenu(InputAction.CallbackContext ctx) { }

    // ===== Slide =====
    void StartSlide()
    {
        // ★地上限定
        if (!IsGrounded()) return;
        if (slideCooldownTimer > 0f) return;
        if (isSliding) return;

        isSliding = true;
        slideTimer = slideKeepTime;
        slideCooldownTimer = slideCooldown;

        // 向き固定：入力があればそれ、なければ現在向き
        if (axisH > 0.2f) slideDir = 1;
        else if (axisH < -0.2f) slideDir = -1;
        else slideDir = sr.flipX ? -1 : 1;

        // AnimatorのSlide(bool)をON（AnyState→Slide が確実に入る）
        animator.SetBool(HashSlide, true);

        // ダッシュは切る（好みで）
        dashTimer = 0f;
    }

    // ===== Punch helpers =====
    void TryPunch()
    {
        if (punchTimer > 0f) return;
        if (isHanging) return;
        if (rocketActive) return;
        if (isSliding) return;

        punchTimer = punchCooldown;

        if (!wasInPunch) comboStep = 0;
        comboStep = Mathf.Clamp(comboStep + 1, 1, 3);

        if (comboStep == 1) animator.SetTrigger(HashPunch);
        else if (comboStep == 2) animator.SetTrigger(HashPunch2);
        else animator.SetTrigger(HashPunch3);

        if (hasIsAttackingParam) animator.SetBool(HashIsAttacking, true);

        if (punchVoiceSource && punchVoices != null && punchVoices.Length > 0)
        {
            var clip = punchVoices[UnityEngine.Random.Range(0, punchVoices.Length)];
            if (clip) punchVoiceSource.PlayOneShot(clip, punchVoiceVolume);
        }
    }

    void SyncPunchWindow()
    {
        if (!punchHitbox || animator == null) { punchVisible = false; wasInPunch = false; return; }

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
        if (rh) { rh.ropeLength = ropeLength; rh.Init(GetComponent<Rigidbody2D>(), this); }
    }

    public void SetHanging(bool h)
    {
        isHanging = h;
        rb.gravityScale = isHanging ? hangGravity : normalGravity;
        rb.drag = isHanging ? hangDrag : normalDrag;
    }

    public void OnRopeReturned()
    {
        ropeShot = false;
        currentRopeHead = null;
        isHanging = false;
        hangJumpRequested = false;
        rb.gravityScale = normalGravity;
        rb.drag = normalDrag;
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

        // Slide解除
        isSliding = false;
        slideTimer = 0f;
        animator.SetBool(HashSlide, false);

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
}