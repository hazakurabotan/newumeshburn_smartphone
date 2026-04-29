using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    // ===== HP =====
    [Header("HP")]
    public int hp = 3;
    public int maxHp = 3;
    public bool isGrabbed = false;
    public bool isFlying = false;

    // Enemy.cs の class Enemy 内に追加（場所はどこでもOK）
    //
    // 例：public bool isFlying = false; のすぐ下あたりが分かりやすい
    public bool IsThrown => isFlying;

    [Tooltip("HPバー（使っているなら入れる）")]
    public EnemyHpBarController hpBar;

    // ===== Death =====
    [Header("Death")]
    public GameObject deathEffectPrefab;
    public float deathEffectLife = 1.2f;

    // ===== Item Drop =====
    [Header("Item Drop")]
    [Tooltip("倒したときにアイテムを落とす")]
    public bool enableItemDrop = false;

    [Tooltip("落とすPrefab（item_2 など）")]
    public GameObject[] dropPrefabs;

    [Range(0f, 1f)]
    public float dropChance = 1f;

    [Min(0)]
    public int dropMinCount = 1;

    [Min(0)]
    public int dropMaxCount = 1;

    [Tooltip("落ちる位置のばらけ（半径）")]
    public float dropScatterRadius = 0.25f;

    [Tooltip("落とした瞬間にちょい飛ばす（X=横/Y=上）")]
    public Vector2 dropImpulse = new Vector2(1.5f, 2.0f);

    [Tooltip("敵の速度を少し引き継ぐ")]
    public bool dropInheritVelocity = true;

    // ===== Thrown Collision =====
    [Header("Thrown Collision")]
    public LayerMask thrownHitLayers;   // Ground, Wall
    public LayerMask landingLayers;     // Ground

    [Header("Bounce Tuning")]
    public float reboundMul = 1.3f;
    public float minReboundSpeed = 6f;
    public float minUpSpeedOnGround = 4f;
    public float minAirTimeAfterBounce = 0.06f;
    public float groundCheckRadius = 0.08f;

    [Header("Anti-Stick")]
    public float depenetrationSkin = 0.03f;
    public float stuckSpeedThreshold = 0.5f;

    [Header("Shell Stun")]
    public float shellStunDefaultSeconds = 1f;

    // ===== 既存挙動（投げで当たった時のダメージ系）=====
    // ※これを消すと「今まで通り」の投げ挙動が変わるので維持
    [Header("Thrown Damage (keep existing behavior)")]
    public int thrownDamage = 1;            // 壁/地面に当たった時に自分が受けるダメージ
    public int collisionDamageToOther = 1;  // 投げ中に別の敵に当たった時、相手に入れるダメージ

    // ===== runtime =====
    Rigidbody2D rb;
    SpriteRenderer sr;

    bool isDead = false;
    bool shellStunned = false;
    float shellStunTimer = 0f;

    float lastBounceTime = -999f;

    public bool IsGrabbed => isGrabbed;
    public bool IsFlying => isFlying;
    public bool IsShellStunned => shellStunned;
    public Rigidbody2D Rb => rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        if (maxHp < 1) maxHp = 1;
        hp = Mathf.Clamp(hp, 0, maxHp);

        // 初期HPバー更新（存在する時だけ）
        TryUpdateHpBar();
    }

    void Update()
    {
        // シェルスタンの解除
        if (shellStunned)
        {
            shellStunTimer -= Time.deltaTime;
            if (shellStunTimer <= 0f)
            {
                shellStunned = false;
                shellStunTimer = 0f;

                // 掴まれ中/投げ中はAI止めたまま
                if (!isGrabbed && !isFlying)
                    SetEnemyAIMoveEnabled(true);
            }
        }
    }

    // ===== ダメージ =====
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, "Unknown");
    }

    public void TakeDamage(int damage, string source)
    {
        if (isDead) return;
        if (damage <= 0) return;

        hp -= damage;
        if (hp < 0) hp = 0;

        TryUpdateHpBar();

        if (hp <= 0)
        {
            Die();
        }
    }

    void TryUpdateHpBar()
    {
        if (hpBar == null) return;

        // EnemyHpBarController 側に SetHp(int hp, int maxHp) がある想定
        // （もし名前が違う場合は、そっちに合わせる）
        hpBar.SetHp(hp, maxHp);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        // アイテムドロップ
        SpawnDrops();

        // 死亡エフェクト
        if (deathEffectPrefab)
        {
            var fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            if (deathEffectLife > 0f) Destroy(fx, deathEffectLife);
        }

        Destroy(gameObject);
    }

    void SpawnDrops()
    {
        if (!enableItemDrop) return;
        if (dropPrefabs == null || dropPrefabs.Length == 0) return;
        if (Random.value > dropChance) return;

        int min = Mathf.Max(0, dropMinCount);
        int max = Mathf.Max(min, dropMaxCount);
        int count = Random.Range(min, max + 1);
        if (count <= 0) return;

        Vector2 basePos = transform.position;
        Vector2 inheritVel = (dropInheritVelocity && rb != null) ? rb.velocity : Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            var prefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
            if (!prefab) continue;

            Vector2 offset = Random.insideUnitCircle * dropScatterRadius;
            var go = Instantiate(prefab, basePos + offset, Quaternion.identity);

            var dropRb = go.GetComponent<Rigidbody2D>();
            if (dropRb != null)
            {
                float dir = (sr != null && sr.flipX) ? -1f : 1f;
                Vector2 impulse = new Vector2(dropImpulse.x * dir, dropImpulse.y);
                dropRb.velocity = inheritVel;
                dropRb.AddForce(impulse, ForceMode2D.Impulse);
            }
        }
    }

    // ===== シェルスタン =====
    public void ApplyShellStun(float seconds)
    {
        if (seconds <= 0f) seconds = shellStunDefaultSeconds;
        shellStunned = true;
        shellStunTimer = seconds;

        // その場で止める
        SetEnemyAIMoveEnabled(false);
    }

    public void ClearShellStun()
    {
        shellStunned = false;
        shellStunTimer = 0f;

        if (!isGrabbed && !isFlying)
            SetEnemyAIMoveEnabled(true);
    }

    // RopeHead から呼ばれる想定
    public void BeginThrow()
    {
        isGrabbed = false;
        isFlying = true;

        lastBounceTime = -999f;

        // 投げ中はAIに速度を上書きさせない
        SetEnemyAIMoveEnabled(false);
    }

    // ===== AI on/off（既存維持）=====
    void SetEnemyAIMoveEnabled(bool enabled)
    {
        // ここは「あなたの敵AIスクリプト」に合わせて維持
        var a = GetComponent<EnemyStraightMouth>();
        if (a) a.enabled = enabled;

        var b = GetComponent<EnemyFloatShooter>();
        if (b) b.enabled = enabled;

        var c = GetComponent<EnemyBoomerangThrower>();
        if (c) c.enabled = enabled;

        var d = GetComponent<EnemyController>();
        if (d) d.enabled = enabled;
    }

    // ===== 投げ中の当たり処理（既存方針維持）=====
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!isFlying) return;

        int layer = col.collider.gameObject.layer;
        if ((thrownHitLayers.value & (1 << layer)) == 0) return;

        HandleThrownCollision(col.collider, col);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isFlying) return;

        int layer = other.gameObject.layer;
        if ((thrownHitLayers.value & (1 << layer)) == 0) return;

        HandleThrownCollision(other, null);
    }

    void HandleThrownCollision(Collider2D other, Collision2D col)
    {
        if (!isFlying) return;

        // 連続ヒット防止
        if (Time.time - lastBounceTime < minAirTimeAfterBounce) return;

        // 1) 別の敵に当たった（投げの攻撃）
        var otherEnemy = other.GetComponentInParent<Enemy>();
        if (otherEnemy != null && otherEnemy != this)
        {
            if (collisionDamageToOther > 0)
            {
                otherEnemy.TakeDamage(collisionDamageToOther, "ThrownEnemy");
            }

            if (thrownDamage > 0)
            {
                TakeDamage(thrownDamage, "ThrownSelf");
                if (isDead) return;
            }

            // バウンド（法線が取れれば反射、なければX反転）
            Vector2 normal = Vector2.right;
            if (col != null && col.contactCount > 0) normal = col.GetContact(0).normal;
            BounceByNormal(normal, isGround: false);
            return;
        }

        // 2) 着地判定（Groundなど）
        int layer = other.gameObject.layer;
        bool isLandingLayer = (landingLayers.value & (1 << layer)) != 0;
        if (isLandingLayer && IsGrounded())
        {
            isFlying = false;

            // 掴まれ中/スタン中でなければAI再開
            if (!isGrabbed && !shellStunned)
                SetEnemyAIMoveEnabled(true);

            return;
        }

        // 3) 壁/地面に当たった（投げ中は今まで通り「自分がダメージ」+ バウンド）
        if (thrownDamage > 0)
        {
            TakeDamage(thrownDamage, "ThrownWall");
            if (isDead) return;
        }

        Vector2 hitNormal = Vector2.right;
        bool ground = false;

        if (col != null && col.contactCount > 0)
        {
            hitNormal = col.GetContact(0).normal;
            ground = hitNormal.y > 0.5f;
        }
        else
        {
            // Triggerの場合：ざっくり「下にあるなら地面扱い」に寄せる
            ground = IsGrounded();
            hitNormal = ground ? Vector2.up : (rb.velocity.x >= 0 ? Vector2.left : Vector2.right);
        }

        BounceByNormal(hitNormal, ground);
    }

    bool IsGrounded()
    {
        // 自分の足元（Collider中心より少し下）で円チェック
        Vector2 origin = rb.position;
        origin.y -= 0.2f;

        // landingLayers が未設定でも ground 判定が欲しいので thrownHitLayers も混ぜる
        LayerMask mask = landingLayers.value != 0 ? landingLayers : thrownHitLayers;
        return Physics2D.OverlapCircle(origin, groundCheckRadius, mask);
    }

    void BounceByNormal(Vector2 normal, bool isGround)
    {
        if (rb == null) return;

        Vector2 v = rb.velocity;
        Vector2 reflected = Vector2.Reflect(v, normal) * reboundMul;

        if (reflected.magnitude < minReboundSpeed)
            reflected = reflected.normalized * minReboundSpeed;

        if (isGround && reflected.y < minUpSpeedOnGround)
            reflected.y = minUpSpeedOnGround;

        rb.velocity = reflected;
        lastBounceTime = Time.time;

        // はまり防止（押し出し）
        if (rb.velocity.magnitude < stuckSpeedThreshold)
        {
            rb.position += normal * depenetrationSkin;
        }
    }
}