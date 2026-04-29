using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    public Transform player;       // プレイヤー
    public float smoothSpeed = 0.2f; // 追従のなめらかさ
    public Vector2 minPosition;    // カメラの左下制限
    public Vector2 maxPosition;    // カメラの右上制限

    void LateUpdate()
    {
        if (player == null) return;

        // 追従位置を計算
        Vector3 targetPos = player.position;
        targetPos.z = transform.position.z;

        // 範囲内に収める
        targetPos.x = Mathf.Clamp(targetPos.x, minPosition.x, maxPosition.x);
        targetPos.y = Mathf.Clamp(targetPos.y, minPosition.y, maxPosition.y);

        // なめらか追従
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
    }
}
