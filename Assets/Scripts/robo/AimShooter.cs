using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AimShooter : MonoBehaviour
{
    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public Transform muzzle;
    public Transform cursor;

    public float bulletSpeed = 20f;
    public float fireInterval = 0.1f;
    public float bulletLifeTime = 3f;

    [Header("Bullet Visual Settings")]
    public float bulletScale = 1f;
    public int bulletSortingOrder = 20;
    public bool rotateBulletToMoveDirection = true;
    public float bulletRotationOffsetZ = 0f;

    [Header("Boss Hit Effect Settings")]
    public GameObject bossHitEffectPrefab;
    public int bulletDamageToBoss = 1;
    public float bossHitEffectScale = 0.15f;
    public float bossHitEffectLifeTime = 0.2f;
    public Vector3 bossHitEffectOffset = Vector3.zero;

    [Header("Input Fallback")]
    [Tooltip("RoboBattleControllerからTryShootが呼ばれない時の保険。Button Southで直接撃つ。")]
    public bool useDirectGamepadShootFallback = true;

    [Header("Debug")]
    public bool debugLog = false;

    private float nextFireTime = 0f;
    private BeamAmmo ammo;

    private void Awake()
    {
        ammo = GetComponent<BeamAmmo>();
    }

    private void Update()
    {
        if (!useDirectGamepadShootFallback) return;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
        {
            TryShoot();
        }
#endif
    }

    public void TryShoot()
    {
        if (Time.time < nextFireTime) return;

        if (bulletPrefab == null)
        {
            Debug.LogWarning("[AimShooter] Bullet Prefab が未設定です。IMG_0726.prefab を入れてください。");
            return;
        }

        if (muzzle == null)
        {
            Debug.LogWarning("[AimShooter] Muzzle が未設定です。");
            return;
        }

        if (cursor == null)
        {
            Debug.LogWarning("[AimShooter] Cursor が未設定です。");
            return;
        }

        if (ammo != null)
        {
            if (!ammo.TryConsume())
            {
                if (debugLog)
                {
                    Debug.Log("[AimShooter] 弾数不足、または同時発射数の上限で撃てません。");
                }

                return;
            }
        }

        Fire();

        nextFireTime = Time.time + fireInterval;
    }

    private void Fire()
    {
        Vector3 spawnPos = muzzle.position;
        spawnPos.z = 0f;

        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bulletObj.name = bulletPrefab.name + "_Shot";

        Vector2 dir = (cursor.position - muzzle.position).normalized;
        if (dir.sqrMagnitude < 0.0001f)
        {
            dir = Vector2.right;
        }

        SetupBulletVisual(bulletObj, dir);
        SetupBulletPhysics(bulletObj, dir);
        SetupBulletHitEffect(bulletObj);

        if (ammo != null)
        {
            ammo.RegisterBullet(bulletObj);
        }

        if (debugLog)
        {
            Debug.Log("[AimShooter] Fire IMG_0726 bullet. dir = " + dir);
        }
    }

    private void SetupBulletVisual(GameObject bulletObj, Vector2 dir)
    {
        bulletObj.transform.localScale = Vector3.one * bulletScale;

        if (rotateBulletToMoveDirection)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            bulletObj.transform.rotation = Quaternion.Euler(0f, 0f, angle + bulletRotationOffsetZ);
        }

        SpriteRenderer sr = bulletObj.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = bulletObj.GetComponentInChildren<SpriteRenderer>();
        }

        if (sr != null)
        {
            sr.enabled = true;
            sr.sortingOrder = bulletSortingOrder;
        }
        else
        {
            Debug.LogWarning("[AimShooter] Bullet Prefab に SpriteRenderer がありません。IMG_0726.prefab を確認してください。");
        }
    }

    private void SetupBulletPhysics(GameObject bulletObj, Vector2 dir)
    {
        Rigidbody2D rb = bulletObj.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = bulletObj.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
        rb.velocity = dir * bulletSpeed;

        Collider2D col = bulletObj.GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = bulletObj.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.2f;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void SetupBulletHitEffect(GameObject bulletObj)
    {
        RoboBattleAimBulletHitEffect hitEffect = bulletObj.GetComponent<RoboBattleAimBulletHitEffect>();
        if (hitEffect == null)
        {
            hitEffect = bulletObj.AddComponent<RoboBattleAimBulletHitEffect>();
        }

        hitEffect.Init(
            bossHitEffectPrefab,
            bulletDamageToBoss,
            bulletLifeTime,
            bossHitEffectScale,
            bossHitEffectLifeTime,
            bossHitEffectOffset
        );
    }
}

public class RoboBattleAimBulletHitEffect : MonoBehaviour
{
    private GameObject hitEffectPrefab;
    private int damageToBoss;
    private float lifeTime;
    private float effectScale;
    private float effectLifeTime;
    private Vector3 effectOffset;

    private bool consumed = false;

    public void Init(
        GameObject effectPrefab,
        int damage,
        float bulletLifeTime,
        float hitEffectScale,
        float hitEffectLifeTime,
        Vector3 hitEffectOffset
    )
    {
        hitEffectPrefab = effectPrefab;
        damageToBoss = damage;
        lifeTime = bulletLifeTime;
        effectScale = hitEffectScale;
        effectLifeTime = hitEffectLifeTime;
        effectOffset = hitEffectOffset;

        CancelInvoke();

        if (lifeTime > 0f)
        {
            Invoke(nameof(DestroySelf), lifeTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        TryHitBoss(other.gameObject, hitPoint);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector3 hitPoint = transform.position;

        if (collision.contactCount > 0)
        {
            hitPoint = collision.GetContact(0).point;
        }

        TryHitBoss(collision.gameObject, hitPoint);
    }

    private void TryHitBoss(GameObject hitObject, Vector3 hitPoint)
    {
        if (consumed) return;
        if (hitObject == null) return;

        BossHP bossHP =
            hitObject.GetComponent<BossHP>() ??
            hitObject.GetComponentInParent<BossHP>() ??
            hitObject.GetComponentInChildren<BossHP>();

        if (bossHP == null) return;

        consumed = true;

        SpawnHitEffect(hitPoint);

        if (damageToBoss > 0)
        {
            bossHP.TakeDamage(damageToBoss);
        }

        Destroy(gameObject);
    }

    private void SpawnHitEffect(Vector3 hitPoint)
    {
        if (hitEffectPrefab == null) return;

        Vector3 spawnPos = hitPoint + effectOffset;
        spawnPos.z = 0f;

        GameObject effectObj = Instantiate(hitEffectPrefab, spawnPos, Quaternion.identity);
        effectObj.transform.localScale = effectObj.transform.localScale * effectScale;

        SpriteRenderer sr = effectObj.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = effectObj.GetComponentInChildren<SpriteRenderer>();
        }

        if (sr != null)
        {
            sr.sortingOrder = 30;
        }

        if (effectLifeTime > 0f)
        {
            Destroy(effectObj, effectLifeTime);
        }
    }

    private void DestroySelf()
    {
        if (consumed) return;

        consumed = true;
        Destroy(gameObject);
    }
}