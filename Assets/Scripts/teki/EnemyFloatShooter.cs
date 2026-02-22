using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyFloatShooter : MonoBehaviour
{
    [Header("Render")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] flySprites;
    public float animFps = 6f;

    [Header("Move")]
    public float moveSpeed = 1.5f;
    public bool moveRight = false;
    public float floatAmplitude = 0.25f;
    public float floatFrequency = 1.2f;

    [Header("Shoot")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float shootInterval = 1.2f;
    public float bulletSpeed = 6f;
    public float bulletLife = 3f;

    [Header("Facing")]
    public bool spriteDefaultFacesRight = true;

    Rigidbody2D rb;
    Enemy enemy;
    Vector2 basePos;
    float animT;
    float shootT;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        enemy = GetComponent<Enemy>();

        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        basePos = rb.position;
    }

    void Update()
    {

        // ★掴み中/投げ飛行中は一切更新しない（アニメも弾も止める）
        if (enemy != null && (enemy.isGrabbed || enemy.isFlying))
            return;

        // アニメ
        if (flySprites != null && flySprites.Length > 0 && spriteRenderer)
        {
            animT += Time.deltaTime * Mathf.Max(0.1f, animFps);
            int idx = (int)animT % flySprites.Length;
            spriteRenderer.sprite = flySprites[idx];
        }

        // 発射
        shootT += Time.deltaTime;
        if (shootT >= shootInterval)
        {
            shootT = 0f;
            Shoot();
        }
    }

    void FixedUpdate()
    {
        // ★掴み中/投げ飛行中は MovePosition しない（これがスタック/停止の原因）
        if (enemy != null && (enemy.isGrabbed || enemy.isFlying))
            return;

        float sign = moveRight ? 1f : -1f;

        float x = rb.position.x + sign * moveSpeed * Time.fixedDeltaTime;
        float y = basePos.y + Mathf.Sin(Time.time * floatFrequency * Mathf.PI * 2f) * floatAmplitude;

        rb.MovePosition(new Vector2(x, y));

        if (spriteRenderer)
            spriteRenderer.flipX = spriteDefaultFacesRight ? (sign < 0f) : (sign > 0f);
    }

    void Shoot()
    {
        if (!bulletPrefab || !firePoint) return;

        var go = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Vector2 dir = (moveRight ? Vector2.right : Vector2.left);

        var b = go.GetComponent<EnemyBullet>();
        if (b) b.Launch(dir, bulletSpeed, bulletLife);
        else
        {
            var rb2 = go.GetComponent<Rigidbody2D>();
            if (rb2) rb2.velocity = dir * bulletSpeed;
            Destroy(go, bulletLife);
        }
    }

    // ★投げ終了後にふわふわ基準位置を更新したい時に呼ぶ
    public void ResetFloatBase()
    {
        basePos = rb.position;
    }
}