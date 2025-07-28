using UnityEngine;

// ----------------------------------------------
// Spike
// トゲ床などにアタッチして「触れたらダメージ」処理を実装
// ----------------------------------------------
public class Spike : MonoBehaviour
{
    public int damage = 1; // 受けるダメージ量（インスペクターで変更可能）

    // --- 何かがトリガーに入ったとき自動で呼ばれる ---
    void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤーだった場合のみ処理
        if (other.CompareTag("Player"))
        {
            // PlayerControllerスクリプトを取得
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // 指定ダメージぶん減らす（TakeDamageはPlayer側の関数）
                player.TakeDamage(damage);
            }
        }
    }
}
