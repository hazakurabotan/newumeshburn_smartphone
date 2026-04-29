using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2f;
    public bool moveRight = true;

    [Header("Wall Check (Trigger)")]
    [Tooltip("壁検知用のTriggerに当たったら反転（従来通り）")]
    public bool flipOnTrigger = true;

    [Header("Wall Check (Collision)")]
    [Tooltip("回転部屋など“Triggerじゃない壁”に当たった時も反転する")]
    public bool flipOnCollision = true;

    [Tooltip("連続反転（ガタガタ）防止のクールダウン")]
    public float flipCooldown = 0.08f;

    [Tooltip("横方向の接触（壁）とみなす法線しきい値。大きいほど厳しめ")]
    [Range(0.0f, 1.0f)]
    public float sideNormalThreshold = 0.55f;

    Rigidbody2D rb;
    Enemy enemy; // 投げられ中判定に使う（なければnull）

    float flipCooldownTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemy = GetComponentInParent<Enemy>();
    }

    void FixedUpdate()
    {
        if (flipCooldownTimer > 0f) flipCooldownTimer -= Time.fixedDeltaTime;

        // ★投げられ中 / 掴まれ中は Enemy.cs 側の物理に任せる
        if (enemy != null && (enemy.IsThrown || enemy.IsGrabbed)) return;

        float dir = moveRight ? 1f : -1f;
        rb.velocity = new Vector2(dir * speed, rb.velocity.y);
    }

    public void Flip()
    {
        moveRight = !moveRight;

        // 見た目
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.flipX = moveRight;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!flipOnTrigger) return;
        if (other == null) return;
        if (!other.isTrigger) return;

        // ★投げられ中 / 掴まれ中は反転させない
        if (enemy != null && (enemy.IsThrown || enemy.IsGrabbed)) return;

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

        // ★投げられ中 / 掴まれ中は反転させない（Enemy.csの投げ処理を守る）
        if (enemy != null && (enemy.IsThrown || enemy.IsGrabbed)) return;

        // プレイヤー系に当たっただけでは反転しない（必要なら）
        if (col.collider.GetComponentInParent<PlayerController>() != null) return;
        if (col.collider.GetComponentInParent<MawaruController>() != null) return;

        // 壁（横方向の接触）なら反転
        for (int i = 0; i < col.contactCount; i++)
        {
            var c = col.GetContact(i);
            if (Mathf.Abs(c.normal.x) >= sideNormalThreshold)
            {
                Flip();
                flipCooldownTimer = flipCooldown;
                return;
            }
        }
    }
}