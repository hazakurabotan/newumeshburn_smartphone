// BuildManager.cs
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance;

    [Header("UI")]
    public GameObject buildPanel;          // Canvas子：ビルド用パネル
    public TextMeshProUGUI spText;         // SP表示
    public GameObject firstSelect;         // 開いた直後に選択したいボタン

    [Header("Points")]
    public int skillPoints = 0;            // 所持SP

    // ─ スキルレベル（必要に応じて増やしてOK） ─
    int dmgLv = 0;
    int shotsLv = 0;
    int hpLv = 0;
    int speedLv = 0;

    PlayerInput _pi;

    void Awake()
    {
        if (Instance == null) Instance = this; else { Destroy(gameObject); return; }
        _pi = FindObjectOfType<PlayerInput>(true);
        if (buildPanel) buildPanel.SetActive(false);
        RefreshUI();
    }

    // ===== SP付与（敵撃破/ドロップで呼ぶ） =====
    public void AddPoints(int amount)
    {
        if (amount <= 0) return;
        skillPoints += amount;
        RefreshUI();
    }

    // ===== 開閉 =====
    public void Toggle()
    {
        if (buildPanel == null) return;
        if (buildPanel.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        if (buildPanel == null) return;
        buildPanel.SetActive(true);

        // 一時停止：プレイヤー操作停止、UIは有効
        Time.timeScale = 0f;
        if (_pi != null)
        {
            _pi.actions.FindActionMap("Player")?.Disable();
            _pi.actions.FindActionMap("UI")?.Enable();
        }

        // 最初に選択させたいボタン
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (firstSelect && es) es.SetSelectedGameObject(firstSelect);

        RefreshUI();
    }

    public void Close()
    {
        if (buildPanel == null) return;
        buildPanel.SetActive(false);

        Time.timeScale = 1f;
        if (_pi != null)
        {
            _pi.actions.FindActionMap("Player")?.Enable();
            _pi.actions.FindActionMap("UI")?.Enable();
        }
    }

    // ===== ボタン（OnClickで結ぶ） =====
    public void UpgradeDamage()
    {
        int cost = 1 + dmgLv; if (skillPoints < cost) return;
        skillPoints -= cost; dmgLv++;

        var pc = FindObjectOfType<PlayerController>();
        if (pc) pc.bulletDamage = 1 + dmgLv; // 1→2→3…

        RefreshUI();
    }

    public void UpgradeShots()
    {
        int cost = 1 + shotsLv; if (skillPoints < cost) return;
        skillPoints -= cost; shotsLv++;

        var ps = FindObjectOfType<PlayerShoot>();
        if (ps) ps.maxShots = Mathf.Clamp(ps.maxShots + 1, 1, 8);

        RefreshUI();
    }

    public void UpgradeMaxHP()
    {
        int cost = 2 + hpLv; if (skillPoints < cost) return;
        skillPoints -= cost; hpLv++;

        var pc = FindObjectOfType<PlayerController>();
        if (pc)
        {
            pc.maxHP += 1;
            pc.Heal(1);
            pc.UpdateHpUI();
        }
        RefreshUI();
    }

    public void UpgradeMoveSpeed()
    {
        int cost = 1 + speedLv; if (skillPoints < cost) return;
        skillPoints -= cost; speedLv++;

        var pc = FindObjectOfType<PlayerController>();
        if (pc) pc.speed += 0.5f;

        RefreshUI();
    }

    public void OnCloseButton() => Close();

    void RefreshUI()
    {
        if (spText) spText.text = $"SP: {skillPoints}";
    }
}
