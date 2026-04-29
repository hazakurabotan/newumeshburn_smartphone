using System.Collections;
using UnityEngine;

public class RotRoomController : MonoBehaviour
{
    [Header("Rotate Target (ここが回る)")]
    [SerializeField] private Transform roomRoot;        // ← ここに Grid_Room を入れる
    [SerializeField] private float rotateDuration = 0.25f;

    [Header("Freeze")]
    [SerializeField] private FreezeAllEnemies freezeAllEnemies;

    private Rigidbody2D rb;
    private bool isRotating;
    private int currentState; // 0:0deg / 1:90deg

    public bool IsRotating => isRotating;

    private void Awake()
    {
        if (roomRoot == null) roomRoot = transform;

        // 回す対象に Rigidbody2D が付いてる前提（Grid_Room に付ける）
        rb = roomRoot.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("[RotRoomController] roomRoot に Rigidbody2D がありません。Grid_Room に Rigidbody2D(Kinematic)を付けて roomRoot に割り当ててください。");
        }
    }

    public bool RequestRotate(int dir)
    {
        if (isRotating) return false;
        if (dir == 0) return false;

        // いまは 0 ↔ 90 のトグル
        int next = (currentState == 0) ? 1 : 0;
        StartCoroutine(RotateToState(next));
        return true;
    }

    private IEnumerator RotateToState(int nextState)
    {
        isRotating = true;
        freezeAllEnemies?.SetFrozen(true);

        float fromZ = roomRoot.eulerAngles.z;
        float toZ = (nextState == 0) ? 0f : 90f;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.fixedDeltaTime / rotateDuration;
            float z = Mathf.LerpAngle(fromZ, toZ, Mathf.SmoothStep(0, 1, t));

            if (rb != null)
                rb.MoveRotation(z);
            else
                roomRoot.rotation = Quaternion.Euler(0, 0, z);

            yield return new WaitForFixedUpdate();
        }

        currentState = nextState;
        freezeAllEnemies?.SetFrozen(false);
        isRotating = false;
    }
}