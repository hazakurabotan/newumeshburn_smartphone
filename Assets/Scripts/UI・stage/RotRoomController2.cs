using System.Collections;
using UnityEngine;

public class RotRoomController2 : MonoBehaviour
{
    [Header("Rotate Target (RoomRoot)")]
    [SerializeField] private Transform roomRoot;

    [Header("Pivot (RoomGear中心など)")]
    [SerializeField] private Transform pivot;   // ★回転の支点

    [SerializeField] private float rotateDuration = 0.25f;

    Rigidbody2D rb;
    bool isRotating;
    int state; // 0=0, 1=90, 2=180, 3=270

    public bool IsRotating => isRotating;

    void Awake()
    {
        if (roomRoot == null) roomRoot = transform;

        rb = roomRoot.GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogError("[RotRoomController2] RoomRoot に Rigidbody2D(Kinematic) が必要です");

        if (pivot == null)
            Debug.LogError("[RotRoomController2] pivot が未設定です（RoomGear中心の Empty を割り当ててください）");
    }

    public bool RequestRotateStep(int dir)
    {
        if (isRotating) return false;
        if (dir == 0) return false;

        int next = (state + (dir > 0 ? 1 : 3)) % 4; // +90 or -90
        StartCoroutine(RotateTo(next));
        return true;
    }

    IEnumerator RotateTo(int nextState)
    {
        isRotating = true;

        float fromZ = roomRoot.eulerAngles.z;
        float toZ = nextState * 90f;

        Vector2 pivotPos = pivot.position;

        // ★開始時点の位置（支点からのオフセット）を記録
        Vector2 startPos = roomRoot.position;
        Vector2 startOffset = startPos - pivotPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.fixedDeltaTime / rotateDuration;
            float z = Mathf.LerpAngle(fromZ, toZ, Mathf.SmoothStep(0, 1, t));

            // ★「fromZ から z まで」の回転差分で、支点まわりに位置を補正
            float deltaZ = z - fromZ;
            Quaternion q = Quaternion.Euler(0f, 0f, deltaZ);
            Vector2 newPos = pivotPos + (Vector2)(q * startOffset);

            if (rb)
            {
                rb.MovePosition(newPos);
                rb.MoveRotation(z);
            }
            else
            {
                roomRoot.SetPositionAndRotation(newPos, Quaternion.Euler(0, 0, z));
            }

            yield return new WaitForFixedUpdate();
        }

        state = nextState;
        isRotating = false;
    }
}