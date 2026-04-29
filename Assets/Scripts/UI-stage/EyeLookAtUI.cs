// EyeLookAtUI.cs（選択追従版）
using UnityEngine;
using UnityEngine.EventSystems;

[ExecuteAlways]
public class EyeLookAtUI : MonoBehaviour
{
    [System.Serializable]
    public class Eye
    {
        public RectTransform eyeArea;
        public RectTransform pupil;
        public Vector2 maxRadius = new(6, 6);
        public Vector2 offset;
        [HideInInspector] public Vector2 vel;
    }

    [SerializeField] Canvas canvas;
    [SerializeField] Eye left;
    [SerializeField] Eye right;
    [SerializeField, Range(0f, 1f)] float follow = 0.30f;
    [SerializeField] float smoothTime = 0.05f;
    [SerializeField] RectTransform fallbackTarget; // 選択が無い時の視線先（顔の中心など）

    void Update()
    {
        var cam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        // いま選択されているUI（ゲームパッド操作で変わる）
        RectTransform t = fallbackTarget;
        var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        if (selected) t = selected.transform as RectTransform;

        // ターゲットが取れたら、その中心のスクリーン座標を計算
        Vector2 screen;
        if (t)
        {
            var world = t.TransformPoint(t.rect.center);
            screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        }
        else
        {
            // それでも無い時はキャラの中心（=このオブジェクト）を見る
            var self = (RectTransform)transform;
            var world = self.TransformPoint(self.rect.center);
            screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        }

        UpdateEye(left, screen, cam);
        UpdateEye(right, screen, cam);
    }

    void UpdateEye(Eye e, Vector2 screen, Camera cam)
    {
        if (!e.eyeArea || !e.pupil) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(e.eyeArea, screen, cam, out var local);
        local = local * follow + e.offset;

        var r = e.maxRadius;
        float nx = local.x / r.x, ny = local.y / r.y;
        float d = nx * nx + ny * ny;
        if (d > 1f)
        {
            float s = 1f / Mathf.Sqrt(d);
            local = new Vector2(local.x * s, local.y * s);
        }

        e.pupil.anchoredPosition = Vector2.SmoothDamp(
            e.pupil.anchoredPosition, local, ref e.vel, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
    }
}
