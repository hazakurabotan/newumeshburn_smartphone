using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIFocusGlowFollower : MonoBehaviour
{
    [Header("Follow target")]
    public RectTransform glow;     // 1枚の枠（Image）
    public Canvas canvas;          // その枠が載っているCanvas
    public Button fallbackFirst;   // 何も選ばれてない時の保険

    [Header("Style")]
    public Vector2 padding = new Vector2(20, 20);
    public float moveLerp = 15f;
    public float sizeLerp = 15f;

    GameObject last;

    Camera UICam => (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    ? canvas.worldCamera : null;

    void OnEnable()
    {
        if (EventSystem.current.currentSelectedGameObject == null && fallbackFirst != null)
            EventSystem.current.SetSelectedGameObject(fallbackFirst.gameObject);

        if (glow != null)
        {
            glow.gameObject.SetActive(true);   // 起動時に必ずON
            glow.SetAsLastSibling();           // いつも最前面
        }
        SnapToSelected();
    }

    void LateUpdate()
    {
        if (glow != null && !glow.gameObject.activeSelf)
            glow.gameObject.SetActive(true);   // 途中でOFFにされても復活
    }

    void Update()
    {
        var sel = EventSystem.current?.currentSelectedGameObject;
        if (sel == null) return;

        if (sel != last) { last = sel; SnapToSelected(); return; }

        FollowSmooth(sel);
    }

    void SnapToSelected()
    {
        var sel = EventSystem.current?.currentSelectedGameObject;
        if (sel == null || glow == null) return;

        var selRT = sel.GetComponent<RectTransform>();
        if (selRT == null) return;

        var parent = glow.parent as RectTransform;
        var cam = UICam;

        Vector3[] corners = new Vector3[4];
        selRT.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        Vector2 center = (min + max) * 0.5f;
        Vector2 size = (max - min) + padding;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, center, cam, out var localCenter);
        glow.anchoredPosition = localCenter;
        glow.sizeDelta = size;
    }

    void FollowSmooth(GameObject sel)
    {
        var selRT = sel.GetComponent<RectTransform>();
        if (selRT == null || glow == null) return;

        var parent = glow.parent as RectTransform;
        var cam = UICam;

        Vector3[] corners = new Vector3[4];
        selRT.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        Vector2 center = (min + max) * 0.5f;
        Vector2 size = (max - min) + padding;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, center, cam, out var localCenter);

        glow.anchoredPosition = Vector2.Lerp(glow.anchoredPosition, localCenter, Time.unscaledDeltaTime * moveLerp);
        glow.sizeDelta = Vector2.Lerp(glow.sizeDelta, size, Time.unscaledDeltaTime * sizeLerp);
    }
}
