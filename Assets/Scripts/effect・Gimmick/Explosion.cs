using UnityEngine;

// ---------------------------------------------------------
// ExplosionScript
// 爆発エフェクトがプレイヤーに当たったらダメージを与え、
// 1秒後に自動で非表示にするスクリプト
// ---------------------------------------------------------
public class ExplosionScript : MonoBehaviour
{
    // プレイヤーに与えるダメージ量（Inspectorで調整可能）
    public int damage = 1;

    // 爆発の当たり判定に「何か」が入った時に呼ばれる
    void OnTriggerEnter2D(Collider2D other)
    {
        // 相手のタグが「Player」かどうか判定
        if (other.CompareTag("Player"))
        {
            // ぶつかった相手にPlayerControllerが付いていれば取得
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // プレイヤーにダメージを与える
                player.TakeDamage(damage);
            }
        }
    }

    // 爆発オブジェクトが表示されたとき（activeになった時）に呼ばれる
    void OnEnable()
    {
        // 1秒後にHideSelf()を呼び出す（自動で消えるタイマー）
        Invoke(nameof(HideSelf), 1f);
    }

    // 爆発オブジェクト自体を非表示にする関数
    void HideSelf()
    {
        gameObject.SetActive(false);
    }
}
