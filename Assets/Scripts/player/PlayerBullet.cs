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
        // すでにヒット済みなら何もしない（多重ヒット防止）
        if (hasHit) return;

        Debug.Log($"弾がヒット！{other.gameObject.name}（タグ：{other.tag}） at {Time.time}");

        // ====== 敵に当たったとき ======
        if (other.CompareTag("Enemy"))
        {
            hasHit = true;  // これ以上当たらないようにフラグを立てる
            Debug.Log("弾が敵に当たった！" + Time.time);
            Debug.Log($"攻撃力{damage}で発射！");

            Enemy enemy = other.GetComponent<Enemy>(); // Enemyスクリプト取得
            if (enemy != null)
            {
                enemy.TakeDamage(2, "gun"); // 敵にダメージを与える
            }

            Destroy(this.gameObject); // 弾を消す
        }
        // ====== ボスに当たったとき ======
        else if (other.CompareTag("Boss"))
        {
            hasHit = true;
            Debug.Log("弾がボスに当たった！" + Time.time);
            Debug.Log($"攻撃力{damage}で発射！");

            // ボス本体のスクリプト（BossSimpleJump）を取得
            BossSimpleJump boss = other.GetComponent<BossSimpleJump>();
            if (boss == null)
            {
                // 親オブジェクト側に付いている場合も考慮
                boss = other.GetComponentInParent<BossSimpleJump>();
            }

            if (boss != null)
            {
                Debug.Log("TakeDamage呼び出し！");
                boss.TakeDamage(damage); // ボスにダメージを与える
            }
            else
            {
                Debug.Log("BossSimpleJumpが見つからない");
            }

            Debug.Log("Destroy直前！");
            Destroy(this.gameObject); // 弾を消す
        }
    }
}
