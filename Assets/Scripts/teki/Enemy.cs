using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [Header("HP")]
    public int hp = 3;
    public int maxHp = 3;

    // Rope用
    public bool isGrabbed = false;
    public bool isFlying = false;

    public EnemyHpBarController hpBar;

    [Header("Thrown Collision")]
    public LayerMask thrownHitLayers;     // Ground + Wall
    public LayerMask landingLayers;       // Ground only

    [Header("Bounce Tuning")]
    public float reboundMul = 1.3f;
    public float minReboundSpeed = 6.0f;

    [Tooltip("地面っぽい法線（normal.yが大きい）に当たった時、最低でもこの上向き速度を保証する")]
    public float minUpSpeedOnGround = 4.0f;

    public float minAirTimeAfterBounce = 0.06f;
    public float groundCheckRadius = 0.08f;

    [Header("Anti-Stick")]
    public float depenetrationSkin = 0.03f;
    public float stuckSpeedThreshold = 0.5f;

    Rigidbody2D rb;
    Collider2D myCol;
    Coroutine landCo;
    bool bouncedOnce = false;

    // ★衝突「直前」の速度を保持（ここが横滑り解消の肝）
    Vector2 lastFlyingVelocity = Vector2.zero;

    // ---------------- Shell Stun ----------------
    [Header("Shell Stun")]
    [SerializeField] float shellStunDefaultSeconds = 1.0f;

    float _shellStunEndTime = -1f;
    bool _shellStunned = false;

    // RopeHead から参照する用（スタン中だけ掴む/投げる判定）
    public bool IsShellStunned => _shellStunned && Time.time < _shellStunEndTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (hpBar) hpBar.SetHp(hp, maxHp);
    }

    // ===== ダメージ =====
    public void TakeDamage(int amount, string cause = "other")
    {
        // 掴み中（投げ飛行じゃない）に被弾無効にしたいならこのまま
        if (isGrabbed && !isFlying) return;

        hp -= amount;
        if (hpBar) hpBar.SetHp(hp, maxHp);

        if (hp <= 0) Die(cause);
    }

    void Die(string cause)
    {
        Destroy(gameObject);
    }

    // ===== Rope投げ開始 =====
    public void BeginThrow()
    {
        isFlying = true;
        bouncedOnce = false;

        // 投げ飛行中はAIが速度を触らないように止める
        SetEnemyAIMoveEnabled(false);

        // 投げた瞬間にスタンの「速度0固定」が邪魔しないよう解除
        ClearShellStun();

        lastFlyingVelocity = rb ? rb.velocity : Vector2.zero;

        if (landCo != null) StopCoroutine(landCo);
        landCo = null;
    }

    // ★スタン解除（AI再開は “掴み/飛行じゃない時だけ”）
    public void ClearShellStun()
    {
        if (!_shellStunned) return;

        _shellStunned = false;
        _shellStunEndTime = -1f;

        if (!isGrabbed && !isFlying)
            SetEnemyAIMoveEnabled(true);
    }

    // ===== Shell スタン（最後に当たった時から seconds 秒：上書き）=====
    public void ApplyShellStun(float seconds)
    {
        if (seconds <= 0f) seconds = shellStunDefaultSeconds;

        _shellStunEndTime = Time.time + seconds;

        if (!_shellStunned)
        {
            _shellStunned = true;
            SetEnemyAIMoveEnabled(false);
        }

        // 速度0は「その瞬間だけ」OK
        if (rb)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void FixedUpdate()
    {
        // ★飛行中は「衝突直前の速度」を毎FixedUpdateで更新しておく
        if (isFlying && rb)
        {
            lastFlyingVelocity = rb.velocity;
        }

        // スタンで止めるのは「通常行動中だけ」
        if (_shellStunned && !isFlying && !isGrabbed && rb)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // スタン終了
        if (_shellStunned && Time.time >= _shellStunEndTime)
        {
            _shellStunned = false;

            if (!isGrabbed && !isFlying)
                SetEnemyAIMoveEnabled(true);
        }
    }

    void SetEnemyAIMoveEnabled(bool enabled)
    {
        var straight = GetComponent<EnemyStraightMouth>();
        if (straight) straight.enabled = enabled;

        var floatShooter = GetComponent<EnemyFloatShooter>();
        if (floatShooter) floatShooter.enabled = enabled;

        var boomerang = GetComponent<EnemyBoomerangThrower>();
        if (boomerang) boomerang.enabled = enabled;
    }

    // ========= 衝突入口 =========
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isFlying) return;

        Vector2 p = transform.position;
        Vector2 cp = other.ClosestPoint(p);
        Vector2 n = (p - cp);
        if (n.sqrMagnitude < 0.0001f) n = Vector2.up;
        n.Normalize();

        HandleFlyingHit(other.gameObject, n);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!isFlying) return;

        Vector2 n = col.contactCount > 0 ? col.GetContact(0).normal : Vector2.up;
        HandleFlyingHit(col.gameObject, n);

        // 貼り付き対策：速度が極小で接触したら少し押し出す
        if (rb && rb.velocity.magnitude <= stuckSpeedThreshold)
        {
            rb.position += n * depenetrationSkin;
        }
    }

    // ========= 投げ飛行ヒット本体 =========
    void HandleFlyingHit(GameObject hitObj, Vector2 normal)
    {
        if (!isFlying || rb == null) return;

        // 他の敵に当たった：その場停止しないで「行動再開」させる
        if (hitObj.CompareTag("Enemy"))
        {
            var otherEnemy = hitObj.GetComponentInParent<Enemy>();
            if (otherEnemy != null && otherEnemy != this)
                otherEnemy.TakeDamage(1, "throw_hit");

            // 自分もダメージ（必要なら消してOK）
            TakeDamage(1, "throw");

            // ★ここが要望対応：停止せず即再開（HP残ってたら）
            EndFlyingAndResumeNow();
            return;
        }

        // 壁/床
        if ((thrownHitLayers.value & (1 << hitObj.layer)) != 0)
        {
            TakeDamage(1, "throw");

            // ★反射計算は「衝突直前の速度」でやる（横滑りの原因潰し）
            Vector2 v = lastFlyingVelocity;
            if (v.sqrMagnitude < 0.0001f) v = rb.velocity;

            // 1回目は反射
            if (!bouncedOnce)
            {
                bouncedOnce = true;

                Vector2 rv = Vector2.Reflect(v, normal);

                // 最低速度保証（弱すぎバウンド/貼り付き防止）
                float sp = rv.magnitude;
                if (sp < minReboundSpeed)
                    rv = (sp < 0.0001f ? normal : rv.normalized) * minReboundSpeed;

                // ★地面っぽい法線なら「上向き」を最低保証（斜め投げ→横滑り抑制）
                if (normal.y >= 0.6f && rv.y < minUpSpeedOnGround)
                    rv.y = minUpSpeedOnGround;

                rb.velocity = rv * reboundMul;

                // 少し押し出して“角で止まる”を減らす
                rb.position += normal * depenetrationSkin;

                StartLandingFlow();
            }
            else
            {
                // 2回目以降は着地待ちへ（ここは好みで調整OK）
                StartLandingFlow();
            }
        }
    }

    void EndFlyingAndResumeNow()
    {
        isFlying = false;
        isGrabbed = false;
        bouncedOnce = false;

        if (landCo != null)
        {
            StopCoroutine(landCo);
            landCo = null;
        }

        // Float系は基準点ズレ対策
        var floatShooter = GetComponent<EnemyFloatShooter>();
        if (floatShooter) floatShooter.ResetFloatBase();

        // スタン中じゃなくHP残ってたら再開
        if (!IsShellStunned && hp > 0)
            SetEnemyAIMoveEnabled(true);
    }

    // ========= 「地面に接してる時だけ」行動再開 =========
    void StartLandingFlow()
    {
        isFlying = true;
        isGrabbed = false;

        if (landCo != null) StopCoroutine(landCo);
        landCo = StartCoroutine(LandThenResume());
    }

    IEnumerator LandThenResume()
    {
        yield return new WaitForSeconds(minAirTimeAfterBounce);

        while (this && !IsTouchingLandingGround())
            yield return null;

        if (!this) yield break;

        isFlying = false;
        isGrabbed = false;

        var floatShooter = GetComponent<EnemyFloatShooter>();
        if (floatShooter) floatShooter.ResetFloatBase();

        if (!IsShellStunned && hp > 0)
            SetEnemyAIMoveEnabled(true);

        landCo = null;
    }

    bool IsTouchingLandingGround()
    {
        if (!myCol) return false;

        Bounds b = myCol.bounds;
        Vector2 foot = new Vector2(b.center.x, b.min.y - 0.02f);

        return Physics2D.OverlapCircle(foot, groundCheckRadius, landingLayers) != null;
    }
}