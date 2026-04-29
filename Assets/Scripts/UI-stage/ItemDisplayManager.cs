using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------
// ItemDisplayManager
// アイテムリスト（UI）の表示・選択・削除を担当するスクリプト
// 「装備処理」は外部（EquipUIManager）に委譲
// ----------------------------------------------
public class ItemDisplayManager : MonoBehaviour
{
    public RectTransform parent;         // アイテム画像を並べる親パネル
    public GameObject itemImagePrefab;   // 1アイテムにつきImage付きのUIプレハブ

    int selectIndex = 0;                 // 現在選択中のインデックス
    GameObject[] displayedItems;         // 表示されているアイテムUIの配列

    void Start()
    {
        Debug.Log("ItemDisplayManager.Start() 呼び出された");

        int count = PlayerInventory.obtainedItems.Count;
        displayedItems = new GameObject[count];

        // --- 所持アイテム数ぶんUIを生成して並べる ---
        for (int i = 0; i < count; i++)
        {
            int itemId = PlayerInventory.obtainedItems[i];
            Debug.Log($"itemId[{i}] = {itemId}");
            GameObject go = Instantiate(itemImagePrefab, parent);

            // GameManagerから対応するアイテム画像（Sprite）を取得
            Sprite sprite = (itemId >= 0 && itemId < GameManager.Instance.itemSprites.Length)
                ? GameManager.Instance.itemSprites[itemId]
                : null;

            if (sprite != null)
            {
                go.GetComponent<Image>().sprite = sprite;
                Debug.Log($"itemId[{itemId}] 用 sprite = {sprite.name}");
            }
            else
            {
                Debug.LogWarning($"sprite が null！ itemId = {itemId}");
            }

            // 横一列に配置
            go.GetComponent<RectTransform>().anchoredPosition = new Vector2(60 + 100 * i, 0);
            displayedItems[i] = go;
        }
        UpdateHighlight();
    }

    void Update()
    {
        // 非表示時やアイテム無しなら何もしない
        if (!gameObject.activeSelf) return;
        if (displayedItems == null || displayedItems.Length == 0) return;

        // 装備確認中は操作不可
        EquipUIManager ui = FindObjectOfType<EquipUIManager>();
        if (ui != null && ui.IsConfirming()) return;

        // ←→キーで選択インデックス切替
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectIndex = (selectIndex + 1) % displayedItems.Length;
            UpdateHighlight();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectIndex = (selectIndex - 1 + displayedItems.Length) % displayedItems.Length;
            UpdateHighlight();
        }

        // Zキーでアイテム使用や装備
        if (Input.GetKeyDown(KeyCode.Z))
        {
            int itemId = PlayerInventory.obtainedItems[selectIndex];
            if (itemId == 1) // 例：回復アイテム
            {
                UseHealItem();
            }
            else if (itemId == 2) // 例：装備アイテム
            {
                if (ui != null)
                {
                    // 装備確認パネルにSprite等を渡して装備処理を依頼
                    Sprite sprite = (itemId >= 0 && itemId < GameManager.Instance.itemSprites.Length)
                        ? GameManager.Instance.itemSprites[itemId]
                        : null;

                    ui.TryEquipItem(itemId, sprite, selectIndex, this);
                }
            }
        }
    }

    // --- 回復アイテム使用処理 ---
    void UseHealItem()
    {
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.Heal(10); // 体力10回復
            Debug.Log("体力回復アイテムを使った！");
        }
        RemoveItemAt(selectIndex); // 使ったら消す
    }

    // --- 指定インデックスのアイテムをUIとリストから削除 ---
    public void RemoveItemAt(int index)
    {
        Debug.Log($"RemoveItemAt呼ばれた: index={index}");

        PlayerInventory.obtainedItems.RemoveAt(index); // データ上から削除
        Destroy(displayedItems[index]);                // 表示UIも削除

        // 配列からも該当要素を除外
        var newList = new List<GameObject>(displayedItems);
        newList.RemoveAt(index);
        displayedItems = newList.ToArray();

        // インデックス調整
        if (selectIndex >= displayedItems.Length)
            selectIndex = Mathf.Max(0, displayedItems.Length - 1);

        RearrangeItems();   // 並び直し
        UpdateHighlight();  // ハイライトも更新
    }

    // --- アイテムを横一列に並び直す ---
    // 配列内の全アイテムを、指定位置に横並びで配置する関数
    void RearrangeItems()
    {
        for (int i = 0; i < displayedItems.Length; i++)
        {
            // 先頭はx=60、その後100pxずつ横にずらして配置
            displayedItems[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(60 + 100 * i, 0);
        }
    }

    // --- 選択中アイテムを拡大してハイライト ---
    // 現在選択しているアイテムだけを少し大きく表示する（ハイライト演出）
    void UpdateHighlight()
    {
        for (int i = 0; i < displayedItems.Length; i++)
        {
            // 選択中は1.2倍拡大、それ以外は等倍に戻す
            displayedItems[i].transform.localScale = (i == selectIndex) ? Vector3.one * 1.2f : Vector3.one;
        }
    }


    // --- 所持リスト内容が変わったときに再描画する関数 ---
    // アイテム追加・削除・装備変更時などに「UIを最新状態に作り直す」ための関数
    public void RefreshDisplay()
    {
        // --- まず、今表示中のアイテムUIをすべて消す ---
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject); // 子オブジェクト全部削除
        }

        // --- 新しい所持アイテム分だけ配列を再確保 ---
        int count = PlayerInventory.obtainedItems.Count;
        displayedItems = new GameObject[count];

        // --- 各アイテムごとにUIを新しく生成 ---
        for (int i = 0; i < count; i++)
        {
            int itemId = PlayerInventory.obtainedItems[i];
            GameObject go = Instantiate(itemImagePrefab, parent); // プレハブを複製＆親にセット

            // アイテムIDに対応するスプライト画像を取得
            Sprite sprite = (itemId >= 0 && itemId < GameManager.Instance.itemSprites.Length)
                ? GameManager.Instance.itemSprites[itemId]
                : null;

            if (sprite != null)
            {
                go.GetComponent<Image>().sprite = sprite; // 画像差し替え
            }
            else
            {
                Debug.LogWarning($"sprite が見つからない itemId = {itemId}");
            }

            // 横一列に並べる
            go.GetComponent<RectTransform>().anchoredPosition = new Vector2(60 + 100 * i, 0);
            displayedItems[i] = go;
        }

        selectIndex = 0; // 再描画時は先頭アイテムを選択
        UpdateHighlight(); // ハイライトも更新
    }

}
