using UnityEngine;

public class AimCursorController : MonoBehaviour
{
    public float moveSpeed = 600f;    // 照準の速さ
    public Vector2 clampX;            // Xの移動範囲
    public Vector2 clampY;            // Yの移動範囲

    Vector2 moveInput;                // 入力ベクトル（RoboBattleControllerから渡す）

    // RoboBattleController から毎フレーム呼ばれる
    public void SetInput(Vector2 input)
    {
        moveInput = input;
    }

    void Update()
    {
        Vector3 pos = transform.localPosition;

        // 入力ベクトルに速度とTime.deltaTimeを掛けて移動
        Vector3 delta =
            new Vector3(moveInput.x, moveInput.y, 0f) * moveSpeed * Time.deltaTime;

        pos += delta;

        // 画面内にクランプ
        pos.x = Mathf.Clamp(pos.x, clampX.x, clampX.y);
        pos.y = Mathf.Clamp(pos.y, clampY.x, clampY.y);

        transform.localPosition = pos;
    }
}
