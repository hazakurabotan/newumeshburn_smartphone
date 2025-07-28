using UnityEngine;

// -----------------------------------------
// Rope
// プレイヤーが前方にロープを伸ばし、敵や箱を掴んで
// 「手元に引き寄せ→指定方向に投げる」ことができるスクリプト
// -----------------------------------------
public class Rope : MonoBehaviour
{
    public float extendSpeed = 8f;    // ロープが伸びる速さ（毎秒）
    public float maxLength = 15f;      // ロープの最大長さ

    public float pullSpeed = 5f;      // 掴んだ対象を手元に引き寄せる速さ
    public float throwForce = 55f;    // 投げるときの初速（力の大きさ）

    private float currentLength = 0.1f; // 今のロープの長さ
    private bool extending = true;      // 今伸ばしてる最中かどうか

    private GameObject target;          // 掴んだ対象オブジェクト（敵や箱）
    private Transform player;           // プレイヤーへの参照
    private bool holding = false;       // 今掴んでいる状態か
    private bool grabbed = false;       // 掴めたかどうか（成功フラグ）

    Vector2 moveDirection = Vector2.right; // ロープの進行方向（初期は右）

    void Start()
    {
        // プレイヤー取得＆向き取得
        player = GameObject.FindGameObjectWithTag("Player").transform;
        float direction = Mathf.Sign(player.localScale.x);

        // プレイヤーの少し前から発生させる
        transform.position = player.position + new Vector3(0.5f * direction, 0, 0);

        // 初期の進行方向・角度も設定
        SetDirection(moveDirection);

        // 伸び始めた時の大きさセット（細く短く）
        Vector3 scale = transform.localScale;
        scale.x = 0.1f;
        transform.localScale = scale;
        currentLength = 0.1f;
        extending = true;

        // 1秒以内に何も掴めなかったら自壊（消える）
        Invoke(nameof(CheckIfNotGrabbed), 1f);
    }

    // --- 呼び出し時に進行方向と見た目を指定 ---
    public void SetDirection(Vector2 dir)
    {
        moveDirection = dir.normalized;
        // ロープ本体の見た目の角度も回転
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        
        // --- 何かを掴んでいるときの処理 ---
        if (holding && target != null)
        {
            // 対象をプレイヤーの位置まで毎フレーム引き寄せる
            target.transform.position = Vector2.MoveTowards(
                target.transform.position,
                player.position,
                pullSpeed * Time.deltaTime
            );

            // Cキー離したら投げる
            if (Input.GetKeyUp(KeyCode.C))
            {
                Throw();
            }
        }
    }

    // --- 掴んでいる対象を投げる処理 ---
    void Throw()
    {
        Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir = moveDirection;
            rb.velocity = dir.normalized * throwForce; // 指定方向に初速を与える

            // 敵オブジェクトなら専用フラグをセット
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.isGrabbed = false;
                enemy.isFlying = true;
            }

            // 箱オブジェクト等にも「投げられた」処理
            ThrowableObject to = target.GetComponent<ThrowableObject>();
            if (to != null) to.ActivateAsProjectile();
        }
        Destroy(gameObject); // ロープ自体は消す
    }

    // --- コライダー接触時（何かに当たったら） ---
    void OnTriggerEnter2D(Collider2D other)
    {
        if (target != null) return; // すでに何か掴んでたら無視

        if (other.CompareTag("Block"))
        {
            // 壁などに当たった場合は、ぶら下がりポイントとしてplayerに通知
            var player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
            if (player != null)
            {
                player.StartHangFromRope(transform.position);
            }
            Destroy(gameObject); // ロープ自体を消す
            return;
        }

        if (other.CompareTag("Enemy") || other.CompareTag("Throwable"))
        {
            // 掴む対象ならロックオン
            target = other.gameObject;
            holding = true;
            grabbed = true;
            extending = false; // もうこれ以上伸ばさない

            // 速度をリセットしておく
            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;

            // 敵フラグのON
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null) enemy.isGrabbed = true;
        }
    }

    // --- 1秒以内に何も掴まなかった場合、自動で消える ---
    void CheckIfNotGrabbed()
    {
        if (!grabbed)
        {
            Destroy(gameObject);
        }
    }

    public void OnThrowButton()
    {
        if (holding && target != null)
        {
            Throw();
        }
    }
}
