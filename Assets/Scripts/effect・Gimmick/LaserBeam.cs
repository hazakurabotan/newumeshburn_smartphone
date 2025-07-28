using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // タグ判定で敵なら
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(3); // ダメージ3を与える
            }
            // Destroy(this.gameObject); ← これをしなければ貫通
        }
    }
}
