using UnityEngine;

// プレイヤーが撃つ弾（PlayerBullet）の処理スクリプト
public class PlayerBullet : MonoBehaviour
{
    public int damage = 1;  // 弾のダメージ量（強化弾なら3などに変更もOK）
    private bool hasHit = false; // すでに何かに当たったかどうか（多重ヒット防止）

    public void SetDamage(int value)
    {
        damage = value;
    }

    // ====== 他のCollider2Dと当たったときに自動で呼ばれる ======
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // ★自分(Player)には反応しない
        if (other.CompareTag("Player")) return;

        if (other.CompareTag("Enemy"))
        {
            hasHit = true;
            var enemy = other.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(2, "gun");
            Destroy(gameObject);
            return;
        }
        else if (other.CompareTag("Boss"))
        {
            hasHit = true;
            var boss = other.GetComponent<BossSimpleJump>() ?? other.GetComponentInParent<BossSimpleJump>();
            if (boss != null) boss.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 地形系(非Trigger)に当たったら即消す
        if (!other.isTrigger) Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        // ★誤って非TriggerでもPlayerは無視
        if (col.collider.CompareTag("Player")) return;
        Destroy(gameObject);
    }

}
