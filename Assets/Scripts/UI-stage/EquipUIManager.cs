using UnityEngine;
using UnityEngine.UI;

// ------------------------------------------------------
// EquipUIManager
// 装備スロットのUIと、装備するかどうかの確認パネルを管理するスクリプト
// ------------------------------------------------------
public class EquipUIManager : MonoBehaviour
{
    // 装備スロット表示用のImage
    public Image equipSlotImage;
    // 「装備しますか？」ダイアログ用パネル
    public GameObject confirmPanel;
    // 「はい」「いいえ」ボタンのImage
    public Image yesButtonImage;
    public Image noButtonImage;

    // 空スロット表示用のSprite（未使用アイテム）
    public Sprite emptySlotSprite;

    // 現在どちらを選択しているか（0=Yes, 1=No）
    int select = 0;
    // 確認パネル表示中かどうか
    bool isConfirming = false;

    // 装備予定アイテムの情報（パネルで保留中のもの）
    int pendingItemId = -1;
    Sprite pendingSprite = null;
    int pendingItemIndex = -1;
    ItemDisplayManager pendingManager = null;

    // 今確認ダイアログが出ているかどうか外部から取得
    public bool IsConfirming() { return isConfirming; }

    // 初期化
    void Start()
    {
        // ゲーム開始時、現在装備中のスプライト画像を取得して表示
        Sprite sprite = GameManager.Instance.GetEquippedSprite();
        if (sprite != null)
        {
            equipSlotImage.sprite = sprite;
            equipSlotImage.enabled = true;
        }
        else
        {
            equipSlotImage.enabled = false;
        }

        // 確認パネルは最初は非表示
        confirmPanel.SetActive(false);
        UpdateButtonUI();
    }

    // 毎フレーム呼ばれる
    void Update()
    {
        // 確認ダイアログ表示中のみ入力を受付
        if (!isConfirming) return;

        // 左右キーで「はい」「いいえ」切り替え
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            select = 1 - select; // 0→1, 1→0を切り替え
            UpdateButtonUI();
        }
        // Zキーで決定（はい/いいえ）
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (select == 0)
                OnClickYes();
            else
                OnClickNo();
        }
        // Xキーでキャンセル（イイエ）
        if (Input.GetKeyDown(KeyCode.X))
        {
            OnClickNo();
        }
    }

    // アイテムを装備しようとした時に呼ばれる
    public void TryEquipItem(int itemId, Sprite itemSprite, int itemIndex, ItemDisplayManager manager)
    {
        // スプライトが設定されていなければ警告を出して終了
        if (itemSprite == null)
        {
            Debug.LogWarning("装備しようとしている sprite が null です！itemId = " + itemId);
            return;
        }

        Debug.Log($"TryEquipItem呼び出し: itemId={itemId}, sprite={itemSprite.name}, itemIndex={itemIndex}");

        // 「装備予定」データを保持
        pendingItemId = itemId;
        pendingSprite = itemSprite;
        pendingItemIndex = itemIndex;
        pendingManager = manager;

        // 確認パネルを表示
        confirmPanel.SetActive(true);
        isConfirming = true;
        select = 0;
        UpdateButtonUI();
    }

    // 「はい」ボタンを押した時の処理
    void OnClickYes()
    {
        // スロット画像を新しいアイテム画像に変更
        equipSlotImage.sprite = pendingSprite;
        equipSlotImage.enabled = true;

        // GameManagerに選択アイテムIDを通知
        GameManager.Instance.equippedItemId = pendingItemId;

        // 確認パネルを閉じる
        confirmPanel.SetActive(false);
        isConfirming = false;

        // 装備したアイテムを一覧から消す（managerがあれば）
        if (pendingManager != null && pendingItemIndex >= 0)
            pendingManager.RemoveItemAt(pendingItemIndex);
    }

    // 「いいえ」ボタンを押した時の処理
    void OnClickNo()
    {
        Debug.Log("イイエを押した。パネルを閉じて通常選択画面へ戻す");
        confirmPanel.SetActive(false);
        isConfirming = false;
    }

    // ボタンUIの色や拡大縮小など切り替え表示
    void UpdateButtonUI()
    {
        yesButtonImage.color = (select == 0) ? Color.yellow : Color.white;
        noButtonImage.color = (select == 1) ? Color.yellow : Color.white;
        yesButtonImage.transform.localScale = (select == 0) ? Vector3.one * 1.2f : Vector3.one;
        noButtonImage.transform.localScale = (select == 1) ? Vector3.one * 1.2f : Vector3.one;
    }
}
