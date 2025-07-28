using UnityEngine;

public class PunchHitbox : MonoBehaviour
{
    public int punchDamage = 2; // ƒpƒ“ƒ`‚ÌˆÐ—Í

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(punchDamage);
        }
        if (other.CompareTag("EnemyBullet"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = -rb.velocity;
                other.tag = "PlayerBullet";
            }
        }
    }
}
