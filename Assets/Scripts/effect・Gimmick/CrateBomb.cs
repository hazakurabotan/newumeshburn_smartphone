using UnityEngine;

// ---------------------------------------------------
// CrateBombSimple
// プレイヤーが触れると一定時間後に爆発する箱（クレート）のサンプルスクリプト
// ---------------------------------------------------
public class CrateBombSimple : MonoBehaviour
{
    // 爆発の見た目（スプライトなど）用のGameObject
    public GameObject explosionSpriteObj;

    // 爆発するまでの遅延時間（秒）
    public float delay = 3f;

    // カウントダウン開始済みかどうかを判定するフラグ
    private bool isCounting = false;

    // 残り時間のタイマー
    private float timer = 0f;

    // 毎フレーム呼ばれる関数
    void Update()
    {
        // カウントダウン中なら…
        if (isCounting)
        {
            // 1フレームごとにタイマーを減らす
            timer -= Time.deltaTime;

            // タイマーが0以下になったら…
            if (timer <= 0f)
            {
                // 爆発エフェクト用オブジェクトが割り当てられていれば
                if (explosionSpriteObj != null)
                {
                    // 爆発を表示（activeにする）
                    explosionSpriteObj.SetActive(true);
                    // 爆発を箱と同じ位置に出す
                    explosionSpriteObj.transform.position = transform.position;
                }

                // この箱（クレート）自体を消す
                Destroy(gameObject);
            }
        }
    }

    // 2DのColliderが「何か」に触れたとき呼ばれる関数
    void OnTriggerEnter2D(Collider2D other)
    {
        // まだカウントダウン開始していなくて、ぶつかった相手がPlayerなら…
        if (!isCounting && other.CompareTag("Player"))
        {
            // カウントダウン開始！
            isCounting = true;
            timer = delay; // 指定した秒数だけ待つ
        }
    }
}
