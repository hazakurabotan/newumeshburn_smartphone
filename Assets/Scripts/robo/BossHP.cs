using UnityEngine;
using System;

public class BossHP : MonoBehaviour
{
    [Header("HP設定")]
    public int maxHP = 50;
    public int currentHP;

    public bool IsDead { get; private set; }

    // HPが変わったときに通知するイベント
    public event Action<int, int> OnHpChanged;
    //          ↑現在HP, 最大HP

    void Awake()
    {
        currentHP = maxHP;
        NotifyHpChanged();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHP -= damage;
        if (currentHP <= 0)
        {
            currentHP = 0;
            IsDead = true;
            Debug.Log("Boss Dead");
        }
        else
        {
            Debug.Log("Boss Damage! HP = " + currentHP);
        }

        NotifyHpChanged();
    }

    void NotifyHpChanged()
    {
        OnHpChanged?.Invoke(currentHP, maxHP);
    }
}
