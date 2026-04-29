using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoinCounterUI : MonoBehaviour
{
    [Header("UI")]
    public Image coinIcon;
    public Sprite coinSprite;

    public TMP_Text coinTextTMP;
    public Text coinTextLegacy;

    [Header("Display")]
    public string prefix = "Å~ ";
    public bool showPrefix = true;

    private int lastCoins = int.MinValue;

    private void Awake()
    {
        ApplyIcon();
        Refresh(true);
    }

    private void OnEnable()
    {
        ApplyIcon();
        Refresh(true);
    }

    private void Update()
    {
        Refresh(false);
    }

    private void ApplyIcon()
    {
        if (coinIcon != null && coinSprite != null)
        {
            coinIcon.sprite = coinSprite;
            coinIcon.enabled = true;
        }
    }

    private void Refresh(bool force)
    {
        int coins = 0;

        if (GameCurrency.Instance != null)
        {
            coins = GameCurrency.Instance.Coins;
        }
        else
        {
            GameCurrency.EnsureInstance();
            if (GameCurrency.Instance != null)
                coins = GameCurrency.Instance.Coins;
        }

        if (!force && coins == lastCoins) return;

        lastCoins = coins;

        string text = showPrefix ? prefix + coins.ToString() : coins.ToString();

        if (coinTextTMP != null)
            coinTextTMP.text = text;

        if (coinTextLegacy != null)
            coinTextLegacy.text = text;
    }
}