using UnityEngine;
using UnityEngine.SceneManagement;

// ボスの簡易ジャンプ行動を制御するクラス
public class BossSimpleJump : MonoBehaviour
{
    // ======= 設定用パラメータ（Inspectorで調整） =======
    public float moveSpeed = 2f;    // 歩く速さ
    public float moveTime = 2f;     // 歩き続ける時間
    public float jumpPower = 7f;    // ジャンプの上方向の力
    public float jumpSpeed = 4f;    // ジャンプ中の横移動速度
    public float standTime = 1f;    // 着地後に止まる時間（秒）
    public bool isActive = false;   // ボスが行動開始するかどうか

    // ======= 内部で使う変数 =======
    private int moveDir = -1;         // 移動方向（-1＝左、1＝右）
    private float timer = 0f;         // 経過時間を測るタイマー
    private int state = 0;            // 行動の状態（0:歩き、1:ジャンプ、2:着地停止）
    private float jumpTargetX;        // ジャンプ後の目標X座標
    private bool isJumping = false;   // ジャンプ中かどうか
    private Rigidbody2D rb;           // Rigidbody2Dコンポーネント（物理演算用）
    private SpriteRenderer sr;        // SpriteRendererコンポーネント（見た目用）

    // HPバー（外部からアサイン）
    public BossHpBarController BossHpPanel;

    // ======= HP管理 =======
    public int maxHP = 15;            // ボスの最大HP
    int currentHP;                    // 現在のHP

    // ======= 初期化 =======
    void Start()
    {
        currentHP = maxHP;    // HP初期化
        rb = GetComponent<Rigidbody2D>();      // Rigidbody2D取得
        sr = GetComponent<SpriteRenderer>();   // SpriteRenderer取得
        timer = 0f;
        state = 0;

        // HPバーがあれば初期HPを反映
        if (BossHpPanel != null)
            BossHpPanel.SetHp(currentHP);
    }

    // ======= 毎フレーム呼ばれる処理 =======
    void Update()
    {
        // ボスがアクティブになってなければ何もしない
        if (!isActive) return;

        // ボスの状態ごとに行動を切り替える
        switch (state)
        {
            case 0: // 歩く（左右に移動）
                timer += Time.deltaTime;   // 経過時間加算
                rb.velocity = new Vector2(moveSpeed * moveDir, rb.velocity.y);  // 横移動

                // 歩き時間が経過したらジャンプ準備へ
                if (timer >= moveTime)
                {
                    timer = 0f;
                    state = 1;

                    // ジャンプの着地点X座標を計算
                    float dx = (sr != null ? sr.bounds.size.x * 5f : 5f);
                    jumpTargetX = transform.position.x + dx * moveDir;

                    // ジャンプ開始：上方向に力＋横方向の速度をセット
                    rb.velocity = new Vector2(jumpSpeed * moveDir, jumpPower);
                    isJumping = true;
                }
                break;

            case 1: // ジャンプ中
                if (isJumping)
                {
                    // 空中でも横速度を維持
                    rb.velocity = new Vector2(jumpSpeed * moveDir, rb.velocity.y);

                    // 目標位置を通り過ぎたらピタッと止める（ズレ防止）
                    if ((moveDir == -1 && transform.position.x <= jumpTargetX) ||
                        (moveDir == 1 && transform.position.x >= jumpTargetX))
                    {
                        Vector2 pos = transform.position;
                        pos.x = jumpTargetX;
                        transform.position = pos;
                        rb.velocity = new Vector2(0, rb.velocity.y);
                    }
                }

                // 地面についたらジャンプ終了
                if (isJumping && IsOnGround())
                {
                    isJumping = false;
                    rb.velocity = Vector2.zero;
                    timer = 0f;
                    state = 2;   // 着地停止状態へ
                }
                break;

            case 2: // 着地後しばらく停止
                rb.velocity = Vector2.zero;
                timer += Time.deltaTime;
                if (timer >= standTime)
                {
                    timer = 0f;
                    moveDir *= -1; // 移動方向を逆にする
                    state = 0;     // また歩き状態へ
                }
                break;
        }
    }

    // ======= 地面にいるか判定する関数 =======
    bool IsOnGround()
    {
        // y方向速度がほぼゼロなら地面についているとみなす
        return Mathf.Abs(rb.velocity.y) < 0.05f;
    }

    // ======= ダメージ処理 =======
    public void TakeDamage(int amount)
    {
        // HPを減らし、範囲外にならないようClampで調整
        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        // HPバーにも反映
        if (BossHpPanel != null)
            BossHpPanel.SetHp(currentHP);

        // デバッグ表示
        Debug.Log("現在のボスHP: " + currentHP);

        // HPゼロ以下なら死亡処理
        if (currentHP <= 0)
        {
            Die();
        }
    }

    // ======= 死亡処理 =======
    void Die()
    {
        // シーンを"Resuit"（リザルト画面など）に切り替え
        SceneManager.LoadScene("Resuit");
    }
}
