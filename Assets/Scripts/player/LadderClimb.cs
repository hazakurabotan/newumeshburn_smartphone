using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// はしごを登れるようにするためのスクリプト
public class LadderClimb : MonoBehaviour
{
    public float climbSpeed = 3f;       // はしごを登る速さ
    private bool onLadder = false;      // 今はしごの上にいるかどうか
    private Rigidbody2D rb;             // 物理演算（移動・ジャンプ等）用のコンポーネント

    void Start()
    {
        rb = GetComponent<Rigidbody2D>(); // Rigidbody2Dを取得
    }

    void Update()
    {
        // はしごに触れている間だけ登り降りできる
        if (onLadder)
        {
            // 上下キーの入力（上＝1、下＝-1、何も押さない＝0）
            float vertical = Input.GetAxisRaw("Vertical");

            // 重力を0にして、はしごの上下移動のみにする
            rb.gravityScale = 0f;

            // 上下移動だけ速度をセット（横はそのまま）
            rb.velocity = new Vector2(rb.velocity.x, vertical * climbSpeed);
        }
    }

    // はしご（Ladderタグ付き）に入った瞬間に呼ばれる
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            onLadder = true; // はしご上フラグON
        }
    }

    // はしごから出た瞬間に呼ばれる
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            onLadder = false;    // はしご上フラグOFF
            rb.gravityScale = 1f; // 重力を元に戻す（普通のジャンプや落下に戻る）
        }
    }
}
