using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerBombProjectile : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField] private float fuseTime = 2.0f;
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private float baseExplosionRadius = 0.9f;

    [Header("Throw")]
    [SerializeField] private float throwSpeedX = 4.8f;
    [SerializeField] private float throwSpeedY = 5.2f;
    [SerializeField] private float maxLife = 5.0f;
    [SerializeField] private float spinSpeed = -540f;

    [Header("Effect")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private float explosionEffectLife = 0.7f;

    [Header("Gizmo")]
    [SerializeField] private bool showExplosionGizmo = true;

    Rigidbody2D rb;
    Collider2D selfCol;

    PlayerController owner;
    Collider2D ownerCollider;

    float explodeAtTime;
    bool exploded = false;
    bool initialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        selfCol = GetComponent<Collider2D>();
    }

    void Start()
    {
        if (!initialized)
        {
            explodeAtTime = Time.time + fuseTime;
            if (rb != null)
            {
                rb.angularVelocity = spinSpeed;
            }
        }

        Destroy(gameObject, maxLife);
    }

    void Update()
    {
        if (exploded) return;

        if (Time.time >= explodeAtTime)
        {
            Explode();
        }
    }

    public void Initialize(
        PlayerController owner,
        Collider2D ownerCollider,
        float dir,
        float speedX,
        float speedY,
        int damage,
        float radius,
        float fuse,
        GameObject effectPrefab,
        float effectLife
    )
    {
        this.owner = owner;
        this.ownerCollider = ownerCollider;

        throwSpeedX = speedX;
        throwSpeedY = speedY;
        baseDamage = damage;
        baseExplosionRadius = radius;
        fuseTime = fuse;
        explosionEffectPrefab = effectPrefab;
        explosionEffectLife = effectLife;

        explodeAtTime = Time.time + fuseTime;
        initialized = true;

        if (rb != null)
        {
            rb.velocity = new Vector2(dir * throwSpeedX, throwSpeedY);
            rb.angularVelocity = spinSpeed;
        }

        if (selfCol != null && ownerCollider != null)
        {
            Physics2D.IgnoreCollision(selfCol, ownerCollider, true);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (exploded) return;
        if (rb == null) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 normal = collision.GetContact(i).normal;
            if (normal.y > 0.5f)
            {
                rb.angularVelocity = 0f;
                return;
            }
        }
    }

    void Explode()
    {
        if (exploded) return;
        exploded = true;

        int finalDamage = baseDamage;
        float finalRadius = baseExplosionRadius;

        MdDeskPageController mdDesk = FindMdDeskPageController();
        if (mdDesk != null &&
            mdDesk.GetCurrentCharacter() == MdDeskPageController.CharacterKind.Player &&
            mdDesk.CurrentEffectType == MdDeskPageController.DiskEffectType.AttackUp)
        {
            finalDamage *= 2;
            finalRadius *= 2f;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, finalRadius);
        HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null) continue;
            if (damagedEnemies.Contains(enemy)) continue;

            damagedEnemies.Add(enemy);
            enemy.TakeDamage(finalDamage, "PlayerBomb");
        }

        if (explosionEffectPrefab != null)
        {
            GameObject fx = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            if (explosionEffectLife > 0f)
            {
                Destroy(fx, explosionEffectLife);
            }
        }

        Destroy(gameObject);
    }

    MdDeskPageController FindMdDeskPageController()
    {
        MdDeskPageController[] all = FindObjectsOfType<MdDeskPageController>(true);
        if (all == null || all.Length == 0) return null;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].gameObject.scene.IsValid())
            {
                return all[i];
            }
        }

        return null;
    }

    void OnDrawGizmosSelected()
    {
        if (!showExplosionGizmo) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, baseExplosionRadius);
    }
}