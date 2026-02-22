using TMPro;
using UnityEngine;

public class PlayerHPText : MonoBehaviour
{
    [SerializeField] private PlayerHP playerHP;
    [SerializeField] private TextMeshProUGUI hpText;

    private void Awake()
    {
        if (!hpText) hpText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (!playerHP) return;

        playerHP.OnHPChanged += UpdateUI;
        UpdateUI(playerHP.currentHP, playerHP.maxHP);
    }

    private void OnDisable()
    {
        if (!playerHP) return;
        playerHP.OnHPChanged -= UpdateUI;
    }

    private void UpdateUI(int current, int max)
    {
        if (!hpText) return;
        hpText.text = $"HP {current}/{max}";
    }
}