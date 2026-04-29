using UnityEngine;

public class PlayerBombThrow : MonoBehaviour
{
    [Header("Bomb")]
    public GameObject bombPrefab;
    public Transform throwPoint;
    public float bombCooldown = 0.35f;
    public float throwSpeedX = 4.8f;
    public float throwSpeedY = 5.2f;
    public float spawnOffsetX = 0.28f;
    public float spawnOffsetY = 0.18f;

    [Header("Explosion")]
    public int bombDamage = 1;
    public float explosionRadius = 0.9f;
    public float fuseTime = 2.0f;
    public GameObject explosionEffectPrefab;
    public float explosionEffectLife = 0.7f;

    [Header("SE")]
    public AudioSource seSource;
    public AudioClip throwSe;

    PlayerController playerController;
    CapsuleCollider2D playerCol;
    float lastThrowTime = -999f;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerCol = GetComponent<CapsuleCollider2D>();

        if (seSource == null)
        {
            seSource = GetComponent<AudioSource>();
        }
    }

    public void OnBombButtonDown()
    {
        if (bombPrefab == null) return;
        if (Time.time - lastThrowTime < bombCooldown) return;

        float dir = (transform.localScale.x >= 0f) ? 1f : -1f;

        Vector3 spawnPos;
        if (throwPoint != null)
        {
            spawnPos = throwPoint.position;
        }
        else
        {
            spawnPos = transform.position + new Vector3(spawnOffsetX * dir, spawnOffsetY, 0f);
        }

        GameObject bombObj = Instantiate(bombPrefab, spawnPos, Quaternion.identity);

        PlayerBombProjectile bomb = bombObj.GetComponent<PlayerBombProjectile>();
        if (bomb != null)
        {
            bomb.Initialize(
                owner: playerController,
                ownerCollider: playerCol,
                dir: dir,
                speedX: throwSpeedX,
                speedY: throwSpeedY,
                damage: bombDamage,
                radius: explosionRadius,
                fuse: fuseTime,
                effectPrefab: explosionEffectPrefab,
                effectLife: explosionEffectLife
            );
        }
        else
        {
            Rigidbody2D rb = bombObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = new Vector2(dir * throwSpeedX, throwSpeedY);
            }
        }

        if (throwSe != null && seSource != null)
        {
            seSource.PlayOneShot(throwSe);
        }

        lastThrowTime = Time.time;
    }
}