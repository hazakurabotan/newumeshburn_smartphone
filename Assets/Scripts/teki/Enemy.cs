using UnityEngine;

// 敵キャラクター用の基本スクリプト
public class Enemy : MonoBehaviour
{
    public int hp = 3;               // 敵の体力（HP）
    public int maxHp = 3;
    public GameObject[] dropItemPrefabs;
    public bool isGrabbed = false;   // 掴まれている状態か（つかみアクション用フラグ）
    public bool isFlying = false;    // 飛んでいる状態か（投げられ中など）
    public EnemyHpBarController hpBar;
    private bool recentlyHit = false;  // 連続ヒット防止用フラグ


    void Start()
    {
        // 開始時に最大HPをセット
        if (hpBar != null)
            hpBar.SetHp(hp, maxHp);
    }


    // ======= ダメージ処理 =======
    public void TakeDamage(int amount, string cause = "other")
    {
        // 連続ヒット防止（攻撃の当たり判定を一瞬だけ無効化）
        if (recentlyHit) return;               // すでにヒット判定中なら何もしない
        recentlyHit = true;                    // ヒットフラグON
        Invoke(nameof(ResetHit), 0.05f);       // 0.05秒後にフラグ解除（調整可能）

        // 掴まれている最中は無敵（ただし「飛んでいる」場合はダメージ有効）
        if (isGrabbed && !isFlying) return;

        // HPを減らす
        hp -= amount;
        if (hpBar != null)
            hpBar.SetHp(hp, maxHp);

        // HPが0以下になったら死亡処理
        if (hp <= 0)
        {
            Die(cause);
        }
    }




    // ======= 連続ヒット解除用関数 =======
    private void ResetHit()
    {
        recentlyHit = false; // 再び攻撃を受け付ける
    }

    // ======= 死亡処理 =======
    private void Die(string cause)
    {
        // 1. ドロップ
        if (dropItemPrefabs != null && dropItemPrefabs.Length > 0)
        {
            if (Random.value < 0.5f)
            {
                int itemType = Random.Range(0, dropItemPrefabs.Length);
                Instantiate(dropItemPrefabs[itemType], transform.position, Quaternion.identity);
            }
        }

        // 2. キルカウント
        if (cause == "gun" && GameManager.Instance != null)
            GameManager.Instance.AddKill();
        // summonや他の手段ではカウントしない

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.GetComponent<PlayerController>().score += 100;
        }



        Destroy(gameObject);
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            TakeDamage(2, "gun");
            Destroy(other.gameObject);
        }
        // ...ほかの処理
    }


}
