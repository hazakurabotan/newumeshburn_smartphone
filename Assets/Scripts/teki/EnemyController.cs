using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 敵キャラクターの移動・反転・制御用スクリプト
public class EnemyController : MonoBehaviour
{

    public GameObject bulletPrefab;
    public float bulletSpeed = 6f;
    public float shootInterval = 2.0f;
    private float shootTimer = 0f;
    private Transform player;


    public Transform firePoint;  

    public float speed = 3.0f;           // 敵の移動速度
    public bool isToRight = false;       // true=右向き、false=左向き（初期値）
    public float revTime = 0f;           // 一定時間ごとに自動で反転したい時の間隔（0なら自動反転なし）
    public LayerMask groundLayer;        // 地面判定用レイヤー（Inspectorで設定）

    float time = 0f;                     // 経過時間カウント（自動反転用）

    private Enemy enemy;                 // Enemy（HP管理や投げられ状態）スクリプトの参照

    void Start()
    {
        // このオブジェクトに付いているEnemyスクリプトを取得
        enemy = GetComponent<Enemy>();

        // 初期の向きを反映（右向きならxスケールを-1にして反転）
        if (isToRight)
        {
            transform.localScale = new Vector2(-1, 1);
        }

        // プレイヤーのTransformを取得
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;

    }
    


    void Update()
    {
        // revTimeが0より大きい時だけ自動反転処理を行う
        if (revTime > 0)
        {
            time += Time.deltaTime; // 経過時間を加算
            if (time >= revTime)
            {
                isToRight = !isToRight; // 向きを反転
                time = 0f;              // タイマーリセット
                // 見た目も左右反転（localScale.xを切り替え）
                transform.localScale = isToRight ? new Vector2(-1, 1) : new Vector2(1, 1);
            }
        }

        if (player != null)
        {
            shootTimer += Time.deltaTime;
            if (shootTimer >= shootInterval)
            {
                ShootToPlayer();
                shootTimer = 0f;
            }
        }


    }


    void ShootToPlayer()
    {
        
        Vector2 dir = (player.position - transform.position).normalized;
        Vector3 shootPos = firePoint != null ? firePoint.position : transform.position; // ←ここ！
        GameObject bullet = Instantiate(bulletPrefab, shootPos, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = dir * bulletSpeed;
        }
    }

    void FixedUpdate()
    {
        // 投げられてる最中は自力で動かない
        if (enemy != null && enemy.isFlying) return;

        // ===== 地面判定 =====
        bool onGround = Physics2D.CircleCast(
            transform.position,   // 中心
            0.5f,                 // 半径
            Vector2.down,         // 下向きにサーチ
            0.5f,                 // 距離
            groundLayer           // チェックするレイヤー
        );

        if (onGround)
        {
            Rigidbody2D rbody = GetComponent<Rigidbody2D>();
            if (rbody != null)
            {
                // 向きに応じて左右に速度をセット
                float moveX = isToRight ? speed : -speed;
                rbody.velocity = new Vector2(moveX, rbody.velocity.y);
            }
        }
    }

    // === 壁やトリガーにぶつかった時に反転（自動で呼ばれる） ===
    private void OnTriggerEnter2D(Collider2D collision)
    {
        isToRight = !isToRight; // 向きを逆に
        time = 0f;              // タイマーもリセット
        // 見た目も反転
        transform.localScale = isToRight ? new Vector2(-1, 1) : new Vector2(1, 1);
    }
}
