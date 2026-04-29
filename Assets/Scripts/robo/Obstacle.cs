using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Stats")]
    [Min(1)] public int maxHP = 1;
    public bool unbreakable = false;

    [Header("Energy Reward")]
    [Min(0f)] public float energyReward = 10f;
    public EnergyItem energyItemPrefab;
    [Min(0)] public int dropCount = 0;
    [Min(0f)] public float dropSpread = 0.5f;

    [Header("Touch Damage")]
    public bool damagePlayerOnTouch = true;
    [Min(0)] public int touchDamage = 1;
    [Min(0f)] public float touchDamageCooldown = 0.35f;
    public bool breakWhenDashed = true;

    [Header("Destroy Effect")]
    public GameObject destroyEffect;
    public Vector3 destroyEffectOffset = Vector3.zero;

    private int currentHP;
    private float lastTouchTime = -999f;
    private bool destroyed;

    private void Awake()
    {
        currentHP = maxHP;
    }

    public void TakeHit(int damage, Vector2 hitPoint, ImpactRunnerController attacker = null)
    {
        if (destroyed)
            return;

        if (unbreakable)
            return;

        currentHP -= damage;

        if (currentHP <= 0)
            BreakObstacle();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerContact(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayerContact(other);
    }

    private void HandlePlayerContact(Collider2D other)
    {
        if (destroyed)
            return;

        ImpactRunnerController player = other.GetComponentInParent<ImpactRunnerController>();
        if (player == null)
            return;

        if (breakWhenDashed && player.IsDashing && !unbreakable)
        {
            BreakObstacle();
            return;
        }

        if (!damagePlayerOnTouch)
            return;

        if (Time.time < lastTouchTime + touchDamageCooldown)
            return;

        lastTouchTime = Time.time;
        player.TakeDamage(touchDamage, transform.position);
    }

    private void BreakObstacle()
    {
        if (destroyed)
            return;

        destroyed = true;

        if (destroyEffect != null)
            Instantiate(destroyEffect, transform.position + destroyEffectOffset, Quaternion.identity);

        if (energyItemPrefab != null && dropCount > 0)
        {
            float eachValue = energyReward / Mathf.Max(1, dropCount);

            for (int i = 0; i < dropCount; i++)
            {
                Vector2 offset = Random.insideUnitCircle * dropSpread;
                EnergyItem item = Instantiate(
                    energyItemPrefab,
                    (Vector2)transform.position + offset,
                    Quaternion.identity
                );
                item.energyAmount = eachValue;
            }
        }
        else
        {
            ImpactRunGameManager.Instance?.AddEnergy(energyReward);
        }

        Destroy(gameObject);
    }
}