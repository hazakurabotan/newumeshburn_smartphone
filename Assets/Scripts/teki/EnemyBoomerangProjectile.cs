using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyBoomerangProjectile : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public string playerTag = "Player";

    [Header("Motion")]
    public float speed = 7f;
    public float outboundTime = 0.45f;     // これだけ進んだら戻る（時間）
    public float maxDistance = 6f;         // もしくは距離で戻す（0なら無効）
    public float returnHoming = 12f;       // 戻りの追従強さ（大きいほど手元に吸い付く）
    public float lifeTime = 4f;            // 保険
    public LayerMask hitWallLayers;        // Ground/Wallに当たったら消す（任意）

    Rigidbody2D rb;
    Transform owner;
    Vector2 dir;
    float t;
    Vector2 spawnPos;
    bool returning;

    public void Init(Transform ownerTransform, Vector2 shootDir, float spd, int dmg)
    {
        owner = ownerTransform;
        dir = shootDir.normalized;
        speed = spd;
        damage = dmg;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // ぶつかり判定はTrigger推奨

        spawnPos = transform.position;
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        t += Time.fixedDeltaTime;

        // 戻る条件：時間 or 距離
        if (!returning)
        {
            if (t >= outboundTime) returning = true;
            if (maxDistance > 0f && Vector2.Distance(spawnPos, rb.position) >= maxDistance) returning = true;
        }

        if (!returning)
        {
            rb.velocity = dir * speed;
            return;
        }

        // 戻り：敵の位置へホーミング
        if (owner == null) { Destroy(gameObject); return; }

        Vector2 toOwner = ((Vector2)owner.position - rb.position);
        Vector2 desiredVel = toOwner.normalized * speed;

        // 速度をなめらかに手元へ寄せる（ブーメランっぽく）
        rb.velocity = Vector2.Lerp(rb.velocity, desiredVel, returnHoming * Time.fixedDeltaTime);

        // 近づいたら回収
        if (toOwner.sqrMagnitude < 0.2f * 0.2f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 壁・床なら消す（任意）
        if ((hitWallLayers.value & (1 << other.gameObject.layer)) != 0)
        {
            Destroy(gameObject);
            return;
        }

        if (!other.CompareTag(playerTag)) return;

        // Mawaru優先
        var ma = other.GetComponent<MawaruController>();
        if (ma != null)
        {
            int dirHit = (other.transform.position.x - transform.position.x) >= 0 ? 1 : -1;
            ma.TakeDamage(damage, dirHit);
            return;
        }

        // PlayerController
        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage(damage);
            return;
        }
    }
}