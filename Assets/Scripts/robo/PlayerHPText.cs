using UnityEngine;
using TMPro;

public class PlayerHPText : MonoBehaviour
{
    public PlayerHP playerHP;
    public TMP_Text hpText;

    private void Awake()
    {
        if (hpText == null)
        {
            hpText = GetComponent<TMP_Text>();
        }

        if (playerHP == null)
        {
            GameObject playerObj = GameObject.FindWithTag("PlayerCore");

            if (playerObj != null)
            {
                playerHP =
                    playerObj.GetComponent<PlayerHP>() ??
                    playerObj.GetComponentInChildren<PlayerHP>() ??
                    playerObj.GetComponentInParent<PlayerHP>();
            }
        }

        if (playerHP == null)
        {
            playerHP = FindObjectOfType<PlayerHP>();
        }
    }

    private void OnEnable()
    {
        if (playerHP != null)
        {
            playerHP.OnHPChanged += UpdateText;
            UpdateText(playerHP.currentHP, playerHP.maxHP);
        }
        else
        {
            Debug.LogWarning("[PlayerHPText] PlayerHP Ç™å©Ç¬Ç©ÇËÇ‹ÇπÇÒÅBPlayer HP Ç… PlayerCore Çì¸ÇÍÇƒÇ≠ÇæÇ≥Ç¢ÅB");
        }
    }

    private void OnDisable()
    {
        if (playerHP != null)
        {
            playerHP.OnHPChanged -= UpdateText;
        }
    }

    private void Start()
    {
        if (playerHP != null)
        {
            UpdateText(playerHP.currentHP, playerHP.maxHP);
        }
    }

    private void UpdateText(int current, int max)
    {
        if (hpText != null)
        {
            hpText.text = current + "/" + max;
        }
    }
}