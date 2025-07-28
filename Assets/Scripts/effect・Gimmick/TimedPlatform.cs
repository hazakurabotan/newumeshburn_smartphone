using UnityEngine;

// ----------------------------------------------
// TimedPlatform
// 一定時間ごとに消えたり現れたりする床ギミック
// ----------------------------------------------
public class TimedPlatform : MonoBehaviour
{
    public float visibleTime = 2f;   // 床が表示される秒数
    public float invisibleTime = 1f; // 床が消えている秒数

    private float timer = 0f;        // タイマー
    private bool isVisible = true;   // 現在の表示状態
    private SpriteRenderer sr;       // 床の見た目を管理
    private Collider2D col;          // 当たり判定

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();  // 見た目の制御用
        col = GetComponent<Collider2D>();    // 乗れる/乗れないの制御用
        SetVisible(true); // 最初は表示・有効状態からスタート
    }

    void Update()
    {
        timer += Time.deltaTime; // 毎フレーム経過秒数を加算

        // --- 表示中でvisibleTimeを超えたら消す ---
        if (isVisible && timer >= visibleTime)
        {
            SetVisible(false); // 消える
            timer = 0f;        // タイマーリセット
        }
        // --- 消えていてinvisibleTimeを超えたら再び表示 ---
        else if (!isVisible && timer >= invisibleTime)
        {
            SetVisible(true); // 再び現れる
            timer = 0f;
        }
    }

    // --- 表示・非表示を切り替える関数 ---
    void SetVisible(bool show)
    {
        isVisible = show;      // 状態フラグ更新
        sr.enabled = show;     // スプライト表示ON/OFF
        if (col != null) col.enabled = show; // コライダーもON/OFF
    }
}
