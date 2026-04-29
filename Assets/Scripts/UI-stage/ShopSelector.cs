using UnityEngine;
using UnityEngine.UI;

// ショップの商品選択UIを管理するスクリプト
public class ShopSelector : MonoBehaviour
{
    public Image[] items;        // 商品画像の配列（UI ImageをInspectorで登録）
    public Sprite selectFrame;   // ハイライト画像（※未使用なら削除OK）

    int selectIndex = 0;         // 現在選択中のインデックス
    public bool selecting = false; // 選択モード中かどうか（外部からもON/OFFできる）

    // ===== 選択モード開始（外部から呼ばれる） =====
    public void StartSelect()
    {
        selecting = true;
        UpdateHighlight();
    }

    void Update()
    {
        // 選択モードじゃなければ何もしない
        if (!selecting) return;

        // 確認パネル（ConfirmPanel）が表示されている間は操作しない
        if (GameObject.Find("ConfirmPanel") != null && GameObject.Find("ConfirmPanel").activeSelf)
            return;

        // 右キーで次の商品へ
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectIndex = (selectIndex + 1) % items.Length;
            UpdateHighlight();
        }
        // 左キーで前の商品へ
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectIndex = (selectIndex + items.Length - 1) % items.Length;
            UpdateHighlight();
        }
        // Zキーで決定
        if (Input.GetKeyDown(KeyCode.Z))
        {
            // ShopManagerのShowConfirmを呼び出して確認パネル表示
            FindObjectOfType<ShopManager>().ShowConfirm(selectIndex);
            selecting = false; // 選択モード終了
        }
    }

    // ===== 選択中の商品だけ大きく（強調表示） =====
    void UpdateHighlight()
    {
        for (int i = 0; i < items.Length; i++)
        {
            // 選択中だけ1.2倍、それ以外は通常サイズ
            items[i].transform.localScale = (i == selectIndex) ? Vector3.one * 1.2f : Vector3.one;
        }
    }
}
