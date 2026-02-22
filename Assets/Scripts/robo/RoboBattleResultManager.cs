using UnityEngine;
using UnityEngine.SceneManagement;

public class RoboBattleResultManager : MonoBehaviour
{
    [Header("Refs")]
    public BossHP bossHP;       // BossMecha に付いている BossHP
    public PlayerHP playerHP;   // PlayerCore に付いている PlayerHP

    [Header("Result Scene")]
    public string resultSceneName = "Result";

    bool isEnding = false;

    void Update()
    {
        if (isEnding) return;

        // ボス撃破 → リザルト
        if (bossHP != null && bossHP.IsDead)
        {
            LoadResult();
            return;
        }

        // プレイヤー死亡 → リザルト
        if (playerHP != null && playerHP.IsDead)
        {
            LoadResult();
            return;
        }
    }

    void LoadResult()
    {
        if (isEnding) return;
        isEnding = true;

        if (string.IsNullOrEmpty(resultSceneName))
        {
            Debug.LogWarning("[RoboBattleResultManager] resultSceneName が空です。");
            return;
        }

        Debug.Log("[RoboBattleResultManager] Load Result Scene: " + resultSceneName);
        SceneManager.LoadScene(resultSceneName);
    }
}
