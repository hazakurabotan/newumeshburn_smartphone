using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JuiceUseManager : MonoBehaviour
{
    public static JuiceUseManager Instance { get; private set; }

    public enum UnlockType
    {
        None,
        Dash,
        UpPunch,
        Slide,
        DoubleJump,
        RocketPunch
    }

    [Serializable]
    public class UnlockEntry
    {
        public string juiceId;
        public string unlockDisplayName;
        public UnlockType unlockType = UnlockType.None;
        public int healAmount = 20;
    }

    [Serializable]
    public class UseResult
    {
        public bool success;
        public bool consumed;
        public bool firstUse;
        public string title;
        public string message;
        public string juiceId;
        public string juiceDisplayName;
        public UnlockType unlockType;
        public int healAmount;
        public int healedAmount;
    }

    [Header("References")]
    [SerializeField] private JuiceInventory juiceInventory;

    [Header("Base Can Direct Use")]
    [SerializeField] private int baseJuiceHealAmount = 20;

    [Header("Unlock Table (Mixed Juice Only)")]
    [SerializeField] private List<UnlockEntry> unlockEntries = new List<UnlockEntry>();

    [Header("Used Once Flags (Debug)")]
    [SerializeField] private List<string> usedJuiceIds = new List<string>();

    [Header("Save")]
    [SerializeField] private bool saveUnlocksToPlayerPrefs = true;
    [SerializeField] private string saveKey = "GirlsGear_UsedJuices";

    public event Action OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (juiceInventory == null)
            juiceInventory = JuiceInventory.Instance;

        EnsureUnlockEntries();

        if (saveUnlocksToPlayerPrefs)
            LoadUsedFlags();

        ApplyUnlocksToCurrentMawaru();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Reset()
    {
        CreateDefaultUnlockEntries();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyUnlocksToCurrentMawaru();
        OnStateChanged?.Invoke();
    }

    private void EnsureUnlockEntries()
    {
        if (unlockEntries == null || unlockEntries.Count != 15)
        {
            CreateDefaultUnlockEntries();
        }
    }

    private UnlockEntry MakeEntry(string juiceId, string unlockDisplayName, UnlockType unlockType, int healAmount)
    {
        return new UnlockEntry
        {
            juiceId = juiceId,
            unlockDisplayName = unlockDisplayName,
            unlockType = unlockType,
            healAmount = healAmount
        };
    }

    private void CreateDefaultUnlockEntries()
    {
        unlockEntries = new List<UnlockEntry>
        {
            MakeEntry("mix_red_red",        "ダッシュ",       UnlockType.Dash,        20),
            MakeEntry("mix_red_blue",       "ダッシュ",       UnlockType.Dash,        20),
            MakeEntry("mix_red_green",      "上パンチ",       UnlockType.UpPunch,     20),
            MakeEntry("mix_red_orange",     "スライド",       UnlockType.Slide,       20),
            MakeEntry("mix_red_purple",     "二段ジャンプ",   UnlockType.DoubleJump,  20),

            MakeEntry("mix_blue_blue",      "上パンチ",       UnlockType.UpPunch,     20),
            MakeEntry("mix_blue_green",     "ロケットパンチ", UnlockType.RocketPunch, 20),
            MakeEntry("mix_blue_orange",    "ダッシュ",       UnlockType.Dash,        20),
            MakeEntry("mix_blue_purple",    "上パンチ",       UnlockType.UpPunch,     20),

            MakeEntry("mix_green_green",    "スライド",       UnlockType.Slide,       20),
            MakeEntry("mix_green_orange",   "スライド",       UnlockType.Slide,       20),
            MakeEntry("mix_green_purple",   "二段ジャンプ",   UnlockType.DoubleJump,  20),

            MakeEntry("mix_orange_orange",  "二段ジャンプ",   UnlockType.DoubleJump,  20),
            MakeEntry("mix_orange_purple",  "ロケットパンチ", UnlockType.RocketPunch, 20),

            MakeEntry("mix_purple_purple",  "ロケットパンチ", UnlockType.RocketPunch, 20),
        };
    }

    public bool HasUsedBefore(string juiceId)
    {
        if (string.IsNullOrWhiteSpace(juiceId))
            return false;

        for (int i = 0; i < usedJuiceIds.Count; i++)
        {
            if (string.Equals(usedJuiceIds[i], juiceId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public UnlockEntry GetUnlockEntry(string juiceId)
    {
        if (string.IsNullOrWhiteSpace(juiceId))
            return null;

        for (int i = 0; i < unlockEntries.Count; i++)
        {
            UnlockEntry entry = unlockEntries[i];
            if (entry == null) continue;

            if (string.Equals(entry.juiceId, juiceId, StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        return null;
    }

    public bool IsBaseJuice(string juiceId)
    {
        JuiceInventory inventory = ResolveInventory();
        if (inventory == null)
            return false;

        return inventory.IsBaseJuiceId(juiceId);
    }

    public string BuildQuickUseLabel(JuiceInventory.JuiceDefinition definition)
    {
        if (definition == null)
            return string.Empty;

        if (IsBaseJuice(definition.id))
        {
            return $"{definition.displayName}  HP{Mathf.Max(0, baseJuiceHealAmount)}";
        }

        UnlockEntry entry = GetUnlockEntry(definition.id);
        if (entry == null)
        {
            return definition.displayName;
        }

        if (HasUsedBefore(definition.id))
        {
            return $"{definition.displayName}  HP{Mathf.Max(0, entry.healAmount)}";
        }

        return $"{definition.displayName}  初回:{entry.unlockDisplayName}";
    }

    public string BuildUnlockInventoryLabel(JuiceInventory.JuiceDefinition definition)
    {
        if (definition == null)
            return string.Empty;

        if (IsBaseJuice(definition.id))
        {
            return $"{definition.displayName}（HP{Mathf.Max(0, baseJuiceHealAmount)}回復）";
        }

        UnlockEntry entry = GetUnlockEntry(definition.id);
        if (entry == null)
            return definition.displayName;

        if (HasUsedBefore(definition.id))
            return $"{definition.displayName}（HP{Mathf.Max(0, entry.healAmount)}回復）";

        return $"{definition.displayName}（初回: {entry.unlockDisplayName}解禁）";
    }

    public string BuildRecoveryInventoryLabel(JuiceInventory.JuiceDefinition definition)
    {
        if (definition == null)
            return string.Empty;

        if (IsBaseJuice(definition.id))
        {
            return $"{definition.displayName}（HP{Mathf.Max(0, baseJuiceHealAmount)}回復）";
        }

        UnlockEntry entry = GetUnlockEntry(definition.id);
        int heal = entry != null ? Mathf.Max(0, entry.healAmount) : 0;
        return $"{definition.displayName}（HP{heal}回復）";
    }

    public bool TryUseJuiceAt(int ownedSlotIndex, out UseResult result)
    {
        result = new UseResult
        {
            success = false,
            consumed = false,
            firstUse = false,
            title = "使えない",
            message = "ジュースを使用できません。",
            juiceId = string.Empty,
            juiceDisplayName = string.Empty,
            unlockType = UnlockType.None,
            healAmount = 0,
            healedAmount = 0
        };

        JuiceInventory inventory = ResolveInventory();
        if (inventory == null)
        {
            result.message = "JuiceInventory が見つかりません。";
            return false;
        }

        JuiceInventory.JuiceDefinition definition = inventory.GetOwnedDefinitionAt(ownedSlotIndex);
        if (definition == null)
        {
            result.message = "そのスロットにジュースがありません。";
            return false;
        }

        result.juiceId = definition.id;
        result.juiceDisplayName = definition.displayName;

        if (IsBaseJuice(definition.id))
        {
            return TryConsumeAsRecovery(
                inventory,
                ownedSlotIndex,
                definition,
                Mathf.Max(0, baseJuiceHealAmount),
                out result
            );
        }

        UnlockEntry entry = GetUnlockEntry(definition.id);
        if (entry == null)
        {
            // ミックス缶だけど設定がない場合は回復扱いに逃がす
            return TryConsumeAsRecovery(
                inventory,
                ownedSlotIndex,
                definition,
                Mathf.Max(0, baseJuiceHealAmount),
                out result
            );
        }

        result.unlockType = entry.unlockType;
        result.healAmount = Mathf.Max(0, entry.healAmount);

        bool firstUse = !HasUsedBefore(definition.id);
        result.firstUse = firstUse;

        if (firstUse)
        {
            bool unlockAlreadyActive = entry.unlockType != UnlockType.None && IsUnlockActive(entry.unlockType);

            if (!inventory.TryRemoveAt(ownedSlotIndex))
            {
                result.message = "ジュースの消費に失敗しました。";
                return false;
            }

            MarkUsed(definition.id);

            if (entry.unlockType != UnlockType.None && !unlockAlreadyActive)
            {
                ApplyUnlock(entry.unlockType, true);

                result.success = true;
                result.consumed = true;
                result.title = "新しいモーション解禁！";
                result.message = $"{definition.displayName} を飲んで「{entry.unlockDisplayName}」を解禁した！";
            }
            else
            {
                result.success = true;
                result.consumed = true;
                result.title = "登録した！";
                result.message = $"{definition.displayName} を初回使用した。次回から HP {entry.healAmount} 回復として使える。";
            }

            SaveUsedFlags();
            ApplyUnlocksToCurrentMawaru();
            OnStateChanged?.Invoke();
            return true;
        }

        return TryConsumeAsRecovery(
            inventory,
            ownedSlotIndex,
            definition,
            Mathf.Max(0, entry.healAmount),
            out result
        );
    }

    private bool TryConsumeAsRecovery(
        JuiceInventory inventory,
        int ownedSlotIndex,
        JuiceInventory.JuiceDefinition definition,
        int healAmount,
        out UseResult result)
    {
        result = new UseResult
        {
            success = false,
            consumed = false,
            firstUse = false,
            title = "回復できない",
            message = "回復対象が見つかりません。",
            juiceId = definition != null ? definition.id : string.Empty,
            juiceDisplayName = definition != null ? definition.displayName : string.Empty,
            unlockType = UnlockType.None,
            healAmount = healAmount,
            healedAmount = 0
        };

        MawaruController mawaru = FindCurrentMawaru();
        if (mawaru == null)
        {
            result.message = "MawaruController が見つからないため、回復できません。";
            return false;
        }

        if (!inventory.TryRemoveAt(ownedSlotIndex))
        {
            result.message = "ジュースの消費に失敗しました。";
            return false;
        }

        int healed = mawaru.RecoverHP(healAmount);

        result.success = true;
        result.consumed = true;
        result.healedAmount = healed;
        result.title = "回復した！";
        result.message = healed > 0
            ? $"{definition.displayName} を飲んで HP が {healed} 回復した！"
            : $"{definition.displayName} を飲んだけど、HP はこれ以上回復しなかった。";

        OnStateChanged?.Invoke();
        return true;
    }

    public bool TryUseFirstOwnedJuiceById(string juiceId, out UseResult result)
    {
        result = null;

        JuiceInventory inventory = ResolveInventory();
        if (inventory == null)
            return false;

        for (int i = 0; i < inventory.Count; i++)
        {
            JuiceInventory.JuiceDefinition definition = inventory.GetOwnedDefinitionAt(i);
            if (definition == null) continue;

            if (string.Equals(definition.id, juiceId, StringComparison.OrdinalIgnoreCase))
                return TryUseJuiceAt(i, out result);
        }

        return false;
    }

    public void ApplyUnlocksToCurrentMawaru()
    {
        MawaruController mawaru = FindCurrentMawaru();
        if (mawaru == null)
            return;

        mawaru.ApplyAllJuiceUnlocks(
            IsUnlockActive(UnlockType.Dash),
            IsUnlockActive(UnlockType.UpPunch),
            IsUnlockActive(UnlockType.Slide),
            IsUnlockActive(UnlockType.DoubleJump),
            IsUnlockActive(UnlockType.RocketPunch)
        );
    }

    public bool IsUnlockActive(UnlockType unlockType)
    {
        if (unlockType == UnlockType.None)
            return false;

        for (int i = 0; i < unlockEntries.Count; i++)
        {
            UnlockEntry entry = unlockEntries[i];
            if (entry == null) continue;
            if (entry.unlockType != unlockType) continue;

            if (HasUsedBefore(entry.juiceId))
                return true;
        }

        return false;
    }

    private void ApplyUnlock(UnlockType unlockType, bool unlocked)
    {
        MawaruController mawaru = FindCurrentMawaru();
        if (mawaru == null)
            return;

        switch (unlockType)
        {
            case UnlockType.Dash:
                mawaru.SetDashUnlocked(unlocked);
                break;

            case UnlockType.UpPunch:
                mawaru.SetUpPunchUnlocked(unlocked);
                break;

            case UnlockType.Slide:
                mawaru.SetSlideUnlocked(unlocked);
                break;

            case UnlockType.DoubleJump:
                mawaru.SetDoubleJumpUnlocked(unlocked);
                break;

            case UnlockType.RocketPunch:
                mawaru.SetRocketPunchUnlocked(unlocked);
                break;
        }
    }

    private JuiceInventory ResolveInventory()
    {
        if (juiceInventory == null)
            juiceInventory = JuiceInventory.Instance;

        return juiceInventory;
    }

    private MawaruController FindCurrentMawaru()
    {
        return FindObjectOfType<MawaruController>(true);
    }

    private void MarkUsed(string juiceId)
    {
        if (string.IsNullOrWhiteSpace(juiceId))
            return;

        if (HasUsedBefore(juiceId))
            return;

        usedJuiceIds.Add(juiceId);
    }

    private void SaveUsedFlags()
    {
        if (!saveUnlocksToPlayerPrefs)
            return;

        string joined = string.Join("|", usedJuiceIds.ToArray());
        PlayerPrefs.SetString(saveKey, joined);
        PlayerPrefs.Save();
    }

    private void LoadUsedFlags()
    {
        usedJuiceIds.Clear();

        if (!saveUnlocksToPlayerPrefs)
            return;

        string joined = PlayerPrefs.GetString(saveKey, string.Empty);
        if (string.IsNullOrWhiteSpace(joined))
            return;

        string[] parts = joined.Split('|');
        for (int i = 0; i < parts.Length; i++)
        {
            string value = parts[i];
            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (!HasUsedBefore(value))
                usedJuiceIds.Add(value);
        }
    }
}