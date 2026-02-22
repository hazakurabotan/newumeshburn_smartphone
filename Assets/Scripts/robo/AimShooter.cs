using UnityEngine;

public class AimShooter : MonoBehaviour
{
    [Header("Bullet Settings")]
    public GameObject bulletPrefab;
    public Transform muzzle;
    public Transform cursor;

    public float bulletSpeed = 20f;
    public float fireInterval = 0.1f;

    float fireTimer;
    BeamAmmo ammo;

    void Awake()
    {
        ammo = GetComponent<BeamAmmo>(); // PlayerCoreにBeamAmmoが付いてる前提
    }

    // RoboBattleController から毎フレーム呼ばれる想定
    public void TryShoot()
    {
        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        // 発射条件
        if (bulletPrefab == null || muzzle == null || cursor == null)
        {
            Debug.LogWarning("AimShooter: bulletPrefab / muzzle / cursor が未設定");
            return;
        }

        // ★弾数10/10＆同時5発制限
        if (ammo != null)
        {
            if (!ammo.TryConsume()) return;
        }

        Fire();
        fireTimer = fireInterval;
    }

    void Fire()
    {
        GameObject bulletObj = Instantiate(bulletPrefab, muzzle.position, Quaternion.identity);

        Vector2 dir = (cursor.position - muzzle.position).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        Rigidbody2D rb = bulletObj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = dir * bulletSpeed;

        // ★弾が消えたら同時弾数を戻す
        if (ammo != null) ammo.RegisterBullet(bulletObj);
    }
}