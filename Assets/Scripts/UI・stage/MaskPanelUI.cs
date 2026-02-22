// MaskPanelUI.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaskPanelUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameObject panelRoot;            // パネル本体（SetActive切替）
    [SerializeField] RectTransform gridRoot;         // GridLayoutGroup の Content
    [SerializeField] MaskItemButton itemPrefab;      // 下のPrefab用スクリプト
    [SerializeField] MaskManager maskManager;

    GameObject firstSpawned;

    public void Open()
    {
        Build();
        panelRoot.SetActive(true);
        // 初期選択（カーソルも動く）
        if (EventSystem.current && firstSpawned)
            EventSystem.current.SetSelectedGameObject(firstSpawned);
    }

    public void Close() => panelRoot.SetActive(false);

    void Build()
    {
        // 既存を掃除
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);

        firstSpawned = null;

        foreach (var m in maskManager.Owned)
        {
            var btn = Instantiate(itemPrefab, gridRoot);
            btn.Setup(m, () =>
            {
                maskManager.Equip(m.id); // 装備！
                Close();                 // 決定後は閉じる（好みで残してもOK）
            });
            if (firstSpawned == null) firstSpawned = btn.gameObject;

            // いま装備中なら見た目ハイライト（任意）
            btn.SetEquipped(maskManager.Equipped == m);
        }
    }
}
