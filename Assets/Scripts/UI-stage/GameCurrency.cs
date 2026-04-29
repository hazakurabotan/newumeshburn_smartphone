using System;
using UnityEngine;

public class GameCurrency : MonoBehaviour
{
    public static GameCurrency Instance { get; private set; }

    [SerializeField] private int startCoins = 0;

    public int Coins { get; private set; }

    public event Action<int> OnCoinsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Coins = Mathf.Max(0, startCoins);
        OnCoinsChanged?.Invoke(Coins);
    }

    public static GameCurrency EnsureInstance()
    {
        if (Instance != null) return Instance;

        GameObject go = new GameObject("GameCurrency");
        return go.AddComponent<GameCurrency>();
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;

        Coins += amount;
        OnCoinsChanged?.Invoke(Coins);
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (Coins < amount) return false;

        Coins -= amount;
        OnCoinsChanged?.Invoke(Coins);
        return true;
    }

    public void SetCoins(int value)
    {
        Coins = Mathf.Max(0, value);
        OnCoinsChanged?.Invoke(Coins);
    }
}