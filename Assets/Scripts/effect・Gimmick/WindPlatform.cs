using UnityEngine;

// ----------------------------------------------
// WindPlatform
// プレイヤーが乗ると「ふわっ」と上昇させる風の足場ギミック
// ----------------------------------------------
public class WindPlatform : MonoBehaviour
{
    public float liftForce = 10f;     // プレイヤーに与える上向き速度（ジャンプ力）
    public float liftDuration = 0.3f; // 持続時間（未使用パラメータ、拡張用）

    // --- プレイヤーが接触し続けている間（毎フレーム）呼ばれる ---
    private void OnCollisionStay2D(Collision2D collision)
    {
        // プレイヤーと接触した時だけ処理
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                // 下向き or 停止中のみ、上向き速度を与える
                if (playerRb.velocity.y <= 0.1f)
                {
                    playerRb.velocity = new Vector2(playerRb.velocity.x, liftForce);
                }
            }
        }
    }
}
