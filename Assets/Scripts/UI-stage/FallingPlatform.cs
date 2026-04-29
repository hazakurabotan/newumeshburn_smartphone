using UnityEngine;

// プレイヤーが「上から」乗ったときだけ落ちる足場
public class FallingPlatform : MonoBehaviour
{
    public float fallDelay = 2f;
    private Rigidbody2D rb;
    private bool isFalling = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Playerと接触＆まだ落ちていない
        if (collision.gameObject.CompareTag("Player") && !isFalling)
        {
            // Playerの中心Y座標と足場の上辺Y座標を比較
            float playerBottomY = collision.collider.bounds.min.y + 0.05f; // 少し余裕を持たせる
            float platformTopY = GetComponent<Collider2D>().bounds.max.y - 0.05f;

            // プレイヤーの足が足場の「少し上」なら、乗ったとみなす！
            if (playerBottomY > platformTopY)
            {
                isFalling = true;
                Invoke("DropPlatform", fallDelay);
            }
        }
    }

    void DropPlatform()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        Destroy(gameObject, 2f);
    }
}
