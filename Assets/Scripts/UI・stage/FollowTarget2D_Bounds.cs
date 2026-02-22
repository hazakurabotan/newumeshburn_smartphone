using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowTarget2D_Bounds : MonoBehaviour
{
    public Transform target;
    public Vector2 offset = Vector2.zero;
    public float smooth = 5f;

    [Header("Stage Bounds (BoxCollider2D)")]
    public BoxCollider2D stageBounds;

    Camera cam;
    float z;

    void Awake()
    {
        cam = GetComponent<Camera>();
        z = transform.position.z;
    }

    void LateUpdate()
    {
        if (!target || !stageBounds) return;

        // 追従したい位置（中心）
        Vector3 desired = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            z
        );

        // カメラ半径（ワールド）
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        // Bounds（ワールド）
        Bounds b = stageBounds.bounds;

        // 「カメラ枠がはみ出さない」クランプ範囲
        float minX = b.min.x + halfW;
        float maxX = b.max.x - halfW;
        float minY = b.min.y + halfH;
        float maxY = b.max.y - halfH;

        // ステージが画面より小さい時の保険（min > max になるのを防ぐ）
        if (minX > maxX) { minX = maxX = b.center.x; }
        if (minY > maxY) { minY = maxY = b.center.y; }

        desired.x = Mathf.Clamp(desired.x, minX, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, maxY);

        // なめらか追従
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);
    }
}