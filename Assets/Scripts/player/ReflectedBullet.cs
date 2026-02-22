using UnityEngine;

public class ReflectedBullet : MonoBehaviour
{
    public int damage = 1;

    private void OnTriggerEnter2D(Collider2D col)
    {
        int enemyL = LayerMask.NameToLayer("Enemy");
        int bossL = LayerMask.NameToLayer("Boss");
        int layer = col.gameObject.layer;

        if (layer == enemyL || layer == bossL)
        {
            // 敵/ボス側の代表的な受け口を呼ぶ（あれば実行される）
            col.gameObject.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            col.gameObject.SendMessage("Damage", damage, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
        }
    }
}
