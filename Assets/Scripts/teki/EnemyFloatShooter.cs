using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyFloatShooter : MonoBehaviour
{
    [Header("Render")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] flySprites;
    public float animFps = 6f;

    [Header("Move")]
    public float moveSpeed = 1.5f;
    public bool moveRight = true;
    public float floatAmplitude = 0.25f;
    public float floatFrequency = 1.2f;

    [Header("Shoot")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float shootInterval = 1.2f;
    public float bulletSpeed = 6f;
    public float bulletLife = 3f;

    [Header("Wall Flip (Non-Trigger walls too)")]
    [Tooltip("回転部屋など“Triggerじゃない壁”も前方Castで検知して反転")]
    public bool flipWhenBlocked = true;

    [Tooltip("前方にこれだけCastして壁を検知")]
    public float wallCheckDistance = 0.12f;

    [Tooltip("反転直後に少しだけ押し戻す（めり込みで停止するの防止）")]
    public float unstuckDistance = 0.03f;

    [Tooltip("連続反転（ガタガタ）防止クールダウン")]
    public float flipCooldown = 0.10f;

    Rigidbody2D rb;
    Collider2D col;
    Enemy enemy;

    float baseY;
    float animTimer;
    int animIndex;

    float shootTimer;

    float flipCooldownTimer;

    // Cast用
    readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    ContactFilter2D castFilter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        enemy = GetComponentInParent<Enemy>();

        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        baseY = rb.position.y;

        // Triggerは無視して“固い壁”だけ見る
        castFilter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = false // ★レイヤーに依存しない（RoomModuleがDefaultでも検知）
        };
    }

    void FixedUpdate()
    {
        if (flipCooldownTimer > 0f) flipCooldownTimer -= Time.fixedDeltaTime;

        // ★投げられ中 / 掴まれ中は Enemy.cs の物理に任せる（AI移動で上書きしない）
        if (enemy != null && (enemy.IsThrown || enemy.IsGrabbed)) return;

        // 進行方向
        float dirX = moveRight ? 1f : -1f;

        // ★前方が塞がってたら反転（回転部屋はTriggerじゃないのでここが効く）
        if (flipWhenBlocked && flipCooldownTimer <= 0f)
        {
            if (IsBlocked(dirX))
            {
                moveRight = !moveRight;
                flipCooldownTimer = flipCooldown;

                // 少し押し戻してめり込み解除
                rb.position += new Vector2(-dirX * unstuckDistance, 0f);

                // 反転後のdirを更新
                dirX = moveRight ? 1f : -1f;
            }
        }

        // 移動 + 浮遊
        float y = baseY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        Vector2 pos = rb.position;
        pos.x += dirX * moveSpeed * Time.fixedDeltaTime;
        pos.y = y;

        rb.MovePosition(pos);

        // 見た目の向き
        if (spriteRenderer) spriteRenderer.flipX = moveRight;

        // 弾
        shootTimer += Time.fixedDeltaTime;
        if (shootTimer >= shootInterval)
        {
            shootTimer = 0f;
            TryShoot(dirX);
        }
    }

    bool IsBlocked(float dirX)
    {
        // 近すぎると誤判定するので、ほんの少し前からCast
        Vector2 dir = new Vector2(dirX, 0f);

        int hitCount = col.Cast(dir, castFilter, castHits, wallCheckDistance);
        if (hitCount <= 0) return false;

        for (int i = 0; i < hitCount; i++)
        {
            var h = castHits[i];
            if (h.collider == null) continue;

            // 自分の子/親は無視（念のため）
            if (h.collider.transform.IsChildOf(transform)) continue;

            // プレイヤー系は無視（ぶつかって反転したくない）
            if (h.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (h.collider.GetComponentInParent<MawaruController>() != null) continue;

            // “敵同士”はレイヤー設定で基本当たらない想定だが、当たってたら無視
            if (h.collider.GetComponentInParent<Enemy>() != null) continue;

            // ここまで来たら壁扱い（RoomModule/回転部屋もここに入る）
            return true;
        }

        return false;
    }

    void Update()
    {
        // アニメ（見た目だけ）
        if (flySprites != null && flySprites.Length > 0 && spriteRenderer != null)
        {
            animTimer += Time.deltaTime;
            float frameTime = (animFps <= 0f) ? 0.2f : (1f / animFps);

            if (animTimer >= frameTime)
            {
                animTimer -= frameTime;
                animIndex = (animIndex + 1) % flySprites.Length;
                spriteRenderer.sprite = flySprites[animIndex];
            }
        }
    }

    void TryShoot(float dirX)
    {
        if (!bulletPrefab || !firePoint) return;

        var go = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        var rb2 = go.GetComponent<Rigidbody2D>();
        if (rb2 != null)
        {
            rb2.velocity = new Vector2(dirX * bulletSpeed, 0f);
        }

        Destroy(go, bulletLife);
    }
}