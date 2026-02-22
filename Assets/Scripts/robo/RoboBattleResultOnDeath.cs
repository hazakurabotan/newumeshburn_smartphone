using UnityEngine;
using UnityEngine.SceneManagement;

public class RoboBattleResultOnDeath : MonoBehaviour
{
    public BossHP bossHP;
    public PlayerHP playerHP;        // ← クラス名は実際のものに合わせて
    public string resultSceneName = "Result"; // リザルトシーン名

    bool isEnding = false;

    void Update()
    {
        if (isEnding) return;

        // BossHP は IsDead プロパティがあるのを、BossPatternController から確認済み
        if (bossHP != null && bossHP.IsDead)
        {
            EndBattle();
            return;
        }

        // ★ここはお姉ちゃんの PlayerHP 実装に合わせて書き換えてね
        // 例）PlayerHP に IsDead があるならこう：
        if (playerHP != null && playerHP.IsDead)
        {
            EndBattle();
            return;
        }

        // IsDead が無くて currentHP だけなら↓みたいにするイメージ
        // if (playerHP != null && playerHP.currentHp <= 0) { EndBattle(); }
    }

    void EndBattle()
    {
        if (isEnding) return;
        isEnding = true;
        SceneManager.LoadScene(resultSceneName);
    }
}
