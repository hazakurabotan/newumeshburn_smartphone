using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBoomerangThrower : MonoBehaviour
{
    [Header("Render")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] walkSprites;   // enemy14a~d（4枚）
    public float animFps = 6f;

    [Header("Move")]
    public bool stayInPlace = true;   // ★追加：その場固定
    public float moveSpeed = 1.2f;
    public bool moveRight = false;
    public bool faceMoveDirection = true;
    public bool spriteDefaultFacesRight = true;
    public bool lockYVelocity = true;

    [Header("Throw")]
    public Transform firePoint;
    public GameObject boomerangPrefab;  // EnemyBoomerangProjectile付き
    public float throwInterval = 1.6f;
    public float boomerangSpeed = 7f;
    public int boomerangDamage = 1;

    [Header("Target")]
    public Transform player; // 空ならTag=Playerから自動取得

    Rigidbody2D rb;
    float animT;
    float throwT;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void Update()
    {
        var e = GetComponent<Enemy>();
        if (e != null && (e.isGrabbed || e.isFlying)) return; // ★掴み/投げ中は発射も停止


        // 歩きアニメ
        if (walkSprites != null && walkSprites.Length > 0 && spriteRenderer)
        {
            animT += Time.deltaTime * Mathf.Max(0.1f, animFps);
            int idx = (int)animT % walkSprites.Length;
            spriteRenderer.sprite = walkSprites[idx];
        }

        // 投げ
        throwT += Time.deltaTime;
        if (throwT >= throwInterval)
        {
            throwT = 0f;
            ThrowBoomerang();
        }
    }

    void FixedUpdate()
    {
        var e = GetComponent<Enemy>();
        if (e != null)
        {
            if (e.isGrabbed)
            {
                rb.velocity = Vector2.zero;   // ★掴み中は完全停止
                return;
            }
            if (e.isFlying)
            {
                return; // ★投げ中は速度/位置を上書きしない（物理に任せる）
            }
        }

        if (stayInPlace)
        {
            // その場固定（Yを落とさない用）
            if (lockYVelocity) rb.velocity = new Vector2(0f, 0f);
            else rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        float sign = moveRight ? 1f : -1f;
        float vx = sign * moveSpeed;
        float vy = lockYVelocity ? 0f : rb.velocity.y;
        rb.velocity = new Vector2(vx, vy);

        if (faceMoveDirection && spriteRenderer)
            spriteRenderer.flipX = spriteDefaultFacesRight ? (sign < 0f) : (sign > 0f);
    }

    void ThrowBoomerang()
    {
        if (!boomerangPrefab || !firePoint) return;

        // 方向：基本はプレイヤー方向。いなければ進行方向
        Vector2 dir = moveRight ? Vector2.right : Vector2.left;
        if (player) dir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;

        var go = Instantiate(boomerangPrefab, firePoint.position, Quaternion.identity);

        var proj = go.GetComponent<EnemyBoomerangProjectile>();
        if (proj)
        {
            proj.Init(transform, dir, boomerangSpeed, boomerangDamage);
        }
        else
        {
            // 保険：Rigidbodyで飛ばすだけ
            var rb2 = go.GetComponent<Rigidbody2D>();
            if (rb2) rb2.velocity = dir * boomerangSpeed;
        }
    }
}