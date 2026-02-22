using UnityEngine;

public class MechaGuardArm : MonoBehaviour
{
    public Vector3 guardOffset;   // ガード時にどれだけ前に出すか（ローカル座標）
    public float moveSpeed = 10f; // ガード位置までの追従速度

    Vector3 defaultLocalPos;
    bool guarding = false;

    void Awake()
    {
        defaultLocalPos = transform.localPosition;
        gameObject.SetActive(false);
    }

    public void SetGuard(bool on)
    {
        if (guarding == on) return;

        guarding = on;

        if (on)
        {
            gameObject.SetActive(true);
        }
        else
        {
            // 解除されたら元の位置に戻して非表示
            transform.localPosition = defaultLocalPos;
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!guarding) return;

        // ガード中は前に出た位置へスムーズに移動
        Vector3 target = defaultLocalPos + guardOffset;
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            target,
            Time.deltaTime * moveSpeed
        );
    }
}
