using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    public int damage = 1;

    [Header("Hit Layers")]
    public LayerMask hitWallLayers;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    // Åöí«â¡ÅFî≠éÀ
    public void Launch(Vector2 dir, float speed, float lifeTime = 3f)
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();
        rb.velocity = dir.normalized * speed;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInLayerMask(other.gameObject.layer, hitWallLayers))
        {
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Player"))
        {
            var ma = other.GetComponent<MawaruController>();
            if (ma != null)
            {
                int dir = (other.transform.position.x - transform.position.x) >= 0 ? 1 : -1;
                ma.TakeDamage(damage, dir);
                Destroy(gameObject);
                return;
            }

            var pc = other.GetComponent<PlayerController>();
            if (pc != null) { pc.TakeDamage(damage); Destroy(gameObject); return; }
        }

        if (!other.isTrigger) Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (IsInLayerMask(col.gameObject.layer, hitWallLayers))
        {
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
        => (mask.value & (1 << layer)) != 0;
}