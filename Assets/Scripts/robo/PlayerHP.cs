using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 50;
    public int currentHP;

    // (current, max)
    public System.Action<int, int> OnHPChanged;

    public bool IsDead => currentHP <= 0;
    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;

    private void Awake()
    {
        if (maxHP <= 0) maxHP = 50;

        // Inspector上で currentHP が 0 のままでも、開始時は最大HPにする
        if (currentHP <= 0)
        {
            currentHP = maxHP;
        }
        else
        {
            currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        }

        Notify();
    }

    private void Start()
    {
        // UI側のStart/OnEnable後にも確実に1回通知する
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

        Debug.Log($"[PlayerHP] Damage {amount} : {before} -> {currentHP}");

        Notify();

        if (currentHP <= 0)
        {
            Debug.Log("[PlayerHP] Player Dead");
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        int before = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + amount);

        Debug.Log($"[PlayerHP] Heal {amount} : {before} -> {currentHP}");

        Notify();
    }

    public void SetHPDirect(int current, int max = -1)
    {
        if (max > 0)
        {
            maxHP = max;
        }

        if (maxHP <= 0)
        {
            maxHP = 1;
        }

        currentHP = Mathf.Clamp(current, 0, maxHP);

        Debug.Log($"[PlayerHP] SetHPDirect : {currentHP}/{maxHP}");

        Notify();
    }

    public void DamageToPlayer(int damage)
    {
        TakeDamage(damage);
    }

    private void Notify()
    {
        OnHPChanged?.Invoke(currentHP, maxHP);
    }
}