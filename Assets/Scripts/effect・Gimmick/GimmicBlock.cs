using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// プレイヤーが近づくと落下し、落下後に消えるギミックブロックのスクリプト
public class GimmicBlock : MonoBehaviour
{
    public float length = 0.0f;       // プレイヤーを感知する距離（この範囲に入るとブロックが落下）
    public bool isDelete = false;     // 落下後に消滅するかどうか（ONなら消える）
    public GameObject deadObj;        // 死亡判定用オブジェクト（ブロック落下中に有効化）

    bool isFell = false;              // ブロックが落下中かどうかのフラグ
    float fadeTime = 0.5f;            // フェードアウト演出の残り時間（秒）

    // ===== ゲーム開始時に一度だけ呼ばれる =====
    void Start()
    {


        // Rigidbody2Dの物理挙動を一時停止（静止状態にする）
        Rigidbody2D rbody = GetComponent<Rigidbody2D>();
        rbody.bodyType = RigidbodyType2D.Static;

        deadObj.SetActive(false); // 死亡判定用オブジェクトを非表示
    }

    // ===== 毎フレーム呼ばれる =====
    void Update()
    {


        // タグ"Player"がついたオブジェクト（プレイヤー）を探す
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // プレイヤーとの距離を測定
            float d = Vector2.Distance(transform.position, player.transform.position);

            // 距離がlength以内なら落下を開始
            if (length >= d)
            {
                Rigidbody2D rbody = GetComponent<Rigidbody2D>();
                if (rbody.bodyType == RigidbodyType2D.Static)
                {
                    // Rigidbody2Dの物理挙動を開始（動的にする）
                    rbody.bodyType = RigidbodyType2D.Dynamic;
                    deadObj.SetActive(true); // 死亡判定用オブジェクトを表示
                }
            }
        }

        // 落下後のフェードアウト処理
        if (isFell)
        {
            fadeTime -= Time.deltaTime; // フェード時間を減らしていく
            Color col = GetComponent<SpriteRenderer>().color;
            // アルファ値（透明度）を0→1に正規化して徐々に消していく
            col.a = Mathf.Clamp01(fadeTime / 0.5f); // 0.5→0.0へ
            GetComponent<SpriteRenderer>().color = col;

            // 完全に消えたらブロックを削除
            if (fadeTime <= 0.0f)
            {
                Destroy(gameObject);
            }
        }
    }

    // ===== 他のCollider2Dとぶつかったときに呼ばれる =====
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collision detected with: " + collision.gameObject.name);

        // isDeleteがONなら落下フラグON（フェード開始）
        if (isDelete)
        {
            isFell = true;
        }
    }

    // ===== エディタ上で範囲を可視化する（選択中のみ表示） =====
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, length); // 赤い線で範囲表示
    }
}
