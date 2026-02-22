using UnityEngine;
using TMPro;

public class BossHPUI : MonoBehaviour
{
    public BossHP bossHP;
    public TextMeshProUGUI hpText;

    void Awake()
    {
        // Inspectorで設定し忘れていた時の保険
        if (bossHP == null)
            bossHP = FindObjectOfType<BossHP>();
        if (hpText == null)
            hpText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (bossHP != null)
        {
            bossHP.OnHpChanged += HandleHpChanged;
        }
    }

    void OnDisable()
    {
        if (bossHP != null)
        {
            bossHP.OnHpChanged -= HandleHpChanged;
        }
    }

    void Start()
    {
        // 初期表示
        if (bossHP != null)
        {
            HandleHpChanged(bossHP.currentHP, bossHP.maxHP);
        }
    }

    void HandleHpChanged(int current, int max)
    {
        // ここでリアルタイム更新
        if (hpText != null)
        {
            hpText.text = $"BOSS HP {current}/{max}";
        }
    }
}
