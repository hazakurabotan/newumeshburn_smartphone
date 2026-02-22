using UnityEngine;

public class SummonFastWalker : MonoBehaviour
{
    [Header("パトロール設定")]
    public float patrolWidth = 4f; // 左右の移動範囲
    public float speed = 20f;      // 移動速度

    [Header("歩行アニメのパラメータ名")]
    public string moveParamName = "isMoving";

    private Vector3 startPos;
    private int direction = 1;
    private Rigidbody2D rb;
    private Animator animator;

    bool HasParam(Animator anim, string name)
    {
        foreach (var p in anim.parameters) if (p.name == name) return true;
        return false;
    }

    void Start()
    {
        Destroy(gameObject, 3f);

        startPos = transform.position;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void FixedUpdate()
    {
        float left = startPos.x - patrolWidth / 2f;
        float right = startPos.x + patrolWidth / 2f;

        if ((direction == 1 && transform.position.x >= right) ||
            (direction == -1 && transform.position.x <= left))
        {
            direction *= -1;
            // 向きを変える
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        // X方向高速移動
        rb.velocity = new Vector2(speed * direction, rb.velocity.y);

        // 歩行アニメ切り替え
        if (animator != null && !string.IsNullOrEmpty(moveParamName))
        {
            if (animator && !string.IsNullOrEmpty(moveParamName) && HasParam(animator, moveParamName))
                animator.SetBool(moveParamName, true);
            animator.SetBool(moveParamName, true);
        }
    }
}
