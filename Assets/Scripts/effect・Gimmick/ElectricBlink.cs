using UnityEngine;

// ---------------------------------------------------
// ElectricWipe
// LineRendererを使って「シュッ」と線が伸びるエフェクトを作るスクリプト
// ---------------------------------------------------
public class ElectricWipe : MonoBehaviour
{
    // エフェクト用のLineRenderer
    public LineRenderer lineRenderer;

    // 線が伸びきるまでのアニメーション時間（秒）
    public float wipeDuration = 1f;

    // 経過時間を記録するタイマー
    private float timer = 0f;

    // ゲーム開始時に1度だけ実行される
    void Start()
    {
        // 最初は完全に透明な状態で開始
        SetAlpha(0f, 0f);
    }

    // 毎フレーム呼ばれる（線の伸びるアニメーションを制御）
    void Update()
    {
        // 経過時間を加算
        timer += Time.deltaTime;

        // tは0～1でループ（0→1→0→1…）
        float t = Mathf.Repeat(timer / wipeDuration, 1f);

        // head（先頭）からwidthの幅だけ不透明で見せる
        SetAlpha(t, 0.1f); // 0.1fは「線の太さ」みたいなイメージ。調整OK
    }

    // headからwidthの範囲だけ線を不透明にし、それ以外は透明にする
    void SetAlpha(float head, float width)
    {
        Gradient gradient = new Gradient();

        // グラデーションの色設定（色は白で固定）
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f), // 開始点
                new GradientColorKey(Color.white, 1.0f), // 終点
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0.0f),                            // 開始は透明
                new GradientAlphaKey(1f, Mathf.Clamp01(head)),             // head位置で不透明
                new GradientAlphaKey(1f, Mathf.Clamp01(head + width)),     // head+width位置も不透明
                new GradientAlphaKey(0f, 1.0f)                             // 終了点は透明
            }
        );
        // LineRendererにグラデーションを適用
        lineRenderer.colorGradient = gradient;
    }
}
