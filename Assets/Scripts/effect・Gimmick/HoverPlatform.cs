using UnityEngine;

// ---------------------------------------------------------
// HoverPlatform
// このスクリプトを付けたオブジェクトは、
// プレイヤーが上に乗ったときに「ふわっ」と浮力を与える
// （例：ふわふわ足場、ホバープラットフォーム）
// ---------------------------------------------------------
public class HoverPlatform : MonoBehaviour
{
    public float hoverHeight = 1.2f;       // 浮かせたい高さ（足場表面からの距離）
    public float hoverStrength = 20f;      // 浮力の強さ（大きいほどビタ止まり）

    // プレイヤーがこの足場に乗ってる間、毎フレーム呼ばれる
    void OnCollisionStay2D(Collision2D collision)
    {
        // もし乗ってるのがPlayerタグ付きオブジェクトなら
        if (collision.gameObject.CompareTag("Player"))
        {
            // プレイヤーのRigidbody2D（物理制御）を取得
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // 足場表面のY座標を求める（自分の位置＋コライダー半分の高さ）
                float surfaceY = transform.position.y + GetComponent<Collider2D>().bounds.extents.y;

                // 目標の浮かせたい高さとの差分を計算
                float diff = (surfaceY + hoverHeight) - collision.transform.position.y;

                // 浮力を計算
                // 目標高さまで押し上げる力 － 現在の落下速度に反発する（減衰的）
                float force = diff * hoverStrength - rb.velocity.y * 8f;

                // 上方向に力を加える（バネのようなイメージ）
                rb.AddForce(Vector2.up * force);

                // ★ふわふわ感やカチッと止めたい場合はhoverStrengthや速度への減衰値を調整！
            }
        }
    }
}
