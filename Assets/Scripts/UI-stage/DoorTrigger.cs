using UnityEngine;
using UnityEngine.SceneManagement;

// ドアやワープポイント用トリガースクリプト
// プレイヤーが範囲内で↑キーを押すと指定したシーンに移動します
public class DoorTrigger : MonoBehaviour
{
    public string sceneToLoad = "Shop"; // 行きたいシーン名をInspectorで指定

    private bool isPlayerInTrigger = false; // プレイヤーが範囲内にいるかのフラグ

    // 毎フレーム呼ばれる
    void Update()
    {
        // プレイヤーが範囲内 かつ ↑キーが押されたときに
        if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.UpArrow))
        {
            // シーン遷移を実行
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    // 2Dコライダーがこのトリガーに入った瞬間に呼ばれる
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 入ってきたオブジェクトが「Player」タグの場合のみ
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true; // フラグON
        }
    }

    // 2Dコライダーがこのトリガーから出た瞬間に呼ばれる
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false; // フラグOFF
        }
    }
}
