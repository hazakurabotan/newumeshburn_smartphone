using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollowBounds2D : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;              // 追従先（切替時に差し替える）
    public Vector2 offset = Vector2.zero;

    [Header("Smoothing")]
    public float smoothTime = 0.15f;      // 小さいほど追従が速い

    [Header("Stage Bounds")]
    public BoxCollider2D stageBounds;     // StageBounds を入れる

    Camera cam;
    Vector3 vel;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (!target) return;

        // desired
        Vector3 desired = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );

        // Clamp（カメラの半サイズを考慮）
        if (stageBounds)
        {
            Bounds b = stageBounds.bounds;

            float camHalfH = cam.orthographicSize;
            float camHalfW = camHalfH * cam.aspect;

            float minX = b.min.x + camHalfW;
            float maxX = b.max.x - camHalfW;
            float minY = b.min.y + camHalfH;
            float maxY = b.max.y - camHalfH;

            // ステージがカメラより小さい時の保険（min>maxになる）
            if (minX > maxX) desired.x = (b.min.x + b.max.x) * 0.5f;
            else desired.x = Mathf.Clamp(desired.x, minX, maxX);

            if (minY > maxY) desired.y = (b.min.y + b.max.y) * 0.5f;
            else desired.y = Mathf.Clamp(desired.y, minY, maxY);
        }

        // Smooth
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref vel, smoothTime);
    }

    // ★切替用：外部から呼ぶ
    public void SetTarget(Transform t)
    {
        target = t;
    }
}