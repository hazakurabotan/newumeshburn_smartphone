using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 50;
    public int currentHP;

    // (current, max)
    public System.Action<int, int> OnHPChanged;

    private void Awake()
    {
        if (maxHP <= 0) maxHP = 50;
        currentHP = Mathf.Clamp(currentHP <= 0 ? maxHP : currentHP, 0, maxHP);
        Notify();
    }

    public void SetFull()
    {
        currentHP = maxHP;
        Notify();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        int before = currentHP;
        currentHP = Mathf.Max(0, currentHP - amount);

        if (currentHP != before) Notify();

        if (currentHP <= 0)
        {
            Debug.Log("Player Dead");
            // ここで死亡処理を呼びたいなら呼ぶ（既存のResult遷移スクリプト等）
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        int before = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + amount);

        if (currentHP != before) Notify();
    }

    public void SetHPDirect(int current, int max = -1)
    {
        if (max > 0)
            maxHP = max;

        if (maxHP <= 0)
            maxHP = 1;

        currentHP = Mathf.Clamp(current, 0, maxHP);
        Notify();
    }


    private void Notify()
    {
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    // ★昔の呼び出し互換：これを呼ばれてもUIが更新されるようにする
    public void DamageToPlayer(int damage)
    {
        TakeDamage(damage);
    }

    public bool IsDead => currentHP <= 0;
}