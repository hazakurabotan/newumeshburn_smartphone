using UnityEngine;
using UnityEngine.SceneManagement;

// シーンを切り替えるためのシンプルなスクリプト
// ボタンやイベントから呼び出して使います
public class ChangeScene : MonoBehaviour
{
    // 移動したいシーンの名前をInspectorで設定
    // 例: "Stage1"や"GameOver"など
    public string sceneName = "Title"; // ← わかりやすくシーン名と命名！

    // この関数を呼ぶと指定したシーンに切り替わる
    public void Load()
    {
        // SceneManager.LoadSceneでシーンをロード
        SceneManager.LoadScene(sceneName);
    }
}
