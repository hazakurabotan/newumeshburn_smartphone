using UnityEngine;

// ----------------------------------------------
// PlayerShoot
// プレイヤーの弾発射（連射・チャージ・リロード対応）を管理するスクリプト
// ----------------------------------------------
public class PlayerShoot : MonoBehaviour
{
    public GameObject bulletPrefab;    // 弾プレハブ（弾の元となるオブジェクト）
    public Transform firePoint;        // 発射位置（Transform）
    public float bulletSpeed = 5f;     // 弾の速度

    [HideInInspector] public float chargeTime = 0f;    // チャージ時間（公開だがインスペクター非表示）
    [HideInInspector] public bool isCharging = false;  // チャージ中フラグ
    public float requiredCharge = 2.0f;                // フルチャージに必要な秒数

    public int maxShots = 3;         // 連射できる回数（弾数制限）
    public float reloadTime = 1.0f;  // 弾切れ後のリロード時間
    private int shotsFired = 0;      // 現在連射した回数
    private float lastFireTime = -99f;   // 最後に撃った時間（リロード判定用）
    private bool isReloading = false;    // リロード中かどうか

    PlayerController playerController;   // プレイヤー本体の情報取得用

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        // リロード中は撃てない
        if (isReloading)
        {
            if (Time.time - lastFireTime > reloadTime)
            {
                shotsFired = 0;
                isReloading = false;
            }
            return;
        }

        // キーボードチャージ開始
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (shotsFired < maxShots)
            {
                isCharging = true;
                chargeTime = 0f;
            }
            else
            {
                isReloading = true;
                lastFireTime = Time.time;
            }
        }

        // ---この判定を必ずこうする---
        if (isCharging)
        {
            chargeTime += Time.deltaTime;
        }

        // キーボード発射
        if (isCharging && Input.GetKeyUp(KeyCode.X))
        {
            Shoot(chargeTime >= requiredCharge);
            isCharging = false;
            chargeTime = 0f;
            shotsFired++;
            if (shotsFired >= maxShots)
            {
                isReloading = true;
                lastFireTime = Time.time;
            }
        }
    }


    // --- 弾を発射する処理（powered=trueでパワーショット）---
    public void Shoot(bool powered)
    {
        // 弾を発生させて初期位置・速度を与える
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        PlayerBullet pb = bullet.GetComponent<PlayerBullet>();

        // プレイヤーの向き（ローカルスケールX）で左右発射切り替え
        float direction = transform.localScale.x > 0 ? 1f : -1f;
        if (rb != null)
            rb.velocity = new Vector2(direction * bulletSpeed, 0f);

        // ダメージや大きさも切り替え
        if (pb != null)
        {
            int baseDamage = 1;
            if (playerController != null)
                baseDamage = playerController.bulletDamage;

            if (powered)
            {
                pb.damage = baseDamage + 2;         // パワーショットは＋２ダメージ
                bullet.transform.localScale *= 4f;  // サイズも大きく
            }
            else
            {
                pb.damage = baseDamage;
            }
        }
        Destroy(bullet, 3.0f); // 3秒後に弾を消す（メモリリーク防止）
    }

    // ボタン押した時
    public void OnShootButtonDown()
    {
        if (shotsFired < maxShots)
        {
            isCharging = true;
            chargeTime = 0f;
        }
        else
        {
            isReloading = true;
            lastFireTime = Time.time;
        }
    }

    // ボタン離した時
    public void OnShootButtonUp()
    {
        if (isCharging)
        {
            Shoot(chargeTime >= requiredCharge); // チャージ判定
            isCharging = false;
            chargeTime = 0f;
            shotsFired++;
            if (shotsFired >= maxShots)
            {
                isReloading = true;
                lastFireTime = Time.time;
            }
        }
    }

}
