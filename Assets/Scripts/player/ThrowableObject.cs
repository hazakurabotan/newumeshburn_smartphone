using UnityEngine;

// プレイヤーが投げられる物体（箱・岩など）のスクリプト
public class ThrowableObject : MonoBehaviour
{
    public int damage = 1;           // 投げたときに与えるダメージ
    private bool isThrown = false;   // 投げられ中かどうか（弾状態フラグ）

    // ==== 投げられた瞬間に呼ばれるメソッド ====
    public void ActivateAsProjectile()
    {
        isThrown = true;             // 「弾」としてアクティブに
        Invoke("Deactivate", 1.0f);  // 1秒後に自動で弾フラグOFF
    }

    // ==== 弾状態を終了する処理 ====
    void Deactivate()
    {
        isThrown = false;
    }

    // ==== 何かにぶつかった時に呼ばれる（物理衝突判定） ====
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 投げられていない状態なら何もしない
        if (!isThrown) return;

        // 敵（Enemyタグ）に当たった場合だけダメージを与える
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Enemy enemy = collision.gameObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage); // 敵にダメージ！
            }
            isThrown = false; // 1回当たったらもうダメージを与えない
        }
    }
}
