using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyStraightMouth : MonoBehaviour
{
    [Header("Refs")]
    public SpriteRenderer spriteRenderer;
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Move")]
    public float moveSpeed = 1.5f;
    public bool moveRight = true;

    [Header("Wall check")]
    public Transform wallCheck;
    public float wallCheckRadius = 0.08f;
    public LayerMask wallLayer;

    [Header("Ledge / Edge Turn")]
    [Tooltip("ONにすると、地面の端で落ちる前に反転する")]
    public bool turnAtLedge = true;

    [Tooltip("足元より少し前を見る距離。落ちるのが遅いなら大きくする")]
    public float edgeCheckForwardOffset = 0.08f;

    [Tooltip("足元から下に地面を探す距離。地面を検出しないなら大きくする")]
    public float edgeCheckDownDistance = 0.45f;

    [Tooltip("足元から少し上の位置からRayを出す")]
    public float edgeRayStartYOffset = 0.12f;

    [Tooltip("Ground Layerが未設定なら、自動で Ground レイヤーを探す")]
    public bool autoUseGroundLayerIfEmpty = true;

    [Header("Animation")]
    public Sprite[] walkSprites;
    public float animFps = 6f;

    [Header("Fix: rotate-room wall / solid side collision turns")]
    [Tooltip("回転部屋や通常壁など、横から固いものに当たったら反転する")]
    public bool turnOnAnySolidSideCollision = true;

    [Tooltip("壁・端で反転した直後の連続反転を防ぐクールダウン")]
    public float wallTurnCooldown = 0.08f;

    [Tooltip("壁から少し押し戻す量")]
    public float depenetrationPush = 0.03f;

    Rigidbody2D rb;
    Collider2D bodyCollider;
    Enemy enemy;

    int animIndex;
    float animTimer;
    float lastTurnTime = -999f;

    bool warnedGroundLayerEmpty = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        enemy = GetComponent<Enemy>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        AutoSetGroundLayerIfNeeded();
    }

    void OnValidate()
    {
        if (moveSpeed < 0f) moveSpeed = 0f;
        if (wallCheckRadius < 0.001f) wallCheckRadius = 0.001f;
        if (edgeCheckForwardOffset < 0f) edgeCheckForwardOffset = 0f;
        if (edgeCheckDownDistance < 0.001f) edgeCheckDownDistance = 0.001f;
        if (edgeRayStartYOffset < 0f) edgeRayStartYOffset = 0f;
        if (wallTurnCooldown < 0f) wallTurnCooldown = 0f;
        if (animFps < 0.01f) animFps = 0.01f;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (enemy != null)
        {
            if (enemy.IsDead)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            if (enemy.IsGrabbed)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            if (enemy.IsFlying)
                return;

            if (enemy.IsShellStunned)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            if (enemy.IsDamageStunned)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
                return;
            }
        }

        AutoSetGroundLayerIfNeeded();

        bool shouldTurn = false;

        if (IsTouchingWallCheck())
            shouldTurn = true;

        if (!shouldTurn && IsAtLedge())
            shouldTurn = true;

        if (shouldTurn && Time.time - lastTurnTime >= wallTurnCooldown)
        {
            TurnAround();
        }

        float vx = (moveRight ? 1f : -1f) * moveSpeed;
        rb.velocity = new Vector2(vx, rb.velocity.y);

        UpdateVisualDirection();
        UpdateWalkAnimation();
    }

    void AutoSetGroundLayerIfNeeded()
    {
        if (!autoUseGroundLayerIfEmpty) return;
        if (groundLayer.value != 0) return;

        int groundLayerIndex = LayerMask.NameToLayer("Ground");
        if (groundLayerIndex >= 0)
        {
            groundLayer = 1 << groundLayerIndex;
        }
    }

    bool IsTouchingWallCheck()
    {
        if (wallCheck == null) return false;
        if (wallLayer.value == 0) return false;

        return Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);
    }

    bool IsAtLedge()
    {
        if (!turnAtLedge) return false;
        if (bodyCollider == null) return false;

        if (groundLayer.value == 0)
        {
            if (!warnedGroundLayerEmpty)
            {
                warnedGroundLayerEmpty = true;
                Debug.LogWarning($"[{nameof(EnemyStraightMouth)}] Ground Layer が未設定です。{name} の Ground Layer に Ground を設定してください。", this);
            }

            return false;
        }

        Bounds b = bodyCollider.bounds;
        float dir = moveRight ? 1f : -1f;

        Vector2 rayOrigin = new Vector2(
            b.center.x + dir * (b.extents.x + edgeCheckForwardOffset),
            b.min.y + edgeRayStartYOffset
        );

        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, edgeCheckDownDistance, groundLayer);

        // 前方足元に地面がない = 落ちる手前なので反転
        return hit.collider == null;
    }

    void TurnAround()
    {
        lastTurnTime = Time.time;
        moveRight = !moveRight;

        if (rb != null)
        {
            float push = Mathf.Max(0f, depenetrationPush);
            rb.position += new Vector2(moveRight ? push : -push, 0f);
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    void UpdateVisualDirection()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.flipX = moveRight;
    }

    void UpdateWalkAnimation()
    {
        if (spriteRenderer == null) return;
        if (walkSprites == null || walkSprites.Length == 0) return;

        animTimer += Time.fixedDeltaTime;

        float spf = 1f / Mathf.Max(0.01f, animFps);

        if (animTimer >= spf)
        {
            animTimer -= spf;
            animIndex = (animIndex + 1) % walkSprites.Length;
            spriteRenderer.sprite = walkSprites[animIndex];
        }
    }

    bool TryGetSideNormal(Collision2D c, out float nx)
    {
        nx = 0f;

        if (c == null || c.contactCount == 0)
            return false;

        for (int i = 0; i < c.contactCount; i++)
        {
            Vector2 n = c.contacts[i].normal;

            if (Mathf.Abs(n.x) > 0.5f && Mathf.Abs(n.x) > Mathf.Abs(n.y))
            {
                nx = n.x;
                return true;
            }
        }

        return false;
    }

    bool ShouldIgnoreCollider(Collider2D other)
    {
        if (other == null) return true;
        if (other.isTrigger) return true;
        if (other.transform.IsChildOf(transform)) return true;

        if (other.GetComponentInParent<PlayerController>() != null) return true;
        if (other.GetComponentInParent<MawaruController>() != null) return true;

        return false;
    }

    void TurnFromSideCollision(float nx)
    {
        if (Time.time - lastTurnTime < wallTurnCooldown)
            return;

        lastTurnTime = Time.time;

        // normal.x > 0 → 左壁に当たった → 右へ
        // normal.x < 0 → 右壁に当たった → 左へ
        moveRight = nx > 0f;

        if (rb != null)
        {
            float push = Mathf.Max(0f, depenetrationPush);
            rb.position += new Vector2(moveRight ? push : -push, 0f);
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (!turnOnAnySolidSideCollision) return;

        if (enemy != null)
        {
            if (enemy.IsDead) return;
            if (enemy.IsFlying) return;
            if (enemy.IsGrabbed) return;
            if (enemy.IsDamageStunned) return;
        }

        if (c == null || ShouldIgnoreCollider(c.collider)) return;

        if (TryGetSideNormal(c, out float nx))
            TurnFromSideCollision(nx);
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (!turnOnAnySolidSideCollision) return;

        if (enemy != null)
        {
            if (enemy.IsDead) return;
            if (enemy.IsFlying) return;
            if (enemy.IsGrabbed) return;
            if (enemy.IsDamageStunned) return;
        }

        if (c == null || ShouldIgnoreCollider(c.collider)) return;

        if (TryGetSideNormal(c, out float nx))
            TurnFromSideCollision(nx);
    }

    void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Bounds b = col.bounds;
        float dir = moveRight ? 1f : -1f;

        Vector2 rayOrigin = new Vector2(
            b.center.x + dir * (b.extents.x + edgeCheckForwardOffset),
            b.min.y + edgeRayStartYOffset
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector2.down * edgeCheckDownDistance);
        Gizmos.DrawSphere(rayOrigin, 0.035f);

        if (wallCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(wallCheck.position, wallCheckRadius);
        }
    }
}