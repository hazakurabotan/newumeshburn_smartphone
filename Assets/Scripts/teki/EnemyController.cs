using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public GameObject bulletPrefab;
    public float bulletSpeed = 6f;
    public float shootInterval = 2.0f;
    float shootTimer;

    public Transform firePoint;

    public float speed = 3.0f;
    public bool isToRight = false;
    public float revTime = 0f;
    public LayerMask groundLayer;

    float time;
    Enemy enemy;

    // ★ 追加：毎フレーム、操作中キャラを拾う
    Transform Target => CharacterSwitchManager.ActiveTarget;

    void Start()
    {
        enemy = GetComponent<Enemy>();
        if (isToRight) transform.localScale = new Vector2(-1, 1);
    }

    void Update()
    {
        // ===== 自動反転 =====
        if (revTime > 0f)
        {
            time += Time.deltaTime;
            if (time >= revTime)
            {
                isToRight = !isToRight;
                time = 0f;
                transform.localScale = isToRight ? new Vector2(-1, 1) : new Vector2(1, 1);
            }
        }

        // ★ ここを「ActiveTarget がいるときだけ撃つ」に変更
        var tgt = Target;
        if (tgt != null)
        {
            shootTimer += Time.deltaTime;
            if (shootTimer >= shootInterval)
            {
                ShootTo(tgt.position);
                shootTimer = 0f;
            }
        }
    }

    // ★ 位置を受け取って撃つ
    void ShootTo(Vector3 targetPos)
    {
        Vector3 shootPos = firePoint ? firePoint.position : transform.position;
        Vector2 dir = (targetPos - shootPos).normalized;

        var bullet = Instantiate(bulletPrefab, shootPos, Quaternion.identity);
        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.gravityScale = 0f;          // ←落下しないように
            rb.velocity = dir * bulletSpeed;
            rb.freezeRotation = true;
        }
    }

    void FixedUpdate()
    {
        if (enemy != null && enemy.isFlying) return;

        bool onGround = Physics2D.CircleCast(transform.position, 0.5f, Vector2.down, 0.5f, groundLayer);
        if (onGround)
        {
            var rbody = GetComponent<Rigidbody2D>();
            if (rbody)
            {
                float moveX = isToRight ? speed : -speed;
                rbody.velocity = new Vector2(moveX, rbody.velocity.y);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        isToRight = !isToRight;
        time = 0f;
        transform.localScale = isToRight ? new Vector2(-1, 1) : new Vector2(1, 1);
    }
}
