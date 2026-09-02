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

    [Header("Patrol Move")]
    [Tooltip("ONÇ…Ç∑ÇÈÇ∆ÅAï«Ç≈ÇÕÇ»Ç≠äJénà íuÇ©ÇÁàÍíËãóó£ÇæÇØìÆÇ¢ÇΩÇÁîΩì]ÇµÇ‹Ç∑")]
    public bool usePatrolDistance = true;

    [Tooltip("äJénà íuÇ©ÇÁç∂âEÇ…Ç«ÇÍÇæÇØìÆÇ¢ÇΩÇÁîΩì]Ç∑ÇÈÇ©")]
    public float patrolDistance = 0.8f;

    [Tooltip("îΩì]ÇµÇΩèuä‘Ç…è≠Çµé~Ç‹ÇÈéûä‘")]
    public float turnPauseSeconds = 0.05f;

    [Header("Shoot")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float shootInterval = 1.2f;
    public float bulletSpeed = 6f;
    public float bulletLife = 3f;

    [Header("Wall Flip")]
    [Tooltip("ï«Ç…Ç‘Ç¬Ç©ÇËÇªÇ§Ç»éûÇ…îΩì]ÇµÇ‹Ç∑ÅBãóó£Ç≈âùïúÇ≥ÇπÇΩÇ¢èÍçáÇÕOFFêÑèß")]
    public bool flipWhenBlocked = false;

    [Tooltip("ëOï˚Ç…Ç±ÇÍÇæÇØCastÇµÇƒï«Çåüím")]
    public float wallCheckDistance = 0.12f;

    [Tooltip("ï«îΩì]éûÇ…è≠ÇµâüÇµñﬂÇ∑ãóó£")]
    public float unstuckDistance = 0.03f;

    [Tooltip("òAë±îΩì]ñhé~ÉNÅ[ÉãÉ_ÉEÉì")]
    public float flipCooldown = 0.10f;

    Rigidbody2D rb;
    Collider2D col;
    Enemy enemy;

    float baseY;
    float startX;

    float animTimer;
    int animIndex;

    float shootTimer;
    float flipCooldownTimer;
    float turnPauseTimer;

    readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    ContactFilter2D castFilter;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        enemy = GetComponentInParent<Enemy>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        baseY = rb.position.y;
        startX = rb.position.x;

        castFilter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = false
        };
    }

    void OnEnable()
    {
        if (rb != null)
        {
            baseY = rb.position.y;
            startX = rb.position.x;
        }

        flipCooldownTimer = 0f;
        turnPauseTimer = 0f;
    }

    void OnValidate()
    {
        if (moveSpeed < 0f) moveSpeed = 0f;
        if (animFps < 0.01f) animFps = 0.01f;
        if (floatFrequency < 0f) floatFrequency = 0f;
        if (patrolDistance < 0.01f) patrolDistance = 0.01f;
        if (turnPauseSeconds < 0f) turnPauseSeconds = 0f;
        if (wallCheckDistance < 0f) wallCheckDistance = 0f;
        if (unstuckDistance < 0f) unstuckDistance = 0f;
        if (flipCooldown < 0f) flipCooldown = 0f;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (flipCooldownTimer > 0f)
            flipCooldownTimer -= Time.fixedDeltaTime;

        if (turnPauseTimer > 0f)
            turnPauseTimer -= Time.fixedDeltaTime;

        if (enemy != null)
        {
            if (enemy.IsThrown || enemy.IsGrabbed || enemy.IsShellStunned)
                return;
        }

        float dirX = moveRight ? 1f : -1f;

        if (usePatrolDistance && flipCooldownTimer <= 0f)
        {
            float rightLimit = startX + Mathf.Abs(patrolDistance);
            float leftLimit = startX - Mathf.Abs(patrolDistance);

            if (moveRight && rb.position.x >= rightLimit)
            {
                moveRight = false;
                flipCooldownTimer = flipCooldown;
                turnPauseTimer = turnPauseSeconds;

                rb.position = new Vector2(rightLimit, rb.position.y);
                dirX = -1f;
            }
            else if (!moveRight && rb.position.x <= leftLimit)
            {
                moveRight = true;
                flipCooldownTimer = flipCooldown;
                turnPauseTimer = turnPauseSeconds;

                rb.position = new Vector2(leftLimit, rb.position.y);
                dirX = 1f;
            }
        }

        if (flipWhenBlocked && flipCooldownTimer <= 0f)
        {
            if (IsBlocked(dirX))
            {
                moveRight = !moveRight;
                flipCooldownTimer = flipCooldown;
                turnPauseTimer = turnPauseSeconds;

                rb.position += new Vector2(-dirX * unstuckDistance, 0f);
                dirX = moveRight ? 1f : -1f;
            }
        }

        float y = baseY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        Vector2 pos = rb.position;

        if (turnPauseTimer <= 0f)
            pos.x += dirX * moveSpeed * Time.fixedDeltaTime;

        pos.y = y;

        rb.MovePosition(pos);

        if (spriteRenderer != null)
            spriteRenderer.flipX = moveRight;

        shootTimer += Time.fixedDeltaTime;
        if (shootTimer >= shootInterval)
        {
            shootTimer = 0f;
            TryShoot(dirX);
        }
    }

    bool IsBlocked(float dirX)
    {
        if (col == null) return false;

        Vector2 dir = new Vector2(dirX, 0f);

        int hitCount = col.Cast(dir, castFilter, castHits, wallCheckDistance);
        if (hitCount <= 0) return false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D h = castHits[i];
            if (h.collider == null) continue;

            if (h.collider.transform.IsChildOf(transform)) continue;

            if (h.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (h.collider.GetComponentInParent<MawaruController>() != null) continue;

            if (h.collider.GetComponentInParent<Enemy>() != null) continue;

            return true;
        }

        return false;
    }

    void Update()
    {
        if (flySprites != null && flySprites.Length > 0 && spriteRenderer != null)
        {
            animTimer += Time.deltaTime;
            float frameTime = 1f / Mathf.Max(0.01f, animFps);

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
        if (bulletPrefab == null) return;
        if (firePoint == null) return;

        GameObject go = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

        Rigidbody2D rb2 = go.GetComponent<Rigidbody2D>();
        if (rb2 != null)
        {
            rb2.velocity = new Vector2(dirX * bulletSpeed, 0f);
        }

        Destroy(go, bulletLife);
    }

    void OnDrawGizmosSelected()
    {
        float centerX = Application.isPlaying ? startX : transform.position.x;
        float d = Mathf.Abs(patrolDistance);

        Vector3 center = new Vector3(centerX, transform.position.y, transform.position.z);
        Vector3 left = center + Vector3.left * d;
        Vector3 right = center + Vector3.right * d;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left, 0.06f);
        Gizmos.DrawWireSphere(right, 0.06f);
    }
}