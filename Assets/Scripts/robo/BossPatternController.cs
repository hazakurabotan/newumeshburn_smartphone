using UnityEngine;
using System.Collections;

public class BossPatternController : MonoBehaviour
{

    [Header("スラッシュ関連")]
    public Sprite normalFarSprite;     // 奥にいるときのスプライト
    public Sprite normalCloseSprite;   // 手前にいるときのスプライト
    public Sprite slashReadySprite;    // 溜めモーションのスプライト
    public Sprite slashAttackSprite;   // 斬撃モーションのスプライト

    [Header("のけぞりスプライト")]
    public Sprite staggerSprite;      // ★ 追加


    public int slashDamageToPlayer = 10; // プレイヤーに与える斬撃ダメージ
    public PlayerHP playerHP;            // PlayerHPスクリプトをここにドラッグして入れる


    [Header("奥側のポイント")]
    public Transform farLeft;
    public Transform farCenter;
    public Transform farRight;

    [Header("手前側のポイント")]
    public Transform closeLeft;
    public Transform closeRight;

    [Header("移動設定")]
    public float moveSpeed = 5f;
    public float waitTimeAtPoint = 0.5f;
    public float farMoveDuration = 3f; // 「左右に移動」している時間

    BossHP bossHP;
    SpriteRenderer sr;
    Collider2D col;

    bool slashCanBeInterrupted = false;
    bool slashInterrupted = false;


    public void OnPunchedByPlayer()
    {
        if (slashCanBeInterrupted)
        {
            slashInterrupted = true;
        }
    }

    void Awake()
    {
        bossHP = GetComponent<BossHP>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    IEnumerator Start()
    {
        // 開始位置は中央・奥
        transform.position = farCenter.position;

        if (normalFarSprite != null)
        {
            sr.sprite = normalFarSprite;
        }
        Debug.Log("BossPattern Start");

        while (true)
        {
            // HPがあるか確認して、死んでたら終了
            if (bossHP != null && bossHP.IsDead)
            {
                Debug.Log("BossPattern end : boss is dead");
                yield break;
            }

            bool leftPattern = Random.value < 0.5f; // true = 左パターン
            Debug.Log("Boss DoPattern  left = " + leftPattern);

            yield return StartCoroutine(DoPattern(leftPattern));
        }
    }

    IEnumerator DoPattern(bool isLeft)
    {
        // 1. 奥：ミサイル発射
        yield return StartCoroutine(DoMissileAttack());

        // 2. 奥：左右に移動
        yield return StartCoroutine(DoFarMove());

        // 3. 奥：中央に戻る
        yield return StartCoroutine(MoveTo(farCenter.position));

        // 4. 奥：端まで移動して消える
        Vector3 sideFarPos = isLeft ? farLeft.position : farRight.position;
        yield return StartCoroutine(MoveTo(sideFarPos));
        yield return StartCoroutine(Disappear());

        // 5. 手前：端から出現 → 斬撃
        Transform closePos = isLeft ? closeLeft : closeRight;
        transform.position = closePos.position;
        yield return StartCoroutine(Appear());
        yield return StartCoroutine(DoSlash(isLeft)); // ここでプレイヤーが左右パンチ＆ガードするフェーズ

        // 6. 手前：端に消える
        yield return StartCoroutine(Disappear());

        // 7. 奥：同じ側から出てくる
        transform.position = sideFarPos;
        yield return StartCoroutine(Appear());

        // ★ここで奥用スプライトに戻す
        if (normalFarSprite != null)
        {
            sr.sprite = normalFarSprite;
        }

        // 8. 奥：ミサイル発射
        yield return StartCoroutine(DoMissileAttack());

        // 9. 奥：左右に移動
        yield return StartCoroutine(DoFarMove());
    }

    // ========= 各アクション =========

    IEnumerator MoveTo(Vector3 targetPos)
    {
        while ((transform.position - targetPos).sqrMagnitude > 0.001f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }
        yield return new WaitForSeconds(waitTimeAtPoint);
    }

    IEnumerator DoFarMove()
    {
        float t = 0f;
        Vector3 a = farLeft.position;
        Vector3 b = farRight.position;

        while (t < farMoveDuration)
        {
            // 左右に往復する簡単な動き
            float pingpong = Mathf.PingPong(t * 0.5f, 1f);
            transform.position = Vector3.Lerp(a, b, pingpong);

            t += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator DoMissileAttack()
    {
        // ここで最大5発までミサイルを撃つ処理
        // 発射間隔などはお好みで
        int count = Random.Range(3, 6); // 3〜5発くらい
        for (int i = 0; i < count; i++)
        {
            FireMissile(); // 実際のミサイル生成処理
            yield return new WaitForSeconds(0.4f);
        }
        yield return new WaitForSeconds(0.5f);
    }

    void FireMissile()
    {
        // ミサイル生成＆方向を決める処理を書く
    }

    IEnumerator DoSlash(bool fromLeft)
    {
        // 近距離パンチが当たる判定をON（任意）
        EnableCloseHitbox(true);

        slashCanBeInterrupted = true;
        slashInterrupted = false;

        // 溜めスプライト
        if (slashReadySprite != null)
            sr.sprite = slashReadySprite;

        // パンチでキャンセルできる時間
        float preTime = 0.5f;
        while (preTime > 0f && !slashInterrupted)
        {
            preTime -= Time.deltaTime;
            yield return null;
        }

        // ここからキャンセル不可
        slashCanBeInterrupted = false;
        EnableCloseHitbox(false);

        if (slashInterrupted)
        {
            // のけぞりアニメだけして終了
            yield return StartCoroutine(PlayStaggerAnim());
            yield break;
        }

        // 斬撃（ヒット＋ダメージ）
        yield return StartCoroutine(PlaySlashAnimAndHit(fromLeft));
    }

    IEnumerator Disappear()
    {
        col.enabled = false;
        // フェードアウトしたいならアルファを徐々に下げる
        sr.enabled = false;
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator Appear()
    {
        sr.enabled = true;
        col.enabled = true;
        yield return new WaitForSeconds(0.3f);
    }

    void EnableCloseHitbox(bool on)
    {
        // 近距離パンチ用ColliderのON/OFFなど
    }

    IEnumerator PlayStaggerAnim()
    {
        Debug.Log("Boss stagger (punched during slash)");

        // まずのけぞり用スプライトに差し替え
        if (staggerSprite != null)
        {
            sr.sprite = staggerSprite;
        }

        // のけぞってる時間
        float t = 0.2f;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            yield return null;
        }

        // 終わったら手前の通常スプライトに戻す
        if (normalCloseSprite != null)
        {
            sr.sprite = normalCloseSprite;
        }
    }

    IEnumerator PlaySlashAnimAndHit(bool fromLeft)
    {
        // 斬撃時のスプライト
        if (slashAttackSprite != null)
            sr.sprite = slashAttackSprite;

        // 演出のため少し待つ
        yield return new WaitForSeconds(0.15f);

        // ★ プレイヤーにダメージ
        if (playerHP != null)
        {
            playerHP.DamageToPlayer(slashDamageToPlayer);
        }

        // 元の手前スプライトへ戻す
        yield return new WaitForSeconds(0.25f);

        if (normalCloseSprite != null)
            sr.sprite = normalCloseSprite;
    }

}
