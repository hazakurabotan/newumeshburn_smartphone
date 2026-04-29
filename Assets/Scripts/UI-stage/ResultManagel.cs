using UnityEngine;
using TMPro;

public class ResultManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText;

    void Start()
    {
        // GameManager の static な totalScore を参照して表示
        scoreText.text = GameManager.totalScore.ToString();
        Debug.Log("リザルト画面に表示されるスコア: " + GameManager.totalScore);
    }
}
