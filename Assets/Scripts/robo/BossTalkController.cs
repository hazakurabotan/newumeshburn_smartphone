using System.Collections;
using UnityEngine;
using TMPro;

public class BossTalkController : MonoBehaviour
{
    [Header("参照")]
    public BossHP bossHP;                    // BossHP をドラッグ
    public TextMeshProUGUI messageText;      // 赤丸の枠にある TextMeshPro をドラッグ

    [Header("表示時間")]
    public float messageDuration = 1.5f;     // 何秒表示するか

    int lastHp;
    Coroutine hideCoroutine;

    void Start()
    {
        if (bossHP == null)
        {
            bossHP = FindObjectOfType<BossHP>();
        }

        if (messageText != null)
        {
            messageText.text = "";
        }

        if (bossHP != null)
        {
            // ★ BossHP は currentHP / maxHP というフィールドを持っている前提
            lastHp = bossHP.currentHP;

            // BossHP のイベントに登録（BossHPUI と同じ書き方にしてね）
            bossHP.OnHpChanged += OnBossHpChanged;
        }

        // 開幕セリフ（戦闘は止めない）
        ShowMessage("全力でかかってこいよ！");
    }

    void OnDestroy()
    {
        if (bossHP != null)
        {
            bossHP.OnHpChanged -= OnBossHpChanged;
        }
    }

    // BossHP から呼ばれるコールバック
    // BossHPUI と同じシグネチャに合わせてね（int current, int max の想定）
    void OnBossHpChanged(int currentHp, int maxHp)
    {
        // HP が減ったときは「いってえ！」
        if (currentHp < lastHp)
        {
            ShowMessage("いってえ！");
        }

        // 残りHPに応じたセリフ（しきい値を1回だけ通るように lastHp と比較）
        if (currentHp <= 0 && lastHp > 0)
        {
            ShowMessage("俺の負けだよ。くそっ！");
        }
        else if (currentHp <= 10 && lastHp > 10)
        {
            ShowMessage("うるせぇ！まだだ！まだやれる");
        }
        else if (currentHp <= 20 && lastHp > 20)
        {
            ShowMessage("はぁ？ここからだし！");
        }
        else if (currentHp <= 30 && lastHp > 30)
        {
            ShowMessage("なかなかやるじゃん");
        }
        else if (currentHp <= 40 && lastHp > 40)
        {
            ShowMessage("まだまだ！");
        }

        lastHp = currentHp;
    }

    void ShowMessage(string text)
    {
        if (messageText == null) return;

        messageText.text = text;

        // 一度表示するたびに、古いコルーチンは止めてタイマーをリセット
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);

        if (messageText != null)
        {
            messageText.text = "";
        }
    }
}
