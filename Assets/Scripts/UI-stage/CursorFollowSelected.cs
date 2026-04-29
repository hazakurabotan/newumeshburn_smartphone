using UnityEngine;
using UnityEngine.EventSystems;

public class CursorFollowSelected : MonoBehaviour
{
    [SerializeField] RectTransform cursor; // ハイライト枠（DetailsRootの子）
    [SerializeField] float speed = 12f;    // 追従速度

    void Update()
    {
        var go = EventSystem.current?.currentSelectedGameObject;
        if (!go || !cursor) return;

        var target = (RectTransform)go.transform;
        // 同じ親配下ならワールド座標をそのまま使ってOK
        cursor.position = Vector3.Lerp(cursor.position, target.position, speed * Time.unscaledDeltaTime);
        // サイズも合わせたいなら：
        // cursor.sizeDelta = Vector2.Lerp(cursor.sizeDelta, target.sizeDelta, speed * Time.unscaledDeltaTime);
    }
}
