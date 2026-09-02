using UnityEngine;

public class RoboBattleBossBulletHitEffect : MonoBehaviour
{
    [Header("Boss Hit")]
    public int damageToBoss = 1;

    [Header("Effect")]
    public GameObject hitEffectPrefab;
    public Vector3 effectOffset = Vector3.zero;
    public float effectScale = 0.15f;
    public float effectLifeTime = 0.2f;

    [Header("Bullet")]
    public float lifeTime = 3f;
    public bool destroyBulletOnBossHit = true;

    private bool consumed = false;

    private void Awake()
    {
        ResetLifeTimer();
    }

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

        ResetLifeTimer();
    }

    private void ResetLifeTimer()
    {
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
            Debug.Log("[RoboBattleBossBulletHitEffect] Boss hit. damage = " + damageToBoss);
        }
        else
        {
            Debug.Log("[RoboBattleBossBulletHitEffect] Boss hit. effect only.");
        }

        if (destroyBulletOnBossHit)
        {
            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect(Vector3 hitPoint)
    {
        if (hitEffectPrefab == null)
        {
            Debug.LogWarning("[RoboBattleBossBulletHitEffect] hitEffectPrefab ‚ª–¢Ý’è‚Å‚·B");
            return;
        }

        Vector3 spawnPos = hitPoint + effectOffset;

        GameObject effectObj = Instantiate(hitEffectPrefab, spawnPos, Quaternion.identity);
        effectObj.transform.localScale = effectObj.transform.localScale * effectScale;

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