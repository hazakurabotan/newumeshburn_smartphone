using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    [Header("Bullet")]
    public GameObject bulletPrefab;     // shell.prefab
    public Transform firePoint;         // FirePoint
    public float bulletSpeed = 5f;

    [Header("Charge (互換用：Inspector/他スクリプト参照のため残す)")]
    public float requiredCharge = 2f;   // 使わない（残すだけ）
    public bool isCharging = false;     // 使わない（残すだけ）
    public float chargeTime = 0f;       // 使わない（残すだけ）

    [Header("Ammo")]
    public int maxShots = 3;
    public float reloadTime = 1.0f;

    int shotsFired = 0;
    float lastFireTime = -999f;
    bool isReloading = false;

    [Header("Animator (ShotPose Trigger)")]
    public string shotTriggerName = "Shot"; // AnimatorのTrigger名
    Animator animator;

    PlayerController playerController;
    CapsuleCollider2D playerCol;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        playerCol = GetComponent<CapsuleCollider2D>();
    }

    void Update()
    {
        // リロード処理
        if (isReloading && Time.time - lastFireTime >= reloadTime)
        {
            shotsFired = 0;
            isReloading = false;
        }
    }

    // =========================
    // ここが PlayerController.OnShoot(ctx.started) から呼ばれる
    // 「押した瞬間に撃つ」仕様に変更
    // =========================
    public void OnShootButtonDown()
    {
        if (isReloading) return;

        if (shotsFired >= maxShots)
        {
            isReloading = true;
            lastFireTime = Time.time;
            return;
        }

        // ★押した瞬間にShotPoseへ
        if (animator != null && !string.IsNullOrEmpty(shotTriggerName))
        {
            animator.SetTrigger(shotTriggerName);
        }

        // ★通常弾を即発射（チャージ無し）
        Shoot(false);

        shotsFired++;
        if (shotsFired >= maxShots)
        {
            isReloading = true;
            lastFireTime = Time.time;
        }
    }

    // 「離した瞬間」は今回何もしない（互換のため残す）
    public void OnShootButtonUp()
    {
        // 何もしない
        // （将来チャージ制を戻すなら、ここで powered 判定して Shoot(true/false) にできる）
        isCharging = false;
        chargeTime = 0f;
    }

    // =========================
    // 弾生成
    // powered=true は将来用（今回は常にfalse）
    // =========================
    public void Shoot(bool powered)
    {
        if (bulletPrefab == null || firePoint == null) return;

        // 向き：scale.x の符号で左右判定
        float dir = (transform.localScale.x >= 0f) ? 1f : -1f;

        Vector3 spawnPos = firePoint.position + new Vector3(0.25f * dir, 0f, 0f);
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // 左向きなら見た目だけ反転
        if (dir < 0f)
            bullet.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // 速度
        var rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = new Vector2(dir * bulletSpeed, 0f);

        // 自分と弾の衝突だけ無視（ここは巻き込み防止で「自分のCapsuleだけ」に限定）
        if (playerCol != null)
        {
            var bCol = bullet.GetComponent<Collider2D>();
            if (bCol != null)
                Physics2D.IgnoreCollision(bCol, playerCol, true);
        }

        // もし PlayerBullet みたいなスクリプトが弾に付いてて damage を持ってるなら反映
        // （クラス名が違ってもコンパイル壊さないため、TryGetComponent を使う）
        int baseDamage = (playerController != null) ? playerController.bulletDamage : 1;

        // DamageDealerが付いてるならそれを優先して上書き（あなたのshellに付いてる）
        var dd = bullet.GetComponent<DamageDealer>();
        if (dd != null)
        {
            dd.damage = powered ? baseDamage + 2 : baseDamage;
        }

        // powered の見た目演出（必要なら）
        if (powered)
            bullet.transform.localScale *= 4f;

        Destroy(bullet, 3.0f);
    }
}