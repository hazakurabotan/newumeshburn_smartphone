using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectManager : MonoBehaviour
{
    [Header("次に行くシーン（キャラ選択）")]
    public string characterSelectSceneName = "CharacterSelect";

    // ノード方式（StageMapCursorから呼ぶ）
    public void ChooseNode(StageNode node)
    {
        if (node == null) return;

        if (string.IsNullOrEmpty(node.stageSceneName))
        {
            Debug.LogWarning("[StageSelect] stageSceneName が空です。NodeにScene名を入れてください。");
            return;
        }

        // キャラ選択を挟まない（操作説明など）
        if (!node.useCharacterSelect)
        {
            SceneManager.LoadScene(node.stageSceneName);
            return;
        }

        // キャラ選択を挟む（本編ステージ・街など）
        SelectedStage.SceneName = node.stageSceneName;
        SceneManager.LoadScene(characterSelectSceneName);
    }

    // 互換用：今までの呼び出しが残ってても動くように
    public void ChooseStage(string stageSceneName)
    {
        SelectedStage.SceneName = stageSceneName;
        SceneManager.LoadScene(characterSelectSceneName);
    }
}