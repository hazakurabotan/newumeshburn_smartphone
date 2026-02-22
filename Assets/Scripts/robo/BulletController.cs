using UnityEngine;

public class BulletController : MonoBehaviour
{
    public int damage = 1;
    public float lifeTime = 1f;

    void Start()
    {
        // 当たらなくても 1 秒後に消える
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Bullet hit : " + other.name);

        // ボスへのダメージ
        if (other.CompareTag("Boss"))
        {
            var bossHp = other.GetComponent<BossHP>();
            if (bossHp != null)
            {
                bossHp.TakeDamage(damage);
            }
            Destroy(gameObject);
            return;
        }

        // ミサイルを撃ち落とす
        if (other.CompareTag("Missile"))
        {
            var missile = other.GetComponent<EnemyMissile>();
            if (missile != null)
            {
                missile.Explode();      // ★ここでさっきの Explode() を呼ぶ
            }

            Destroy(gameObject);        // 弾も消える
        }
    }
}
