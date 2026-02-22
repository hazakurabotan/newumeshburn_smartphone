using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MawaruAnimatorBridge : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundRadius = 0.1f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] SpriteRenderer sr; // 左右反転に使う（任意）

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        float speedAbs = Mathf.Abs(rb.velocity.x);
        bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        animator.SetFloat("Speed", speedAbs);
        animator.SetBool("Grounded", grounded);

        // 進行方向で左右反転（任意）
        if (sr && speedAbs > 0.01f) sr.flipX = rb.velocity.x < 0f;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck) { Gizmos.color = Color.green; Gizmos.DrawWireSphere(groundCheck.position, groundRadius); }
    }
}
