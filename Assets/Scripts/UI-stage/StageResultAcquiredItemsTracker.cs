using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class StageResultAcquiredItemsTracker : MonoBehaviour
{
    [Serializable]
    private class MdOwnedInfo
    {
        public string key;
        public string title;
        public Sprite icon;

        public MdOwnedInfo(string key, string title, Sprite icon)
        {
            this.key = key;
            this.title = title;
            this.icon = icon;
        }
    }

    [Header("Track Targets")]
    [SerializeField] private bool trackJuices = true;
    [SerializeField] private bool trackMdDisks = true;

    [Header("MD Polling")]
    [SerializeField] private float mdPollInterval = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    private JuiceInventory hookedJuiceInventory;
    private Dictionary<int, int> previousJuiceCounts = new Dictionary<int, int>();

    private Dictionary<string, MdOwnedInfo> previousMdOwnedMap = new Dictionary<string, MdOwnedInfo>();
    private bool mdBaselineCaptured = false;
    private float nextMdPollTime = 0f;

    private FieldInfo mdDisksField;

    private void Awake()
    {
        mdDisksField = typeof(MdDeskPageController).GetField(
            "mdDisks",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        StageResultSession.EnsureInstance();
    }

    private void OnEnable()
    {
        ResetBaselinesNow();
    }

    private void OnDisable()
    {
        UnhookJuiceInventory();
    }

    private void Start()
    {
        ResetBaselinesNow();
    }

    private void Update()
    {
        if (trackJuices && hookedJuiceInventory == null)
        {
            TryHookJuiceInventory(false);
        }

        if (trackMdDisks)
        {
            if (Time.unscaledTime >= nextMdPollTime)
            {
                nextMdPollTime = Time.unscaledTime + Mathf.Max(0.05f, mdPollInterval);
                PollMdChanges();
            }
        }
    }

    public void ResetBaselinesNow()
    {
        TryHookJuiceInventory(true);
        CaptureMdBaselineIfPossible();

        if (verboseLog)
        {
            Debug.Log("[StageResultAcquiredItemsTracker] Baselines reset.");
        }
    }

    private void TryHookJuiceInventory(bool captureBaseline)
    {
        if (!trackJuices)
            return;

        JuiceInventory inventory = JuiceInventory.Instance;
        if (inventory == null)
            return;

        if (hookedJuiceInventory == inventory)
        {
            if (captureBaseline)
            {
                previousJuiceCounts = BuildJuiceCountMap(inventory);
            }

            return;
        }

        UnhookJuiceInventory();

        hookedJuiceInventory = inventory;
        hookedJuiceInventory.OnInventoryChanged += OnJuiceInventoryChanged;

        if (captureBaseline)
        {
            previousJuiceCounts = BuildJuiceCountMap(hookedJuiceInventory);
        }

        if (verboseLog)
        {
            Debug.Log("[StageResultAcquiredItemsTracker] JuiceInventory hooked.");
        }
    }

    private void UnhookJuiceInventory()
    {
        if (hookedJuiceInventory != null)
        {
            hookedJuiceInventory.OnInventoryChanged -= OnJuiceInventoryChanged;
            hookedJuiceInventory = null;
        }
    }

    private void OnJuiceInventoryChanged()
    {
        if (!trackJuices)
            return;

        if (hookedJuiceInventory == null)
            return;

        Dictionary<int, int> currentCounts = BuildJuiceCountMap(hookedJuiceInventory);

        if (verboseLog)
        {
            int previousTotal = CountTotal(previousJuiceCounts);
            int currentTotal = CountTotal(currentCounts);

            Debug.Log(
                "[StageResultAcquiredItemsTracker] Juice changed. Previous=" +
                previousTotal +
                " Current=" +
                currentTotal +
                " Result display is calculated by StageResultSession at goal."
            );
        }

        previousJuiceCounts = currentCounts;
    }

    private int CountTotal(Dictionary<int, int> counts)
    {
        int total = 0;

        if (counts == null)
            return total;

        foreach (KeyValuePair<int, int> pair in counts)
        {
            total += pair.Value;
        }

        return total;
    }

    private Dictionary<int, int> BuildJuiceCountMap(JuiceInventory inventory)
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();

        if (inventory == null || inventory.OwnedJuiceIndices == null)
            return counts;

        IReadOnlyList<int> owned = inventory.OwnedJuiceIndices;

        for (int i = 0; i < owned.Count; i++)
        {
            int definitionIndex = owned[i];

            if (!inventory.IsValidDefinitionIndex(definitionIndex))
                continue;

            if (!counts.ContainsKey(definitionIndex))
            {
                counts.Add(definitionIndex, 1);
            }
            else
            {
                counts[definitionIndex]++;
            }
        }

        return counts;
    }

    private void CaptureMdBaselineIfPossible()
    {
        if (!trackMdDisks)
            return;

        Dictionary<string, MdOwnedInfo> currentMap;
        bool foundAnyController = TryBuildCurrentMdOwnedMap(out currentMap);

        if (!foundAnyController)
            return;

        previousMdOwnedMap = currentMap;
        mdBaselineCaptured = true;

        if (verboseLog)
        {
            Debug.Log("[StageResultAcquiredItemsTracker] MD baseline captured. Count=" + previousMdOwnedMap.Count);
        }
    }

    private void PollMdChanges()
    {
        if (!trackMdDisks)
            return;

        Dictionary<string, MdOwnedInfo> currentMap;
        bool foundAnyController = TryBuildCurrentMdOwnedMap(out currentMap);

        if (!foundAnyController)
            return;

        if (!mdBaselineCaptured)
        {
            previousMdOwnedMap = currentMap;
            mdBaselineCaptured = true;
            return;
        }

        foreach (KeyValuePair<string, MdOwnedInfo> pair in currentMap)
        {
            if (previousMdOwnedMap.ContainsKey(pair.Key))
                continue;

            MdOwnedInfo info = pair.Value;
            StageResultSession.EnsureInstance().RegisterMd(info.title, info.icon);

            if (verboseLog)
            {
                Debug.Log("[StageResultAcquiredItemsTracker] MD registered: " + info.title);
            }
        }

        previousMdOwnedMap = currentMap;
    }

    private bool TryBuildCurrentMdOwnedMap(out Dictionary<string, MdOwnedInfo> map)
    {
        map = new Dictionary<string, MdOwnedInfo>();

        MdDeskPageController[] controllers = FindObjectsOfType<MdDeskPageController>(true);
        if (controllers == null || controllers.Length == 0)
            return false;

        if (mdDisksField == null)
            return false;

        bool foundAnyController = false;

        for (int controllerIndex = 0; controllerIndex < controllers.Length; controllerIndex++)
        {
            MdDeskPageController controller = controllers[controllerIndex];
            if (controller == null)
                continue;

            object rawList = mdDisksField.GetValue(controller);
            System.Collections.IList diskList = rawList as System.Collections.IList;
            if (diskList == null)
                continue;

            foundAnyController = true;

            for (int diskIndex = 0; diskIndex < diskList.Count; diskIndex++)
            {
                object diskObject = diskList[diskIndex];
                if (diskObject == null)
                    continue;

                Type diskType = diskObject.GetType();

                bool owned = GetBoolField(diskType, diskObject, "owned");
                if (!owned)
                    continue;

                string id = GetStringField(diskType, diskObject, "id");
                string title = GetStringField(diskType, diskObject, "title");
                Sprite icon = GetSpriteField(diskType, diskObject, "artworkSprite");

                string key = BuildDiskKey(controllerIndex, diskIndex, id, title);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                if (!map.ContainsKey(key))
                {
                    map.Add(key, new MdOwnedInfo(
                        key,
                        string.IsNullOrWhiteSpace(title) ? "MD" : title,
                        icon
                    ));
                }
            }
        }

        return foundAnyController;
    }

    private string BuildDiskKey(int controllerIndex, int diskIndex, string id, string title)
    {
        if (!string.IsNullOrWhiteSpace(id))
            return "ID:" + id;

        if (!string.IsNullOrWhiteSpace(title))
            return "TITLE:" + title;

        return "CTRL:" + controllerIndex + "_IDX:" + diskIndex;
    }

    private bool GetBoolField(Type type, object target, string fieldName)
    {
        FieldInfo field = type.GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (field == null || field.FieldType != typeof(bool))
            return false;

        return (bool)field.GetValue(target);
    }

    private string GetStringField(Type type, object target, string fieldName)
    {
        FieldInfo field = type.GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (field == null)
            return string.Empty;

        object value = field.GetValue(target);
        return value as string ?? string.Empty;
    }

    private Sprite GetSpriteField(Type type, object target, string fieldName)
    {
        FieldInfo field = type.GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        if (field == null)
            return null;

        object value = field.GetValue(target);
        return value as Sprite;
    }
}