using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public int damage = 1;
    public GameObject owner;   // î≠éÀé“ÅiBossÅj
    public bool destroyOnHit = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner) return;
        if (owner && other.transform.root == owner.transform.root) return;

        // Player
        var player = other.GetComponent<PlayerController>();
        if (player) { player.TakeDamage(damage); if (destroyOnHit) Destroy(gameObject); return; }

        // Mawaru
        var mawaru = other.GetComponent<MawaruController>();
        if (mawaru) { mawaru.TakeDamage(damage); if (destroyOnHit) Destroy(gameObject); return; }

        // Åö Boss
        var boss = other.GetComponent<BossController2D>();
        if (boss) { boss.ApplyDamage(damage); if (destroyOnHit) Destroy(gameObject); return; }
    }
}
