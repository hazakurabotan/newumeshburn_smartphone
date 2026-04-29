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

    [Header("Wall check (従来：WallLayer)")]
    public Transform wallCheck;
    public float wallCheckRadius = 0.08f;
    public LayerMask wallLayer;

    [Header("Animation")]
    public Sprite[] walkSprites;
    public float animFps = 6f;

    [Header("Fix: rotate-room wall (any solid side collision turns)")]
    [Tooltip("回転部屋(=Defaultなど)の壁でも止まらず反転させる。通常のwallLayer設定はそのままでOK")]
    public bool turnOnAnySolidSideCollision = true;

    [Tooltip("壁に当たった直後の連続反転を防ぐクールダウン")]
    public float wallTurnCooldown = 0.08f;

    [Tooltip("壁から少し押し戻す量（めり込みで停止するの対策）")]
    public float depenetrationPush = 0.03f;

    Rigidbody2D rb;
    Enemy enemy;

    int animIndex;
    float animTimer;
    float lastWallTurnTime = -999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemy = GetComponent<Enemy>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void FixedUpdate()
    {
        // 掴まれ/投げられ/スタン中はAI停止
        if (enemy != null)
        {
            if (enemy.IsGrabbed) { rb.velocity = Vector2.zero; return; }
            if (enemy.IsFlying) return;
            if (enemy.IsShellStunned) { rb.velocity = Vector2.zero; return; }
        }

        // 地上チェック（必要なら利用）
        bool grounded = false;
        if (groundCheck) grounded = Physics2D.OverlapCircle(groundCheck.position, 0.08f, groundLayer);

        // 従来のWallLayer判定（通常壁用）
        bool hitWall = false;
        if (wallCheck)
            hitWall = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);

        if (hitWall && Time.time - lastWallTurnTime >= wallTurnCooldown)
        {
            lastWallTurnTime = Time.time;
            moveRight = !moveRight;
        }

        // 移動
        float vx = (moveRight ? 1f : -1f) * moveSpeed;
        rb.velocity = new Vector2(vx, rb.velocity.y);

        // 見た目
        if (spriteRenderer) spriteRenderer.flipX = moveRight;

        // アニメ
        if (walkSprites != null && walkSprites.Length > 0 && spriteRenderer)
        {
            animTimer += Time.fixedDeltaTime;
            float spf = 1f / Mathf.Max(0.01f, animFps);
            if (animTimer >= spf)
            {
                animTimer -= spf;
                animIndex = (animIndex + 1) % walkSprites.Length;
                spriteRenderer.sprite = walkSprites[animIndex];
            }
        }
    }

    bool TryGetSideNormal(Collision2D c, out float nx)
    {
        nx = 0f;
        if (c == null || c.contactCount == 0) return false;

        for (int i = 0; i < c.contactCount; i++)
        {
            var n = c.contacts[i].normal;
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
        if (!other) return true;
        if (other.isTrigger) return true;
        if (other.transform.IsChildOf(transform)) return true;

        if (other.GetComponentInParent<PlayerController>() != null) return true;
        if (other.GetComponentInParent<MawaruController>() != null) return true;

        return false;
    }

    void TurnFromSideCollision(float nx)
    {
        if (Time.time - lastWallTurnTime < wallTurnCooldown) return;
        lastWallTurnTime = Time.time;

        // normal.x > 0 → 左壁に当たった → 右へ
        // normal.x < 0 → 右壁に当たった → 左へ
        moveRight = nx > 0f;

        if (rb)
        {
            float push = Mathf.Max(0f, depenetrationPush);
            rb.position += new Vector2(moveRight ? push : -push, 0f);
        }
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (!turnOnAnySolidSideCollision) return;
        if (enemy != null && enemy.IsFlying) return;
        if (enemy != null && enemy.IsGrabbed) return;
        if (c == null || ShouldIgnoreCollider(c.collider)) return;

        if (TryGetSideNormal(c, out float nx))
            TurnFromSideCollision(nx);
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (!turnOnAnySolidSideCollision) return;
        if (enemy != null && enemy.IsFlying) return;
        if (enemy != null && enemy.IsGrabbed) return;
        if (c == null || ShouldIgnoreCollider(c.collider)) return;

        if (TryGetSideNormal(c, out float nx))
            TurnFromSideCollision(nx);
    }
}