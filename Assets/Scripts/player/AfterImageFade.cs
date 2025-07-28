using UnityEngine;

// -----------------------------------------------
// AfterImageFade
// キャラクターの動きに合わせて発生する「残像」を、
// 時間が経つごとに少しずつ消していくスクリプトです。
// -----------------------------------------------
public class AfterImageFade : MonoBehaviour
{
    // この残像が消えるまでの合計時間（秒）
    public float fadeTime = 1.0f;

    // SpriteRenderer：スプライト画像を表示するためのコンポーネント
    private SpriteRenderer sr;

    // 残り時間を記録するタイマー
    private float timer;

    // スタート時に一度だけ実行される関数
    void Start()
    {
        // このオブジェクトについているSpriteRendererコンポーネントを取得
        sr = GetComponent<SpriteRenderer>();
        // タイマーを最大値（fadeTime）で初期化
        timer = fadeTime;
    }

    // 毎フレーム呼ばれる関数（1秒間に複数回実行される）
    void Update()
    {
        // 1フレームごとに、経過した分だけタイマーを減らす
        timer -= Time.deltaTime;

        // 残り時間に応じて透明度（アルファ値）を計算
        // timerがfadeTimeのとき→1（最初は完全な色）
        // timerが0のとき→0（完全に透明）
        float alpha = Mathf.Clamp01(timer / fadeTime);

        // SpriteRendererが正しく取得できている場合のみ
        if (sr != null)
        {
            // 現在の色（R,G,B,Aの情報）を取得
            var c = sr.color;
            // 透明度（A）を「計算したalpha × 0.5」に変更
            // → 0.5倍することで、最初からちょっと半透明な残像にする
            c.a = alpha * 0.5f;
            // 変更した色をSpriteRendererに反映
            sr.color = c;
        }

        // タイマーが0以下（残像の寿命が尽きた）になったら…
        if (timer <= 0)
            // このゲームオブジェクト自体を消去（残像が画面から消える）
            Destroy(gameObject);
    }
}
