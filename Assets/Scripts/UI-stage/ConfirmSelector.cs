using UnityEngine;
using UnityEngine.UI;

// 「はい／いいえ」など2択選択肢のカーソルUIを制御するクラス
public class ConfirmSelector : MonoBehaviour
{
    public GameObject yesHighlight;   // 「はい」選択中に強調表示するUI（例：枠や色を変えるパネル等）
    public GameObject noHighlight;    // 「いいえ」選択中に強調表示するUI

    int selectIndex = 0; // 選択中のインデックス（0なら「はい」、1なら「いいえ」）
    public ShopManager shopManager; // ショップ管理スクリプト（Inspectorで割り当て）

    // UIがアクティブになった瞬間に呼ばれる（選択状態を初期化）
    void OnEnable()
    {
        selectIndex = 0; // 初期状態は「はい」
        UpdateHighlight(); // 強調表示も更新
    }

    // 毎フレーム呼ばれる
    void Update()
    {
        // 自分のオブジェクトが非表示なら何もしない
        if (!gameObject.activeSelf) return;

        // 左右キーが押されたら選択肢を切り替え
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectIndex = 1 - selectIndex; // 0⇔1をトグル切り替え
            UpdateHighlight(); // 強調表示も更新
        }

        // Zキーが押されたら決定
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (selectIndex == 0)
                shopManager.OnConfirmYes(); // 「はい」選択時
            else
                shopManager.OnConfirmNo();  // 「いいえ」選択時
        }
    }

    // 選択中のボタンだけ強調表示する関数
    void UpdateHighlight()
    {
        yesHighlight.SetActive(selectIndex == 0); // 「はい」選択中なら強調
        noHighlight.SetActive(selectIndex == 1);  // 「いいえ」選択中なら強調
    }
}
