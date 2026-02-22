using UnityEngine;

public class EnemyMissile : MonoBehaviour
{
    [Header("移動")]
    public float speed = 3f;

    [Header("スプライト")]
    public Sprite misairu1;
    public Sprite misairu2;
    public Sprite misairu3;
    public Sprite bakuhaSprite;
    public SpriteRenderer spriteRenderer;

    [Header("タイミング")]
    // misairu1 → misairu2 → misairu3 に変わる間隔
    public float changeInterval = 0.3f;
    // misairu3 になってから爆発するまでの時間
    public float afterNearDelay = 0.5f;

    [Header("ダメージ")]
    public int damage = 1;          // プレイヤーに与えるダメージ

    [Header("エフェクト（あれば）")]
    public GameObject explosionPrefab;

    Transform player;
    float timer;
    bool isNear;        // misairu3 状態か？
    bool isExploded;    // もう爆発したか？

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // プレイヤー（PlayerCore）を探す
        player = GameObject.FindWithTag("PlayerCore")?.transform;

        // 最初のスプライトを設定
        if (spriteRenderer != null && misairu1 != null)
        {
            spriteRenderer.sprite = misairu1;
        }
    }

    void Update()
    {
        if (isExploded)
            return;

        // ▼ ミサイルの移動
        if (player != null)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;
        }

        // ▼ スプライトの切り替えと、自動爆発
        timer += Time.deltaTime;

        if (!isNear)
        {
            // misairu1 → misairu2 → misairu3
            if (timer >= changeInterval)
            {
                timer = 0f;

                if (spriteRenderer.sprite == misairu1 && misairu2 != null)
                {
                    spriteRenderer.sprite = misairu2;
                }
                else if (spriteRenderer.sprite == misairu2 && misairu3 != null)
                {
                    spriteRenderer.sprite = misairu3;
                    isNear = true;
                    timer = 0f;  // misairu3 になった瞬間にタイマーリセット
                }
            }
        }
        else
        {
            // misairu3 状態になってから afterNearDelay 秒後に
            // 「プレイヤーに当たった扱い」で爆発させる
            if (timer >= afterNearDelay)
            {
                Explode(doDamage: true);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isExploded)
            return;

        // ガードで防がれた場合（盾など）
        if (other.CompareTag("Guard"))
        {
            Explode(doDamage: false);
            return;
        }

        // プレイヤーに直撃した場合
        if (other.CompareTag("PlayerCore"))
        {
            DamagePlayer(other);
            Explode(doDamage: false);   // 直撃時もここで爆発演出
        }
    }

    /// <summary>
    /// プレイヤーにダメージを与える
    /// </summary>
    void DamagePlayer(Collider2D other = null)
    {
        PlayerHP hp = null;

        // まず当たった Collider から探す
        if (other != null)
        {
            hp = other.GetComponent<PlayerHP>()
                 ?? other.GetComponentInParent<PlayerHP>()
                 ?? other.GetComponentInChildren<PlayerHP>();
        }

        // 見つからなければ、タグから探す（保険）
        if (hp == null)
        {
            GameObject playerObj = GameObject.FindWithTag("PlayerCore");
            if (playerObj != null)
            {
                hp = playerObj.GetComponent<PlayerHP>()
                     ?? playerObj.GetComponentInChildren<PlayerHP>()
                     ?? playerObj.GetComponentInParent<PlayerHP>();
            }
        }

        if (hp != null)
        {
            hp.TakeDamage(damage);
            Debug.Log($"[EnemyMissile] Player に {damage} ダメージ");
        }
        else
        {
            Debug.LogWarning("[EnemyMissile] PlayerHP が見つからないのでダメージを与えられませんでした");
        }
    }

    /// <summary>
    /// 外部（プレイヤー弾）からも呼べる爆発処理
    /// </summary>
    public void Explode(bool doDamage = false)
    {
        if (isExploded)
            return;

        if (doDamage)
        {
            // タイマーによる自動爆発用：プレイヤーにダメージ
            DamagePlayer(null);
        }

        ExplodeInternal();
    }

    /// <summary>
    /// 実際の爆発演出と削除
    /// </summary>
    void ExplodeInternal()
    {
        isExploded = true;
        timer = 0f;

        // スプライトを爆発絵に変更
        if (spriteRenderer != null && bakuhaSprite != null)
        {
            spriteRenderer.sprite = bakuhaSprite;
        }

        // コライダー無効化（これ以上当たり判定を出さない）
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 爆発エフェクトの生成（設定されていれば）
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        // 少し待ってから本体を削除
        Destroy(gameObject, 0.3f);
    }
}
