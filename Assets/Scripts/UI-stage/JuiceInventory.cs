using System;
using System.Collections.Generic;
using UnityEngine;

public class JuiceInventory : MonoBehaviour
{
    public static JuiceInventory Instance { get; private set; }

    [Serializable]
    public class JuiceDefinition
    {
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite icon;

        [Header("Flags")]
        public bool canAppearFromVendingMachine = false;
        public bool canUseAsMixIngredient = false;
        public bool canDrinkDirectly = false;
    }

    private struct DefaultDefinitionSpec
    {
        public string id;
        public string displayName;
        public string description;
        public bool canAppearFromVendingMachine;
        public bool canUseAsMixIngredient;
        public bool canDrinkDirectly;

        public DefaultDefinitionSpec(
            string id,
            string displayName,
            string description,
            bool canAppearFromVendingMachine,
            bool canUseAsMixIngredient,
            bool canDrinkDirectly)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.canAppearFromVendingMachine = canAppearFromVendingMachine;
            this.canUseAsMixIngredient = canUseAsMixIngredient;
            this.canDrinkDirectly = canDrinkDirectly;
        }
    }

    [Header("Inventory")]
    [SerializeField] private int maxSlots = 5;

    [Header("Juice Definitions (20 kinds)")]
    [SerializeField] private JuiceDefinition[] juiceDefinitions = new JuiceDefinition[20];

    [Header("Owned Juice Indices (Debug)")]
    [SerializeField] private List<int> ownedJuiceIndices = new List<int>();

    public int MaxSlots => maxSlots;
    public int Count => ownedJuiceIndices != null ? ownedJuiceIndices.Count : 0;
    public bool IsFull => Count >= maxSlots;
    public IReadOnlyList<int> OwnedJuiceIndices => ownedJuiceIndices;
    public JuiceDefinition[] Definitions => juiceDefinitions;

    public event Action OnInventoryChanged;

    private const int RequiredDefinitionCount = 20;

    private static readonly string[] BaseJuiceIds =
    {
        "red",
        "blue",
        "green",
        "orange",
        "purple"
    };

    private static readonly DefaultDefinitionSpec[] DefaultSpecs =
    {
        new DefaultDefinitionSpec("red", "レッドソーダ", "赤い定番ソーダ。勢いのある味わい。", true,  true,  true),
        new DefaultDefinitionSpec("blue", "スターソーダ", "きらめく青いスターソーダ。クールな炭酸感。", true,  true,  true),
        new DefaultDefinitionSpec("green", "ライムソーダ", "さわやかなライムのソーダ。軽やかな刺激。", true,  true,  true),
        new DefaultDefinitionSpec("orange", "オレンジソーダ", "オレンジの風味が広がるジューシーソーダ。", true,  true,  true),
        new DefaultDefinitionSpec("purple", "グレープソーダ", "ぶどうの甘みを感じる濃いめのソーダ。", true,  true,  true),

        new DefaultDefinitionSpec("mix_red_red",       "レッドレッドミックス",         "ミックスして完成した缶ジュース。", false, false, true),
        new DefaultDefinitionSpec("mix_red_blue",      "アップルサイダー",             "ミックスして完成した缶ジュース。", false, false, true),
        new DefaultDefinitionSpec("mix_red_green",     "レッドライムミックス",         "ミックスして完成した缶ジュース。", false, false, true),
        new DefaultDefinitionSpec("mix_red_orange",    "レッドオレンジミックス",       "ミックスして完成した缶ジュース。", false, false, true),
        new DefaultDefinitionSpec("mix_red_purple",    "レッドグレープミックス",       "ミックスして完成した缶ジュース。", false, false, true),

        new DefaultDefinitionSpec("mix_blue_blue",     "ブルーブルーミックス",         "ミックスして完成した缶ジュース。", false, false, true),
        new DefaultDefinitionSpec("mix_blue_green",    "ブルーライムミックス",         "ミックスして完成した缶ジュース。", false, false, true),
        new DefaultDefinitionSpec("mix_blue_orange",   "ブルーオレンジミックス",       "ミックスして完成した缶ジュース。", false, false, true),
        new DefaultDefinitionSpec("mix_blue_purple",   "ブルーグレープミックス",       "ミックスして完成した缶ジュース。", false, false, true),

        new DefaultDefinitionSpec("mix_green_green",   "ライムライムミックス",         "ミックスして完成した缶ジュース。", false, false, true),
        new DefaultDefinitionSpec("mix_green_orange",  "ライムオレンジミックス",       "ミックスして完成した缶ジュース。", false, false, true),
        new DefaultDefinitionSpec("mix_green_purple",  "ライムグレープミックス",       "ミックスして完成した缶ジュース。", false, false, true),

        new DefaultDefinitionSpec("mix_orange_orange", "オレンジオレンジミックス",     "ミックスして完成した缶ジュース。", false, false, true),
        new DefaultDefinitionSpec("mix_orange_purple", "オレンジグレープミックス",     "ミックスして完成した缶ジュース。", false, false, true),

        new DefaultDefinitionSpec("mix_purple_purple", "グレープグレープミックス",     "ミックスして完成した缶ジュース。", false, false, true)
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureDefinitionsPreservingData();
        PatchKnownDefinitions();
        ApplyMixedIconFallbacks();
        RemoveInvalidOwnedIndices();
    }

    private void Reset()
    {
        CreateDefaultDefinitions();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureDefinitionsPreservingData();
        PatchKnownDefinitions();
        ApplyMixedIconFallbacks();
        RemoveInvalidOwnedIndices();
    }
