using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MawaruPunchHitbox : MonoBehaviour
{
    [Header("与ダメージ")]
    public int damage = 2;

    [Header("敵弾をはじく（false=消す）")]
    public bool reflectEnemyBullets = true;

    [Header("当てる相手のLayer")]
    public LayerMask hittableLayers; // Enemy, EnemyBullet をチェック

    MawaruController owner;

    void Awake()
    {
        owner = GetComponentInParent<MawaruController>();

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // 保険：Square は “弾” 扱いにされないよう Tag を必ず外す
        gameObject.tag = "Untagged";

        // 物理衝突用に Layer は PlayerBullet のままでOK（弾との当たり用）
        int lb = LayerMask.NameToLayer("PlayerBullet");
        if (lb >= 0) gameObject.layer = lb;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // レイヤーフィルタ
        if ((hittableLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // 1) 敵本体
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage, "MawaruPunch");

            // 軽いノックバック（任意）
            var rb = enemy.GetComponent<Rigidbody2D>();
            if (rb)
            {
                float dir = Mathf.Sign(other.bounds.center.x - transform.position.x); // +右
                rb.velocity = new Vector2(6f * dir, rb.velocity.y + 1.5f);
            }
            return; // Square は破壊しない
        }

        // 2) 敵の弾
        var eb = other.GetComponent<EnemyBullet>();
        if (eb != null)
        {
            if (reflectEnemyBullets)
            {
                var rb = eb.GetComponent<Rigidbody2D>();
                if (rb) rb.velocity = new Vector2(-rb.velocity.x, rb.velocity.y);

                // 敵に当たる側へ切替
                other.tag = "PlayerBullet";
                int lb = LayerMask.NameToLayer("PlayerBullet");
                if (lb >= 0) other.gameObject.layer = lb;

                // ★★★ ここから追加：Mawaru と Player には当たらないようにする ★★★

                // 反射された弾の全ての collider
                var bulletCols = other.GetComponentsInChildren<Collider2D>();

                // 1) Mawaru 本人の collider を取得
                if (owner != null)
                {
                    var mawaruCols = owner.GetComponentsInChildren<Collider2D>();
                    foreach (var bCol in bulletCols)
                        foreach (var mCol in mawaruCols)
                            if (bCol && mCol)
                                Physics2D.IgnoreCollision(bCol, mCol, true);
                }

                // 2) PlayerController（Meguru側）の collider にも当たらないようにする
                var player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    var playerCols = player.GetComponentsInChildren<Collider2D>();
                    foreach (var bCol in bulletCols)
                        foreach (var pCol in playerCols)
                            if (bCol && pCol)
                                Physics2D.IgnoreCollision(bCol, pCol, true);
                }

                // ★★★ 追加ここまで ★★★
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
    }
}
