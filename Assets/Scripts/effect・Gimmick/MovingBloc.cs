using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 動く床（リフトやエレベーターなど）を制御するスクリプト
public class MovingBloc : MonoBehaviour
{
    public float moveX = 0.0f;   // X方向の移動距離
    public float moveY = 0.0f;   // Y方向の移動距離
    public float times = 0.0f;   // 片道にかかる移動時間（秒）
    public float wait = 0.0f;    // 停止時間（往復の端での待ち時間）
    public bool isMoveWhenOn = false; // 乗ったときだけ動くならON
    public bool isCanMove = true;     // 床が現在動けるかどうか

    Vector3 startPos;  // 床の初期位置
    Vector3 endPos;    // 移動先の位置
    bool isReverse = false; // 移動方向を逆転するフラグ

    float movep = 0.0f; // Lerp補間値（0→1）

    // ========== 初期化 ==========
    void Start()
    {
        startPos = transform.position; // スタート位置記録
        endPos = new Vector2(startPos.x + moveX, startPos.y + moveY); // 終点を計算

        // 乗った時だけ動く設定なら最初は動かさない
        if (isMoveWhenOn)
        {
            isCanMove = false;
        }
    }

    // ========== 毎フレーム呼ばれる ==========
    void Update()
    {
        // 動ける状態なら…
        if (isCanMove)
        {
            float distance = Vector2.Distance(startPos, endPos); // 総移動距離
            float ds = distance / times;        // 1秒あたりの移動距離
            float df = ds * Time.deltaTime;     // 1フレームで進む距離
            movep += df / distance;             // 補間値を進める（0→1）

            if (isReverse)
            {
                // 終点→始点に戻る
                transform.position = Vector2.Lerp(endPos, startPos, movep);
            }
            else
            {
                // 始点→終点へ進む
                transform.position = Vector2.Lerp(startPos, endPos, movep);
            }

            // 端まで到達したら
            if (movep >= 1.0f)
            {
                movep = 0.0f;             // 補間値リセット
                isReverse = !isReverse;   // 移動方向を逆転
                isCanMove = false;        // 一時停止

                if (!isMoveWhenOn)
                {
                    // 「乗った時だけ動く」でなければ、wait秒後に再始動
                    Invoke("Move", wait);
                }
            }
        }
    }

    // ========== 外部から移動開始する関数 ==========
    public void Move()
    {
        isCanMove = true;
    }

    // ========== 外部から移動停止する関数 ==========
    public void Stop()
    {
        isCanMove = false;
    }

    // ========== プレイヤーが乗った時の処理 ==========
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            // プレイヤーをこの床の子オブジェクトに（親子関係にして一緒に動かす）
            collision.transform.SetParent(transform);

            if (isMoveWhenOn)
            {
                // 乗った時だけ動く場合、ここで移動開始
                isCanMove = true;
            }
        }
    }

    // ========== プレイヤーが降りた時の処理 ==========
    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            // プレイヤーを元の親に戻す（親子関係解除）
            collision.transform.SetParent(null);
        }
    }

    // ========== エディタ上で移動範囲を可視化 ==========
    void OnDrawGizmosSelected()
    {
        Vector2 fromPos;
        if (startPos == Vector3.zero)
        {
            fromPos = transform.position;
        }
        else
        {
            fromPos = startPos;
        }

        // 移動ルートの線（中心から移動量ぶんのベクトル）
        Gizmos.DrawWireCube(fromPos, new Vector2(fromPos.x + moveX, fromPos.y + moveY));

        // 床本体のサイズ
        Vector2 size = GetComponent<SpriteRenderer>().size;
        // 初期位置のワイヤーフレーム
        Gizmos.DrawWireCube(fromPos, new Vector2(size.x, size.y));
        // 移動先のワイヤーフレーム
        Vector2 toPos = new Vector3(fromPos.x + moveX, fromPos.y + moveY);
        Gizmos.DrawWireCube(toPos, new Vector2(size.x, size.y));
    }
}
