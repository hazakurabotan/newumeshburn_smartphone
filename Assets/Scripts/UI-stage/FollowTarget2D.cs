using UnityEngine;

public class FollowTarget2D : MonoBehaviour
{
    public Transform target;      // Mawaru の Transform
    public Vector2 offset;        // (0, 0) 推奨
    public float smooth = 5f;     // 追従の滑らかさ
    public Vector2 minClamp;      // 画面左下ワールド座標
    public Vector2 maxClamp;      // 画面右上ワールド座標

    float z;

    void Awake() { z = transform.position.z; }

    void LateUpdate()
    {
        if (!target) return;
        var desired = new Vector3(target.position.x + offset.x,
                                  target.position.y + offset.y, z);

        // スムーズ追従
        var pos = Vector3.Lerp(transform.position, desired, Time.deltaTime * smooth);

        // ステージ外に出ないようクランプ（不要なら削除）
        pos.x = Mathf.Clamp(pos.x, minClamp.x, maxClamp.x);
        pos.y = Mathf.Clamp(pos.y, minClamp.y, maxClamp.y);

        transform.position = pos;
    }
}
