using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyStraightMouth : MonoBehaviour
{

    [Header("Facing")]
    public bool spriteDefaultFacesRight = true; // もしデフォが左向きならfalse

    [Header("Render")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] mouthSprites = new Sprite[3]; // 1,2,3

    [Header("Move (Straight)")]
    public float moveSpeed = 3.5f;
    [Tooltip("true=右へ / false=左へ")]
    public bool moveRight = false;
    public bool faceMoveDirection = true;
    public bool lockYVelocity = true;

    [Header("Mouth Anim (Slow)")]
    [Tooltip("口パクの速度。小さいほどゆっくり（例: 2〜6）")]
    public float slowFps = 3f;

    [Header("Contact Damage")]
    public int contactDamage = 1;
    public float hitCooldown = 0.5f;
    public string playerTag = "Player"; // Player/mawaru13を同じTagにするのが楽

    [Header("Knockback (when enemy is hit)")]
    public float knockbackForce = 7f;
    public float knockbackUp = 1.5f;   // 完全水平なら0
    public float stunTime = 0.15f;


    [Header("Despawn on Wall")]
    public float despawnDelay = 1f;
    public LayerMask wallLayers; // Wallレイヤーを指定

    bool dying;

    Rigidbody2D rb;
    bool stunned;
    float lastHitTime;

    int frame;
    float frameProgress;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (mouthSprites != null && mouthSprites.Length >= 1 && mouthSprites[0] != null)
            spriteRenderer.sprite = mouthSprites[0];
    }

    void Update()
    {
        if (mouthSprites == null || mouthSprites.Length < 3) return;

        // 口パクをゆっくり（固定FPS）
        frameProgress += Time.deltaTime * Mathf.Max(0.1f, slowFps);
        while (frameProgress >= 1f)
        {
            frameProgress -= 1f;
            frame = (frame + 1) % 3;
            spriteRenderer.sprite = mouthSprites[frame];
        }
    }

    void FixedUpdate()
    {
        var e = GetComponent<Enemy>();

        // ★掴み中：完全停止（移動スクリプトが速度を上書きしない）
        if (e != null && e.isGrabbed)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // ★投げられて飛行中：移動スクリプトで速度を上書きしない（物理に任せる）
        if (e != null && e.isFlying)
        {
            return;
        }

        if (stunned)
        {
            if (lockYVelocity) rb.velocity = new Vector2(rb.velocity.x, 0f);
            return;
        }

        float sign = moveRight ? 1f : -1f;
        float vx = sign * moveSpeed;
        float vy = lockYVelocity ? 0f : rb.velocity.y;
        rb.velocity = new Vector2(vx, vy);

        if (faceMoveDirection && spriteRenderer)
            spriteRenderer.flipX = spriteDefaultFacesRight ? (sign < 0f) : (sign > 0f);
    }

    // 接触ダメ（敵のCollider2Dは IsTrigger ON 推奨）
    void OnCollisionEnter2D(Collision2D col)
    {
        var e = GetComponent<Enemy>();
        if (e != null && e.isFlying) return; // ★飛行中は消さない

        if (((1 << col.gameObject.layer) & wallLayers) != 0)
            Destroy(gameObject);
    }

    IEnumerator DespawnAfter()
    {
        yield return new WaitForSeconds(despawnDelay);
        Destroy(gameObject);
    }

    // プレイヤー攻撃側から呼ぶ：ノックバック
    public void TakeHit(int damage, Vector2 attackerPos, float kbForce = 7f, float kbUp = 1.5f)
    {
        var enemy = GetComponent<Enemy>();
        if (enemy) enemy.TakeDamage(damage);

        Vector2 dir = ((Vector2)transform.position - attackerPos).normalized;
        rb.velocity = Vector2.zero;
        rb.AddForce(new Vector2(dir.x * kbForce, kbUp), ForceMode2D.Impulse);

        StartCoroutine(StunCoroutine()); // ★追加
    }

    IEnumerator StunCoroutine()
    {
        stunned = true;
        yield return new WaitForSeconds(stunTime);
        stunned = false;
    }

    public void KnockbackFrom(Vector2 attackerPos)
    {
        // これ重要：無効状態なら何もしない
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;
        if (dying) return; // 壁消滅などの途中なら無視（dying使ってるなら）

        if (!rb) rb = GetComponent<Rigidbody2D>();

        Vector2 dir = ((Vector2)transform.position - attackerPos).normalized;
        rb.velocity = Vector2.zero;
        rb.AddForce(new Vector2(dir.x * knockbackForce, knockbackUp), ForceMode2D.Impulse);

        StartCoroutine(StunCoroutine());
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var e = GetComponent<Enemy>();
        if (e != null && e.isFlying) return; // ★飛行中は消さない

        if (((1 << other.gameObject.layer) & wallLayers) != 0)
            Destroy(gameObject);
    }


}