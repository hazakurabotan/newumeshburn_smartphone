using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// シェル（敵弾やトゲ弾など）のダメージ処理スクリプト
public class ShellDamage : MonoBehaviour
{
    public int damage = 1; // プレイヤーに与えるダメージ量（Inspectorで調整可能）

    // 他のCollider2D（当たり判定）とぶつかった時に呼ばれる
    private void OnTriggerEnter2D(Collider2D other)
    {
        // ===== プレイヤーに当たった時 =====
        if (other.CompareTag("Player"))
        {
            // プレイヤーのPlayerControllerスクリプトを取得
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // プレイヤーにダメージを与える
                player.TakeDamage(damage);
            }

            // 弾自身は消す
            Destroy(gameObject);
        }
        // ===== 地面やブロックなどに当たった時も消す =====
        else if (other.CompareTag("Ground") || other.CompareTag("Block"))
        {
            Destroy(gameObject);
        }
    }
}
