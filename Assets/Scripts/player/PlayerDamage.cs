using UnityEngine;

public class PlayerDamage : MonoBehaviour
{
    private PlayerController player;

    void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (player != null)
                player.TakeDamage(1, other.bounds.center.x);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.CompareTag("Enemy"))
        {
            if (player != null)
                player.TakeDamage(1, col.collider.bounds.center.x);
        }
    }
}