using System;
using UnityEngine;

public class BeamAmmo : MonoBehaviour
{
    [Header("Ammo")]
    public int maxAmmo = 10;
    public int currentAmmo = 10;

    [Tooltip("補充間隔（秒）。2なら2秒ごとに+1")]
    public float regenInterval = 2f;

    [Header("Simultaneous Shots")]
    [Tooltip("同時に画面に存在できる弾の最大数（連打上限）")]
    public int maxSimultaneousShots = 5;

    public event Action<int, int> OnAmmoChanged; // (cur, max)

    int activeShots = 0;
    float regenTimer = 0f;

    void Awake()
    {
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    void Update()
    {
        if (currentAmmo < maxAmmo)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= regenInterval)
            {
                regenTimer -= regenInterval;
                currentAmmo = Mathf.Min(maxAmmo, currentAmmo + 1);
                OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
            }
        }
        else
        {
            regenTimer = 0f;
        }
    }

    public bool CanShoot()
    {
        if (currentAmmo <= 0) return false;
        if (activeShots >= maxSimultaneousShots) return false;
        return true;
    }

    public bool TryConsume()
    {
        if (!CanShoot()) return false;

        currentAmmo--;
        activeShots++;
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        return true;
    }

    public void RegisterBullet(GameObject bullet)
    {
        if (!bullet) return;

        var t = bullet.GetComponent<BulletLifeTracker>();
        if (!t) t = bullet.AddComponent<BulletLifeTracker>();
        t.Bind(this);
    }

    void OnBulletDestroyed()
    {
        activeShots = Mathf.Max(0, activeShots - 1);
        // activeShots表示は要らないので、弾数UIだけ更新（必要ならここで別イベント追加も可）
    }

    // 弾が消えたら同時数を戻す
    public class BulletLifeTracker : MonoBehaviour
    {
        BeamAmmo owner;
        public void Bind(BeamAmmo a) => owner = a;

        void OnDestroy()
        {
            if (owner) owner.OnBulletDestroyed();
        }
    }
}