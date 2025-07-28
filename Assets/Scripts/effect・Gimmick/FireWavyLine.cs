using UnityEngine;

// -----------------------------------------------------------
// FireWavyLine
// LineRendererを使って「炎」や「ビーム」のような
// うねうね揺れる線を描画するスクリプト
// -----------------------------------------------------------
public class FireWavyLine : MonoBehaviour
{
    // 炎の見た目になる線を描くLineRenderer
    public LineRenderer lineRenderer;

    // 線を構成する頂点の数（多いほど細かく滑らかになる）
    public int pointCount = 20;

    // 線の長さ（単位はワールド座標）
    public float length = 2f;

    // 揺れ（うねり）の最大高さ
    public float waveHeight = 0.2f;

    // 揺れ（うねり）の速さ
    public float waveSpeed = 2f;

    // 初期化
    void Start()
    {
        // LineRendererが未設定なら自分から取得
        if (!lineRenderer) lineRenderer = GetComponent<LineRenderer>();
        // 頂点数（Position Count）を設定
        lineRenderer.positionCount = pointCount;
    }

    // 毎フレーム呼ばれる
    void Update()
    {
        float time = Time.time * waveSpeed;  // 時間による揺れ
        Vector3 basePos = transform.position; // オブジェクトの現在位置を基準に

        for (int i = 0; i < pointCount; i++)
        {
            // 0～lengthまで均等に分割したx座標
            float x = (float)i / (pointCount - 1) * length;

            // y座標：サイン波で「うねうね」＋PerlinNoiseで微妙なゆらぎを追加
            float y = Mathf.Sin(x * 10f + time + i) * waveHeight * Mathf.PerlinNoise(time, x);

            // 頂点iの位置をセット（基準座標＋x, y）
            lineRenderer.SetPosition(i, basePos + new Vector3(x, y, 0));
        }
    }
}
