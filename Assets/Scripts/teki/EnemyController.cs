using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2f;
    public bool moveRight = true;

    [Header("Wall Check (Trigger)")]
    [Tooltip("壁検知用Triggerに当たったら反転する")]
    public bool flipOnTrigger = true;

    [Header("Wall Check (Collision)")]
    [Tooltip("壁など、Triggerではない横衝突でも反転する")]
    public bool flipOnCollision = true;

    [Tooltip("連続反転を防ぐクールダウン")]
    public float flipCooldown = 0.08f;

    [Tooltip("横方向の接触を壁とみなすしきい値")]
    [Range(0.0f, 1.0f)]
    public float sideNormalThreshold = 0.55f;

    [Header("Ledge / Edge Turn")]
    [Tooltip("ONにすると、地面の端で落ちる前に反転する")]
    public bool flipAtLedge = true;

    [Tooltip("足元より少し前を見る距離。落ちるなら大きくする")]
    public float edgeCheckForwardOffset = 0.12f;

    [Tooltip("足元から下に地面を探す距離。地面を検出しないなら大きくする")]
    public float edgeCheckDownDistance = 0.55f;

    [Tooltip("足元から少し上の位置からRayを出す")]
    public float edgeRayStartYOffset = 0.12f;

    [Tooltip("地面判定に使うLayer。基本は Ground を入れる")]
    public LayerMask groundLayer;

    [Tooltip("Ground Layerが未設定なら、自動で Ground レイヤーを探す")]
    public bool autoUseGroundLayerIfEmpty = true;

    [Tooltip("端で反転した時に少し内側へ戻す距離")]
    public float ledgePushBack = 0.03f;

    Rigidbody2D rb;
    Collider2D bodyCollider;
    SpriteRenderer spriteRenderer;
    Enemy enemy;

    float flipCooldownTimer;
    bool warnedGroundLayerEmpty = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        enemy = GetComponentInParent<Enemy>();

        AutoSetGroundLayerIfNeeded();
        UpdateVisualDirection();
    }

    void OnValidate()
    {
        if (speed < 0f) speed = 0f;
        if (flipCooldown < 0f) flipCooldown = 0f;
        if (edgeCheckForwardOffset < 0f) edgeCheckForwardOffset = 0f;
        if (edgeCheckDownDistance < 0.01f) edgeCheckDownDistance = 0.01f;
        if (edgeRayStartYOffset < 0f) edgeRayStartYOffset = 0f;
        if (ledgePushBack < 0f) ledgePushBack = 0f;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (flipCooldownTimer > 0f)
            flipCooldownTimer -= Time.fixedDeltaTime;

        if (enemy != null)
        {
            if (enemy.IsDead)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            if (enemy.IsThrown || enemy.IsFlying || enemy.IsGrabbed || enemy.IsShellStunned || enemy.IsDamageStunned)
                return;
        }

        AutoSetGroundLayerIfNeeded();

        if (flipAtLedge && IsAtLedge() && flipCooldownTimer <= 0f)
        {
            Flip();
            flipCooldownTimer = flipCooldown;

            float pushDir = moveRight ? 1f : -1f;
            rb.position += new Vector2(pushDir * ledgePushBack, 0f);
        }

        float dir = moveRight ? 1f : -1f;
        rb.velocity = new Vector2(dir * speed, rb.velocity.y);

        UpdateVisualDirection();
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

    bool IsAtLedge()
    {
        if (bodyCollider == null) return false;

        if (groundLayer.value == 0)
        {
            if (!warnedGroundLayerEmpty)
            {
                warnedGroundLayerEmpty = true;
                Debug.LogWarning($"[{nameof(EnemyController)}] Ground Layer が未設定です。{name} の Ground Layer に Ground を設定してください。", this);
            }

            return false;
        }

        Bounds b = bodyCollider.bounds;
        float dir = moveRight ? 1f : -1f;

        Vector2 rayOrigin = new Vector2(
            b.center.x + dir * (b.extents.x + edgeCheckForwardOffset),
            b.min.y + edgeRayStartYOffset
        );

        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            edgeCheckDownDistance,
            groundLayer
        );

        return hit.collider == null;
    }

    public void Flip()
    {
        moveRight = !moveRight;
        UpdateVisualDirection();
    }

    void UpdateVisualDirection()
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = moveRight;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!flipOnTrigger) return;
        if (other == null) return;
        if (!other.isTrigger) return;
        if (flipCooldownTimer > 0f) return;

        if (enemy != null)
        {
            if (enemy.IsDead) return;
            if (enemy.IsThrown || enemy.IsFlying || enemy.IsGrabbed || enemy.IsShellStunned || enemy.IsDamageStunned) return;
        }

        Flip();
        flipCooldownTimer = flipCooldown;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        TryFlipByCollision(col);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        TryFlipByCollision(col);
    }

    void TryFlipByCollision(Collision2D col)
    {
        if (!flipOnCollision) return;
        if (flipCooldownTimer > 0f) return;
        if (col == null || col.collider == null) return;
        if (col.collider.isTrigger) return;

        if (enemy != null)
        {
            if (enemy.IsDead) return;
            if (enemy.IsThrown || enemy.IsFlying || enemy.IsGrabbed || enemy.IsShellStunned || enemy.IsDamageStunned) return;
        }

        if (col.collider.GetComponentInParent<PlayerController>() != null) return;
        if (col.collider.GetComponentInParent<MawaruController>() != null) return;

        for (int i = 0; i < col.contactCount; i++)
        {
            ContactPoint2D c = col.GetContact(i);

            if (Mathf.Abs(c.normal.x) >= sideNormalThreshold)
            {
                Flip();
                flipCooldownTimer = flipCooldown;
                return;
            }
        }
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
    }
}