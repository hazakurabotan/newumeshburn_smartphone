using UnityEngine;
using UnityEngine.EventSystems;

public class UmeMenuCursor : MonoBehaviour
{
    [Header("References")]
    public RectTransform root;         // MainMenu の RectTransform
    public GameObject firstSelect;     // 最初に選ばせたいボタン(WEAPON)

    [Header("Follow")]
    public float moveSpeed = 14f;
    public Vector2 padding = new Vector2(16, 16);

    RectTransform rt;

    void Awake() { rt = GetComponent<RectTransform>(); }

    void OnEnable()
    {
        // 最初に何も選択されていないケースへ対応
        if (firstSelect) EventSystem.current?.SetSelectedGameObject(firstSelect);
    }

    void LateUpdate()
    {
        var es = EventSystem.current;
        if (es == null) return;

        var sel = es.currentSelectedGameObject;
        if (sel == null)
        {
            if (firstSelect) es.SetSelectedGameObject(firstSelect);
            return;
        }

        var tr = sel.transform as RectTransform;
        if (tr == null || root == null) return;

        // ボタンの中心をワールド→rootローカルへ
        var worldCenter = tr.TransformPoint(tr.rect.center);
        var localCenter = root.InverseTransformPoint(worldCenter);

        // 位置
        rt.anchoredPosition = Vector2.Lerp(
            rt.anchoredPosition, (Vector2)localCenter, Time.unscaledDeltaTime * moveSpeed);

        // サイズ（ボタンのサイズ＋パディング）
        var size = tr.rect.size + padding;
        rt.sizeDelta = Vector2.Lerp(rt.sizeDelta, size, Time.unscaledDeltaTime * moveSpeed);
    }
}
