using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnergyItem : MonoBehaviour
{
    [Header("Value")]
    public float energyAmount = 5f;

    [Header("Life")]
    [Min(0f)] public float lifeTime = 10f;

    [Header("Motion")]
    [Min(0f)] public float floatAmplitude = 0.15f;
    [Min(0f)] public float floatSpeed = 3f;

    [Header("Magnet")]
    [Min(0f)] public float magnetDistance = 2f;
    [Min(0f)] public float magnetSpeed = 8f;

    private Vector3 basePosition;
    private float spawnTime;
    private ImpactRunnerController player;
    private bool collected;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        basePosition = transform.position;
        spawnTime = Time.time;
    }

    private void Start()
    {
        player = FindObjectOfType<ImpactRunnerController>();
    }

    private void Update()
    {
        if (collected)
            return;

        if (lifeTime > 0f && Time.time >= spawnTime + lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist <= magnetDistance)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    player.transform.position,
                    magnetSpeed * Time.deltaTime
                );
                return;
            }
        }

        Vector3 p = basePosition;
        p.y += Mathf.Sin((Time.time - spawnTime) * floatSpeed) * floatAmplitude;
        transform.position = p;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        ImpactRunnerController runner = other.GetComponentInParent<ImpactRunnerController>();
        if (runner == null)
            return;

        Collect();
    }

    private void Collect()
    {
        collected = true;
        ImpactRunGameManager.Instance?.AddEnergy(energyAmount);
        Destroy(gameObject);
    }
}