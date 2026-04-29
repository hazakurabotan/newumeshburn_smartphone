// どのアイテムを何個もってるか保存＆UIへ反映するクラス
using UnityEngine;
using UnityEngine.UI;

public class TradeItemInventory : MonoBehaviour
{
    // 3種類のアイテム個数を配列で管理
    public int[] itemCounts = new int[3];

    // パネル上の個数表示UI（Unityのインスペクターで3つ入れる）
    public Text[] itemCountTexts;

    // アイテムを増やす関数（itemTypeは0～2）
    public void AddItem(int itemType)
    {
        itemCounts[itemType]++;
        UpdateUI();
    }

    public void UpdateUI()
    {
        for (int i = 0; i < itemCountTexts.Length; i++)
        {
            itemCountTexts[i].text = itemCounts[i].ToString();
        }
    }
}
