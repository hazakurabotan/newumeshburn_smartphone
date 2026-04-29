using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemInventoryPageController : MonoBehaviour
{
    public enum ItemEffectType
    {
        None = 0,
        HealHP = 1
    }

    [System.Serializable]
    public class InventoryItemEntry
    {
        [Header("Item Data")]
        public string itemId = "mix_heal_item";
        public string itemName = "回復アイテム";
        [TextArea(2, 4)] public string description = "HPを回復する。";
        public bool usable = true;

        [Header("Effect")]
        public ItemEffectType effectType = ItemEffectType.HealHP;
        public int effectValue = 20;

        [Header("UI")]
        public GameObject rowRoot;
        public TMP_Text nameText;
        public TMP_Text countText;
        public TMP_Text descriptionText;
        public Button useButton;
    }

    [Header("Roots")]
    [SerializeField] private GameObject inventoryPageRoot;
    [SerializeField] private GameObject settingsMainRoot;

    [Header("Buttons")]
    [SerializeField] private Button backButton;

    [Header("Selection")]
    [SerializeField] private GameObject firstSelectedOnOpen;
    [SerializeField] private GameObject firstSelectedOnClose;

    [Header("Message")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private string defaultMessage = "持ち物を確認できます。";
    [SerializeField] private string emptyItemMessage = "そのアイテムは持っていない。";
    [SerializeField] private string cannotUseMessage = "このアイテムはここでは使えない。";
    [SerializeField] private string noTargetMessage = "回復先が設定されていない。";

    [Header("Heal Target")]
    [SerializeField] private GameObject hpTarget;
    [SerializeField]
    private string[] healMethodNames =
    {
        "RecoverHp",
        "RecoverHP",
        "Heal",
        "HealHP",
        "AddHp",
        "AddHP",
        "RestoreHp",
        "RestoreHP"
    };

    [Header("Items")]
    [SerializeField] private InventoryItemEntry[] items;

    private bool isBound;

    private void Awake()
    {
        if (inventoryPageRoot == null)
        {
            inventoryPageRoot = gameObject;
        }

        if (inventoryPageRoot != null)
        {
            inventoryPageRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        BindEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    public void OpenPage()
    {
        if (inventoryPageRoot != null)
        {
            inventoryPageRoot.SetActive(true);
        }

        RefreshAll();

        GameObject selectTarget = firstSelectedOnOpen;
        if (selectTarget == null)
        {
            selectTarget = FindFirstUsableButtonObject();
        }

        if (selectTarget == null && backButton != null)
        {
            selectTarget = backButton.gameObject;
        }

        SetSelected(selectTarget);
    }

    public void ClosePage()
    {
        if (inventoryPageRoot != null)
        {
            inventoryPageRoot.SetActive(false);
        }

        if (settingsMainRoot != null)
        {
            settingsMainRoot.SetActive(true);
        }

        SetSelected(firstSelectedOnClose);
    }

    private void BindEvents()
    {
        if (isBound) return;
        isBound = true;

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ClosePage);
            backButton.onClick.AddListener(ClosePage);
        }

        if (items != null)
        {
            for (int i = 0; i < items.Length; i++)
            {
                int capturedIndex = i;

                if (items[i] != null && items[i].useButton != null)
                {
                    items[i].useButton.onClick.RemoveAllListeners();
                    items[i].useButton.onClick.AddListener(() => OnUseItemPressed(capturedIndex));
                }
            }
        }
    }

    private void UnbindEvents()
    {
        isBound = false;

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ClosePage);
        }

        if (items != null)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null && items[i].useButton != null)
                {
                    items[i].useButton.onClick.RemoveAllListeners();
                }
            }
        }
    }

    public void RefreshAll()
    {
        if (messageText != null)
        {
            messageText.text = defaultMessage;
        }

        if (items == null) return;

        for (int i = 0; i < items.Length; i++)
        {
            RefreshRow(i);
        }
    }

    private void RefreshRow(int index)
    {
        if (items == null) return;
        if (index < 0 || index >= items.Length) return;
        if (items[index] == null) return;

        InventoryItemEntry item = items[index];
        int count = GetItemCount(item.itemId);

        if (item.rowRoot != null)
        {
            item.rowRoot.SetActive(true);
        }

        if (item.nameText != null)
        {
            item.nameText.text = item.itemName;
        }

        if (item.countText != null)
        {
            item.countText.text = "× " + count.ToString();
        }

        if (item.descriptionText != null)
        {
            item.descriptionText.text = item.description;
        }

        if (item.useButton != null)
        {
            item.useButton.interactable = item.usable && count > 0;
        }
    }

    private void OnUseItemPressed(int index)
    {
        if (items == null) return;
        if (index < 0 || index >= items.Length) return;
        if (items[index] == null) return;

        InventoryItemEntry item = items[index];
        int count = GetItemCount(item.itemId);

        if (count <= 0)
        {
            SetMessage(emptyItemMessage);
            RefreshRow(index);
            return;
        }

        if (!item.usable)
        {
            SetMessage(cannotUseMessage);
            RefreshRow(index);
            return;
        }

        bool applied = ApplyItemEffect(item);
        if (!applied)
        {
            RefreshRow(index);
            return;
        }

        SetItemCount(item.itemId, count - 1);
        RefreshRow(index);
        SetMessage(item.itemName + "を使った。");
    }

    private bool ApplyItemEffect(InventoryItemEntry item)
    {
        switch (item.effectType)
        {
            case ItemEffectType.None:
                return true;

            case ItemEffectType.HealHP:
                return TryHealTarget(item.effectValue);

            default:
                return false;
        }
    }

    private bool TryHealTarget(int amount)
    {
        if (hpTarget == null)
        {
            SetMessage(noTargetMessage);
            return false;
        }

        MonoBehaviour[] components = hpTarget.GetComponents<MonoBehaviour>();
        if (components == null || components.Length == 0)
        {
            SetMessage(noTargetMessage);
            return false;
        }

        for (int c = 0; c < components.Length; c++)
        {
            MonoBehaviour mb = components[c];
            if (mb == null) continue;

            System.Type type = mb.GetType();

            for (int i = 0; i < healMethodNames.Length; i++)
            {
                string methodName = healMethodNames[i];
                if (string.IsNullOrEmpty(methodName)) continue;

                MethodInfo intMethod = type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new System.Type[] { typeof(int) },
                    null
                );

                if (intMethod != null)
                {
                    intMethod.Invoke(mb, new object[] { amount });
                    return true;
                }

                MethodInfo floatMethod = type.GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new System.Type[] { typeof(float) },
                    null
                );

                if (floatMethod != null)
                {
                    floatMethod.Invoke(mb, new object[] { (float)amount });
                    return true;
                }
            }
        }

        SetMessage("HP回復用のメソッドが見つからない。");
        return false;
    }

    private int GetItemCount(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        return PlayerPrefs.GetInt(BuildRecoveryItemKey(itemId), 0);
    }

    private void SetItemCount(string itemId, int count)
    {
        if (string.IsNullOrEmpty(itemId)) return;

        PlayerPrefs.SetInt(BuildRecoveryItemKey(itemId), Mathf.Max(0, count));
        PlayerPrefs.Save();
    }

    private string BuildRecoveryItemKey(string itemId)
    {
        return "MixRecoveryItemCount_" + itemId;
    }

    private void SetMessage(string text)
    {
        if (messageText != null)
        {
            messageText.text = text;
        }
    }

    private GameObject FindFirstUsableButtonObject()
    {
        if (items == null) return null;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            if (items[i].useButton == null) continue;
            if (!items[i].useButton.interactable) continue;

            return items[i].useButton.gameObject;
        }

        return null;
    }

    private void SetSelected(GameObject target)
    {
        if (target == null) return;
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }
}