#endif

    private void EnsureDefinitionsPreservingData()
    {
        if (juiceDefinitions == null)
        {
            juiceDefinitions = new JuiceDefinition[RequiredDefinitionCount];
        }
        else if (juiceDefinitions.Length < RequiredDefinitionCount)
        {
            ResizeDefinitionsPreserve(RequiredDefinitionCount);
        }

        for (int i = 0; i < DefaultSpecs.Length; i++)
        {
            ApplyDefaultSpecIfNeeded(i, DefaultSpecs[i]);
        }
    }

    private void ResizeDefinitionsPreserve(int targetLength)
    {
        JuiceDefinition[] oldArray = juiceDefinitions;
        JuiceDefinition[] newArray = new JuiceDefinition[targetLength];

        if (oldArray != null)
        {
            int copyCount = Mathf.Min(oldArray.Length, newArray.Length);
            for (int i = 0; i < copyCount; i++)
            {
                newArray[i] = oldArray[i];
            }
        }

        juiceDefinitions = newArray;
    }

    private void ApplyDefaultSpecIfNeeded(int index, DefaultDefinitionSpec spec)
    {
        if (index < 0)
            return;

        if (juiceDefinitions == null || index >= juiceDefinitions.Length)
            return;

        if (juiceDefinitions[index] == null)
        {
            juiceDefinitions[index] = new JuiceDefinition();
        }

        JuiceDefinition definition = juiceDefinitions[index];

        if (string.IsNullOrWhiteSpace(definition.id))
            definition.id = spec.id;

        if (string.IsNullOrWhiteSpace(definition.displayName))
            definition.displayName = spec.displayName;

        if (string.IsNullOrWhiteSpace(definition.description))
            definition.description = spec.description;

        if (string.Equals(definition.id, spec.id, StringComparison.OrdinalIgnoreCase))
        {
            definition.canAppearFromVendingMachine = spec.canAppearFromVendingMachine;
            definition.canUseAsMixIngredient = spec.canUseAsMixIngredient;
            definition.canDrinkDirectly = spec.canDrinkDirectly;
        }
    }

    private void CreateDefaultDefinitions()
    {
        juiceDefinitions = new JuiceDefinition[RequiredDefinitionCount];

        for (int i = 0; i < DefaultSpecs.Length; i++)
        {
            DefaultDefinitionSpec spec = DefaultSpecs[i];
            juiceDefinitions[i] = new JuiceDefinition
            {
                id = spec.id,
                displayName = spec.displayName,
                description = spec.description,
                icon = null,
                canAppearFromVendingMachine = spec.canAppearFromVendingMachine,
                canUseAsMixIngredient = spec.canUseAsMixIngredient,
                canDrinkDirectly = spec.canDrinkDirectly
            };
        }
    }

    private void PatchKnownDefinitions()
    {
        if (juiceDefinitions == null)
            return;

        for (int i = 0; i < juiceDefinitions.Length; i++)
        {
            JuiceDefinition definition = juiceDefinitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                continue;

            bool isBase = IsBaseJuiceId(definition.id);
            bool isMix = definition.id.StartsWith("mix_", StringComparison.OrdinalIgnoreCase);

            if (isBase)
            {
                definition.canAppearFromVendingMachine = true;
                definition.canUseAsMixIngredient = true;
                definition.canDrinkDirectly = true;
            }
            else if (isMix)
            {
                definition.canAppearFromVendingMachine = false;
                definition.canUseAsMixIngredient = false;
                definition.canDrinkDirectly = true;
            }
        }
    }

    private void ApplyMixedIconFallbacks()
    {
        if (juiceDefinitions == null)
            return;

        for (int i = 0; i < juiceDefinitions.Length; i++)
        {
            JuiceDefinition definition = juiceDefinitions[i];
            if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                continue;

            if (definition.icon != null)
                continue;

            if (!definition.id.StartsWith("mix_", StringComparison.OrdinalIgnoreCase))
                continue;

            definition.icon = ResolveFallbackIconForJuiceId(definition.id);
        }
    }

    private void RemoveInvalidOwnedIndices()
    {
        if (ownedJuiceIndices == null)
            ownedJuiceIndices = new List<int>();

        for (int i = ownedJuiceIndices.Count - 1; i >= 0; i--)
        {
            if (!IsValidDefinitionIndex(ownedJuiceIndices[i]))
            {
                ownedJuiceIndices.RemoveAt(i);
            }
        }
    }

    public bool IsBaseJuiceId(string juiceId)
    {
        if (string.IsNullOrWhiteSpace(juiceId))
            return false;

        for (int i = 0; i < BaseJuiceIds.Length; i++)
        {
            if (string.Equals(BaseJuiceIds[i], juiceId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public Sprite ResolveFallbackIconForJuiceId(string juiceId)
    {
        if (string.IsNullOrWhiteSpace(juiceId))
            return null;

        JuiceDefinition direct = GetDefinitionById(juiceId);
        if (direct != null && direct.icon != null)
            return direct.icon;

        if (juiceId.StartsWith("mix_", StringComparison.OrdinalIgnoreCase))
        {
            string pair = juiceId.Substring(4);
            string[] parts = pair.Split('_');

            if (parts.Length >= 1)
            {
                JuiceDefinition a = GetDefinitionById(parts[0]);
                if (a != null && a.icon != null)
                    return a.icon;
            }

            if (parts.Length >= 2)
            {
                JuiceDefinition b = GetDefinitionById(parts[1]);
                if (b != null && b.icon != null)
                    return b.icon;
            }
        }

        return null;
    }

    public bool TryAddRandom(out JuiceDefinition obtainedDefinition, out int obtainedDefinitionIndex)
    {
        obtainedDefinition = null;
        obtainedDefinitionIndex = -1;

        if (IsFull)
            return false;

        if (juiceDefinitions == null || juiceDefinitions.Length == 0)
            return false;

        List<int> candidateIndices = new List<int>();

        for (int i = 0; i < juiceDefinitions.Length; i++)
        {
            JuiceDefinition definition = juiceDefinitions[i];
            if (definition == null)
                continue;

            if (!definition.canAppearFromVendingMachine)
                continue;

            candidateIndices.Add(i);
        }

        if (candidateIndices.Count <= 0)
            return false;

        int chosenDefinitionIndex = candidateIndices[UnityEngine.Random.Range(0, candidateIndices.Count)];
        ownedJuiceIndices.Add(chosenDefinitionIndex);

        obtainedDefinition = juiceDefinitions[chosenDefinitionIndex];
        obtainedDefinitionIndex = chosenDefinitionIndex;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryAddDefinitionIndex(int definitionIndex, out JuiceDefinition obtainedDefinition)
    {
        obtainedDefinition = null;

        if (IsFull)
            return false;

        if (!IsValidDefinitionIndex(definitionIndex))
            return false;

        ownedJuiceIndices.Add(definitionIndex);
        obtainedDefinition = juiceDefinitions[definitionIndex];

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryAddByDefinitionIndex(int definitionIndex)
    {
        return TryAddDefinitionIndex(definitionIndex, out _);
    }

    public bool TryAddByDefinitionIndex(int definitionIndex, out JuiceDefinition obtainedDefinition)
    {
        return TryAddDefinitionIndex(definitionIndex, out obtainedDefinition);
    }

    public bool TryAddByDefinitionIndex(int definitionIndex, out JuiceDefinition obtainedDefinition, out int ownedSlotIndex)
    {
        obtainedDefinition = null;
        ownedSlotIndex = -1;

        if (IsFull)
            return false;

        if (!IsValidDefinitionIndex(definitionIndex))
            return false;

        ownedJuiceIndices.Add(definitionIndex);
        obtainedDefinition = juiceDefinitions[definitionIndex];
        ownedSlotIndex = ownedJuiceIndices.Count - 1;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryAddById(string juiceId, out JuiceDefinition obtainedDefinition, out int obtainedDefinitionIndex)
    {
        obtainedDefinition = null;
        obtainedDefinitionIndex = -1;

        if (string.IsNullOrWhiteSpace(juiceId))
            return false;

        int definitionIndex = GetDefinitionIndexById(juiceId);
        if (definitionIndex < 0)
            return false;

        if (!TryAddDefinitionIndex(definitionIndex, out obtainedDefinition))
            return false;

        obtainedDefinitionIndex = definitionIndex;
        return true;
    }

    public bool TryRemoveAt(int ownedSlotIndex)
    {
        if (ownedSlotIndex < 0 || ownedSlotIndex >= ownedJuiceIndices.Count)
            return false;

        ownedJuiceIndices.RemoveAt(ownedSlotIndex);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryRemoveFirstByDefinitionIndex(int definitionIndex, out int removedOwnedSlotIndex)
    {
        removedOwnedSlotIndex = -1;

        for (int i = 0; i < ownedJuiceIndices.Count; i++)
        {
            if (ownedJuiceIndices[i] == definitionIndex)
            {
                ownedJuiceIndices.RemoveAt(i);
                removedOwnedSlotIndex = i;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }

    public bool TryRemoveFirstById(string juiceId, out int removedOwnedSlotIndex)
    {
        removedOwnedSlotIndex = -1;

        if (string.IsNullOrWhiteSpace(juiceId))
            return false;

        int definitionIndex = GetDefinitionIndexById(juiceId);
        if (definitionIndex < 0)
            return false;

        return TryRemoveFirstByDefinitionIndex(definitionIndex, out removedOwnedSlotIndex);
    }

    public void ClearAll()
    {
        if (ownedJuiceIndices == null)
            ownedJuiceIndices = new List<int>();

        ownedJuiceIndices.Clear();
        OnInventoryChanged?.Invoke();
    }

    public JuiceDefinition GetOwnedDefinitionAt(int ownedSlotIndex)
    {
        if (ownedSlotIndex < 0 || ownedSlotIndex >= ownedJuiceIndices.Count)
            return null;

        int definitionIndex = ownedJuiceIndices[ownedSlotIndex];
        if (!IsValidDefinitionIndex(definitionIndex))
            return null;

        return juiceDefinitions[definitionIndex];
    }

    public int GetOwnedDefinitionIndexAt(int ownedSlotIndex)
    {
        if (ownedSlotIndex < 0 || ownedSlotIndex >= ownedJuiceIndices.Count)
            return -1;

        return ownedJuiceIndices[ownedSlotIndex];
    }

    public Sprite GetDisplayIconForOwnedSlot(int ownedSlotIndex)
    {
        JuiceDefinition definition = GetOwnedDefinitionAt(ownedSlotIndex);
        if (definition == null)
            return null;

        if (definition.icon != null)
            return definition.icon;

        return ResolveFallbackIconForJuiceId(definition.id);
    }

    public string GetDisplayNameForOwnedSlot(int ownedSlotIndex)
    {
        JuiceDefinition definition = GetOwnedDefinitionAt(ownedSlotIndex);
        return definition != null ? definition.displayName : string.Empty;
    }

    public string GetDescriptionForOwnedSlot(int ownedSlotIndex)
    {
        JuiceDefinition definition = GetOwnedDefinitionAt(ownedSlotIndex);
        return definition != null ? definition.description : string.Empty;
    }

    public int GetDefinitionIndexById(string juiceId)
    {
        if (string.IsNullOrWhiteSpace(juiceId) || juiceDefinitions == null)
            return -1;

        for (int i = 0; i < juiceDefinitions.Length; i++)
        {
            JuiceDefinition definition = juiceDefinitions[i];
            if (definition == null)
                continue;

            if (string.Equals(definition.id, juiceId, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public JuiceDefinition GetDefinitionAt(int definitionIndex)
    {
        if (!IsValidDefinitionIndex(definitionIndex))
            return null;

        return juiceDefinitions[definitionIndex];
    }

    public JuiceDefinition GetDefinitionById(string juiceId)
    {
        int definitionIndex = GetDefinitionIndexById(juiceId);
        if (definitionIndex < 0)
            return null;

        return GetDefinitionAt(definitionIndex);
    }

    public bool IsValidDefinitionIndex(int definitionIndex)
    {
        if (juiceDefinitions == null)
            return false;

        if (definitionIndex < 0 || definitionIndex >= juiceDefinitions.Length)
            return false;

        if (juiceDefinitions[definitionIndex] == null)
            return false;

        if (string.IsNullOrWhiteSpace(juiceDefinitions[definitionIndex].id))
            return false;

        return true;
    }

    public List<JuiceDefinition> GetOwnedDefinitionsSnapshot()
    {
        List<JuiceDefinition> result = new List<JuiceDefinition>();

        for (int i = 0; i < ownedJuiceIndices.Count; i++)
        {
            JuiceDefinition definition = GetOwnedDefinitionAt(i);
            if (definition != null)
                result.Add(definition);
        }

        return result;
    }

    public List<string> GetOwnedDisplayNamesSnapshot()
    {
        List<string> result = new List<string>();

        for (int i = 0; i < ownedJuiceIndices.Count; i++)
        {
            JuiceDefinition definition = GetOwnedDefinitionAt(i);
            if (definition != null)
                result.Add(definition.displayName);
        }

        return result;
    }

    public List<int> GetOwnedMixIngredientSlotIndicesSnapshot()
    {
        List<int> result = new List<int>();

        for (int i = 0; i < ownedJuiceIndices.Count; i++)
        {
            JuiceDefinition definition = GetOwnedDefinitionAt(i);
            if (definition == null)
                continue;

            if (!definition.canUseAsMixIngredient)
                continue;

            result.Add(i);
        }

        return result;
    }

    public List<int> GetOwnedDrinkableSlotIndicesSnapshot()
    {
        List<int> result = new List<int>();

        for (int i = 0; i < ownedJuiceIndices.Count; i++)
        {
            JuiceDefinition definition = GetOwnedDefinitionAt(i);
            if (definition == null)
                continue;

            if (!definition.canDrinkDirectly)
                continue;

            result.Add(i);
        }

        return result;
    }
}