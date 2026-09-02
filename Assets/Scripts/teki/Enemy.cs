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

    public bool IsThrown => isFlying;

    [Tooltip("HPバー（使っているなら入れる）")]
    public EnemyHpBarController hpBar;

    // ===== Damage / Death Sprite =====
    [Header("Damage / Death Sprite")]
    [Tooltip("ダメージを受けた時に表示する画像。IMG_0138 (1).PNG を入れる")]
    public Sprite damageSprite;

    [Tooltip("ダメージ画像を表示する秒数")]
    public float damageSpriteSeconds = 0.35f;

    [Tooltip("HPが0になった時に表示する画像。IMG_0145 (1).PNG を入れる")]
    public Sprite deadSprite;

    [Tooltip("死亡画像を表示してから消えるまでの秒数。0にするとすぐ消える")]
    public float deathSpriteSecondsBeforeDestroy = 0.8f;

    [Tooltip("画像切り替え中だけAnimatorを止める。Animatorが画像を上書きする場合はON")]
    public bool disableAnimatorWhileSpecialSprite = true;

    // ===== Damage Knockback =====
    [Header("Damage Knockback")]
    [Tooltip("ダメージ時に少し横へずらす")]
    public bool enableDamageKnockback = true;

    [Tooltip("ノックバックで横にずれる距離。0.05〜0.2くらいがおすすめ")]
    public float damageKnockbackX = 0.12f;

    [Tooltip("ノックバックで上にずれる距離。基本は0でOK")]
    public float damageKnockbackY = 0f;

    [Tooltip("ノックバック移動にかける時間。短いほどピクッとする")]
    public float damageKnockbackMoveSeconds = 0.08f;

    [Tooltip("ノックバック後に横速度を必ず止める。基本ON")]
    public bool stopVelocityAfterDamageKnockback = true;

    [Tooltip("ノックバック方向が逆に感じる場合だけON")]
    public bool invertDamageKnockbackDirection = false;

    [Tooltip("互換用。今回の距離式ノックバックでは基本使いません")]
    public bool forceDamageKnockbackY = false;

    // ===== Death =====
    [Header("Death")]
    public GameObject deathEffectPrefab;
    public float deathEffectLife = 1.2f;

    // ===== Item Drop =====
    [Header("Item Drop")]
    [Tooltip("倒したときにアイテムを落とす。EnemyDeathRandomDropを使う場合はOFFのままでOK")]
    public bool enableItemDrop = false;

    [Tooltip("落とすPrefab")]
    public GameObject[] dropPrefabs;

    [Range(0f, 1f)]
    public float dropChance = 1f;

    [Min(0)]
    public int dropMinCount = 1;

    [Min(0)]
    public int dropMaxCount = 1;

    [Tooltip("落ちる位置のばらけ")]
    public float dropScatterRadius = 0.25f;

    [Tooltip("落とした瞬間にちょい飛ばす")]
    public Vector2 dropImpulse = new Vector2(1.5f, 2.0f);

    [Tooltip("敵の速度を少し引き継ぐ")]
    public bool dropInheritVelocity = true;

    // ===== Thrown Collision =====
    [Header("Thrown Collision")]
    public LayerMask thrownHitLayers;
    public LayerMask landingLayers;

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

    [Header("Thrown Damage (keep existing behavior)")]
    public int thrownDamage = 1;
    public int collisionDamageToOther = 1;

    // ===== runtime =====
    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator animator;

    bool isDead = false;
    bool shellStunned = false;
    bool damageStunned = false;

    float shellStunTimer = 0f;
    float lastBounceTime = -999f;

    Coroutine damageReactionRoutine;
    Coroutine deathRoutine;

    public bool IsGrabbed => isGrabbed;
    public bool IsFlying => isFlying;
    public bool IsShellStunned => shellStunned;
    public bool IsDamageStunned => damageStunned;
    public bool IsDead => isDead;
    public Rigidbody2D Rb => rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>(true);

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (maxHp < 1) maxHp = 1;
        hp = Mathf.Clamp(hp, 0, maxHp);

        TryUpdateHpBar();
    }

    void Update()
    {
        if (isDead) return;

        if (shellStunned)
        {
            shellStunTimer -= Time.deltaTime;

            if (shellStunTimer <= 0f)
            {
                shellStunned = false;
                shellStunTimer = 0f;

                if (!isGrabbed && !isFlying && !damageStunned)
                    SetEnemyAIMoveEnabled(true);
            }
        }
    }

    // ===== Damage =====
    public void TakeDamage(int damage)
    {
        TakeDamageInternal(damage, false, Vector2.zero, "Unknown");
    }

    public void TakeDamage(int damage, string source)
    {
        TakeDamageInternal(damage, false, Vector2.zero, source);
    }

    public void TakeDamage(int damage, Vector2 attackerWorldPosition)
    {
        TakeDamageInternal(damage, true, attackerWorldPosition, "AttackerPosition");
    }

    public void TakeDamage(int damage, Transform attacker)
    {
        if (attacker != null)
            TakeDamageInternal(damage, true, attacker.position, "AttackerTransform");
        else
            TakeDamageInternal(damage, false, Vector2.zero, "Unknown");
    }

    void TakeDamageInternal(int damage, bool hasAttackerPosition, Vector2 attackerWorldPosition, string source)
    {
        if (isDead) return;
        if (damage <= 0) return;

        hp -= damage;
        if (hp < 0) hp = 0;

        TryUpdateHpBar();

        if (hp <= 0)
        {
            Die();
            return;
        }

        StartDamageReaction(hasAttackerPosition, attackerWorldPosition);
    }

    void StartDamageReaction(bool hasAttackerPosition, Vector2 attackerWorldPosition)
    {
        if (isDead) return;

        if (damageReactionRoutine != null)
            StopCoroutine(damageReactionRoutine);

        damageReactionRoutine = StartCoroutine(DamageReactionCoroutine(hasAttackerPosition, attackerWorldPosition));
    }

    IEnumerator DamageReactionCoroutine(bool hasAttackerPosition, Vector2 attackerWorldPosition)
    {
        damageStunned = true;

        SetEnemyAIMoveEnabled(false);

        if (rb != null)
        {
            rb.angularVelocity = 0f;
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        if (disableAnimatorWhileSpecialSprite && animator != null)
            animator.enabled = false;

        if (sr != null && damageSprite != null)
            sr.sprite = damageSprite;

        float totalWait = Mathf.Max(0f, damageSpriteSeconds);
        float elapsed = 0f;

        if (enableDamageKnockback && rb != null)
        {
            float dir = GetDamageKnockbackDirection(hasAttackerPosition, attackerWorldPosition);

            float moveSeconds = Mathf.Clamp(damageKnockbackMoveSeconds, 0.01f, totalWait > 0f ? totalWait : 0.01f);
            Vector2 startPos = rb.position;
            Vector2 targetPos = startPos + new Vector2(dir * Mathf.Abs(damageKnockbackX), damageKnockbackY);

            float moveTimer = 0f;

            while (moveTimer < moveSeconds)
            {
                if (isDead) yield break;

                if (sr != null && damageSprite != null)
                    sr.sprite = damageSprite;

                moveTimer += Time.fixedDeltaTime;
                elapsed += Time.fixedDeltaTime;

                float t = Mathf.Clamp01(moveTimer / moveSeconds);
                float smoothT = t * t * (3f - 2f * t);

                rb.MovePosition(Vector2.Lerp(startPos, targetPos, smoothT));

                yield return new WaitForFixedUpdate();
            }

            if (stopVelocityAfterDamageKnockback)
                rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        while (elapsed < totalWait)
        {
            if (isDead) yield break;

            if (sr != null && damageSprite != null)
                sr.sprite = damageSprite;

            if (stopVelocityAfterDamageKnockback && rb != null)
                rb.velocity = new Vector2(0f, rb.velocity.y);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (stopVelocityAfterDamageKnockback && rb != null)
            rb.velocity = new Vector2(0f, rb.velocity.y);

        damageStunned = false;

        if (!isDead)
        {
            if (disableAnimatorWhileSpecialSprite && animator != null)
                animator.enabled = true;

            if (!isGrabbed && !isFlying && !shellStunned)
                SetEnemyAIMoveEnabled(true);
        }

        damageReactionRoutine = null;
    }

    float GetDamageKnockbackDirection(bool hasAttackerPosition, Vector2 attackerWorldPosition)
    {
        float dir = 0f;

        if (hasAttackerPosition)
            dir = Mathf.Sign(transform.position.x - attackerWorldPosition.x);

        if (Mathf.Abs(dir) < 0.01f && rb != null && Mathf.Abs(rb.velocity.x) > 0.01f)
            dir = -Mathf.Sign(rb.velocity.x);

        if (Mathf.Abs(dir) < 0.01f)
        {
            EnemyStraightMouth straight = GetComponent<EnemyStraightMouth>();
            if (straight != null)
                dir = straight.moveRight ? -1f : 1f;
        }

        if (Mathf.Abs(dir) < 0.01f)
        {
            if (sr != null)
                dir = sr.flipX ? -1f : 1f;
            else
                dir = -1f;
        }

        if (invertDamageKnockbackDirection)
            dir *= -1f;

        return dir;
    }

    void TryUpdateHpBar()
    {
        if (hpBar == null) return;
        hpBar.SetHp(hp, maxHp);
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        if (damageReactionRoutine != null)
        {
            StopCoroutine(damageReactionRoutine);
            damageReactionRoutine = null;
        }

        if (deathRoutine != null)
            StopCoroutine(deathRoutine);

        deathRoutine = StartCoroutine(DeathCoroutine());
    }

    IEnumerator DeathCoroutine()
    {
        damageStunned = false;
        shellStunned = false;
        shellStunTimer = 0f;

        SetEnemyAIMoveEnabled(false);

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }

        if (disableAnimatorWhileSpecialSprite && animator != null)
            animator.enabled = false;

        if (sr != null && deadSprite != null)
            sr.sprite = deadSprite;

        SpawnDrops();

        if (deathEffectPrefab != null)
        {
            GameObject fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            if (deathEffectLife > 0f)
                Destroy(fx, deathEffectLife);
        }

        float wait = Mathf.Max(0f, deathSpriteSecondsBeforeDestroy);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

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
        Vector2 inheritVel = Vector2.zero;

        if (dropInheritVelocity && rb != null)
            inheritVel = rb.velocity;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = dropPrefabs[Random.Range(0, dropPrefabs.Length)];
            if (prefab == null) continue;

            Vector2 offset = Random.insideUnitCircle * dropScatterRadius;
            GameObject go = Instantiate(prefab, basePos + offset, Quaternion.identity);

            Rigidbody2D dropRb = go.GetComponent<Rigidbody2D>();
            if (dropRb != null)
            {
                float dir = 1f;

                if (sr != null && sr.flipX)
                    dir = -1f;

                Vector2 impulse = new Vector2(dropImpulse.x * dir, dropImpulse.y);

                dropRb.velocity = inheritVel;
                dropRb.AddForce(impulse, ForceMode2D.Impulse);
            }
        }
    }

    // ===== Shell Stun =====
    public void ApplyShellStun(float seconds)
    {
        if (isDead) return;

        if (seconds <= 0f)
            seconds = shellStunDefaultSeconds;

        shellStunned = true;
        shellStunTimer = seconds;

        SetEnemyAIMoveEnabled(false);
    }

    public void ClearShellStun()
    {
        if (isDead) return;

        shellStunned = false;
        shellStunTimer = 0f;

        if (!isGrabbed && !isFlying && !damageStunned)
            SetEnemyAIMoveEnabled(true);
    }

    public void BeginThrow()
    {
        if (isDead) return;

        isGrabbed = false;
        isFlying = true;

        damageStunned = false;

        if (damageReactionRoutine != null)
        {
            StopCoroutine(damageReactionRoutine);
            damageReactionRoutine = null;
        }

        if (disableAnimatorWhileSpecialSprite && animator != null)
            animator.enabled = true;

        lastBounceTime = -999f;

        SetEnemyAIMoveEnabled(false);
    }

    void SetEnemyAIMoveEnabled(bool enabled)
    {
        EnemyStraightMouth a = GetComponent<EnemyStraightMouth>();
        if (a != null) a.enabled = enabled;

        EnemyFloatShooter b = GetComponent<EnemyFloatShooter>();
        if (b != null) b.enabled = enabled;

        EnemyBoomerangThrower c = GetComponent<EnemyBoomerangThrower>();
        if (c != null) c.enabled = enabled;

        EnemyController d = GetComponent<EnemyController>();
        if (d != null) d.enabled = enabled;
    }

    // ===== Thrown collision =====
    void OnCollisionEnter2D(Collision2D col)
    {
        if (isDead) return;
        if (!isFlying) return;
        if (col == null || col.collider == null) return;

        int layer = col.collider.gameObject.layer;
        if ((thrownHitLayers.value & (1 << layer)) == 0) return;

        HandleThrownCollision(col.collider, col);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (!isFlying) return;
        if (other == null) return;

        int layer = other.gameObject.layer;
        if ((thrownHitLayers.value & (1 << layer)) == 0) return;

        HandleThrownCollision(other, null);
    }

    void HandleThrownCollision(Collider2D other, Collision2D col)
    {
        if (isDead) return;
        if (!isFlying) return;
        if (other == null) return;

        if (Time.time - lastBounceTime < minAirTimeAfterBounce) return;

        Enemy otherEnemy = other.GetComponentInParent<Enemy>();
        if (otherEnemy != null && otherEnemy != this)
        {
            if (collisionDamageToOther > 0)
                otherEnemy.TakeDamage(collisionDamageToOther, transform.position);

            if (thrownDamage > 0)
            {
                TakeDamage(thrownDamage, "ThrownSelf");
                if (isDead) return;
            }

            Vector2 normal = Vector2.right;

            if (col != null && col.contactCount > 0)
                normal = col.GetContact(0).normal;

            BounceByNormal(normal, false);
            return;
        }

        int layer = other.gameObject.layer;
        bool isLandingLayer = (landingLayers.value & (1 << layer)) != 0;

        if (isLandingLayer && IsGrounded())
        {
            isFlying = false;

            if (!isGrabbed && !shellStunned && !damageStunned)
                SetEnemyAIMoveEnabled(true);

            return;
        }

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
            ground = IsGrounded();

            if (rb != null)
                hitNormal = ground ? Vector2.up : (rb.velocity.x >= 0f ? Vector2.left : Vector2.right);
        }

        BounceByNormal(hitNormal, ground);
    }

    bool IsGrounded()
    {
        if (rb == null) return false;

        Vector2 origin = rb.position;
        origin.y -= 0.2f;

        LayerMask mask = landingLayers.value != 0 ? landingLayers : thrownHitLayers;
        return Physics2D.OverlapCircle(origin, groundCheckRadius, mask);
    }

    void BounceByNormal(Vector2 normal, bool isGround)
    {
        if (isDead) return;
        if (rb == null) return;

        Vector2 v = rb.velocity;
        Vector2 reflected = Vector2.Reflect(v, normal) * reboundMul;

        if (reflected.sqrMagnitude < 0.0001f)
            reflected = normal * minReboundSpeed;

        if (reflected.magnitude < minReboundSpeed)
            reflected = reflected.normalized * minReboundSpeed;

        if (isGround && reflected.y < minUpSpeedOnGround)
            reflected.y = minUpSpeedOnGround;

        rb.velocity = reflected;
        lastBounceTime = Time.time;

        if (rb.velocity.magnitude < stuckSpeedThreshold)
            rb.position += normal * depenetrationSkin;
    }
}