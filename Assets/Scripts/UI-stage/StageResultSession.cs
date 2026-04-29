using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class StageResultSession : MonoBehaviour
{
    [Serializable]
    public class ResultItemData
    {
        public string itemName;
        public Sprite icon;
        public int count;

        public ResultItemData()
        {
            itemName = "";
            icon = null;
            count = 1;
        }

        public ResultItemData(string itemName, Sprite icon, int count = 1)
        {
            this.itemName = itemName;
            this.icon = icon;
            this.count = Mathf.Max(1, count);
        }
    }

    [Serializable]
    public class ResultSnapshot
    {
        public string stageName;
        public float clearSeconds;
        public List<ResultItemData> mdItems = new List<ResultItemData>();
        public List<ResultItemData> juiceItems = new List<ResultItemData>();
    }

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

    public static StageResultSession Instance { get; private set; }

    [Header("Runtime")]
    [SerializeField] private string currentStageName = "";
    [SerializeField] private float stageStartTime = 0f;
    [SerializeField] private bool stageStarted = false;
    [SerializeField] private bool baselineCaptured = false;

    [Header("Debug Baseline")]
    [SerializeField] private List<int> baselineJuiceDefinitionIndices = new List<int>();
    [SerializeField] private List<string> baselineMdKeys = new List<string>();

    [Header("Debug Registered MD Only")]
    [SerializeField] private List<ResultItemData> manuallyRegisteredMdItems = new List<ResultItemData>();

    private Dictionary<int, int> baselineJuiceCountMap = new Dictionary<int, int>();
    private Dictionary<string, MdOwnedInfo> baselineMdOwnedMap = new Dictionary<string, MdOwnedInfo>();

    private FieldInfo mdDisksField;

    public static StageResultSession EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        StageResultSession existing = FindObjectOfType<StageResultSession>(true);
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("StageResultSession");
        Instance = go.AddComponent<StageResultSession>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        mdDisksField = typeof(MdDeskPageController).GetField(
            "mdDisks",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
    }

    public void BeginStage(string stageName)
    {
        currentStageName = stageName;
        stageStartTime = Time.time;
        stageStarted = true;
        baselineCaptured = false;

        baselineJuiceCountMap.Clear();
        baselineMdOwnedMap.Clear();
        baselineJuiceDefinitionIndices.Clear();
        baselineMdKeys.Clear();
        manuallyRegisteredMdItems.Clear();

        StopAllCoroutines();
        StartCoroutine(CaptureBaselineDeferred());

        Debug.Log("[StageResultSession] BeginStage: " + currentStageName);
    }

    public void ClearRecordedItemsOnly()
    {
        manuallyRegisteredMdItems.Clear();
    }

    public void RegisterJuice(string itemName, Sprite icon)
    {
        // ここでは何もしない。
        // ジュースは「入手した瞬間」ではなく、
        // ゴール時点で実際に所持している数だけを CreateSnapshot() で計算する。
        // これにより、途中で飲んだジュースやミックス素材として消費したジュースはリザルトに出ない。
    }

    public void RegisterMd(string itemName, Sprite icon)
    {
        string safeName = string.IsNullOrWhiteSpace(itemName) ? "MD" : itemName;

        for (int i = 0; i < manuallyRegisteredMdItems.Count; i++)
        {
            ResultItemData item = manuallyRegisteredMdItems[i];
            if (item == null)
                continue;

            if (item.itemName == safeName && item.icon == icon)
            {
                item.count++;
                return;
            }
        }

        manuallyRegisteredMdItems.Add(new ResultItemData(safeName, icon, 1));
    }

    public ResultSnapshot CreateSnapshot()
    {
        if (!baselineCaptured)
        {
            CaptureBaselineNow();
        }

        ResultSnapshot snapshot = new ResultSnapshot();
        snapshot.stageName = currentStageName;
        snapshot.clearSeconds = GetCurrentClearSeconds();

        BuildJuiceResult(snapshot);
        BuildMdResult(snapshot);

        Debug.Log(
            "[StageResultSession] CreateSnapshot Stage=" +
            snapshot.stageName +
            " Time=" +
            FormatTime(snapshot.clearSeconds) +
            " MD=" +
            snapshot.mdItems.Count +
            " Juice=" +
            snapshot.juiceItems.Count
        );

        return snapshot;
    }

    public float GetCurrentClearSeconds()
    {
        if (!stageStarted)
            return 0f;

        return Mathf.Max(0f, Time.time - stageStartTime);
    }

    public static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int hour = total / 3600;
        int minute = (total % 3600) / 60;
        int second = total % 60;

        return string.Format("{0:00}:{1:00}:{2:00}", hour, minute, second);
    }

    private IEnumerator CaptureBaselineDeferred()
    {
        yield return null;
        yield return null;

        CaptureBaselineNow();
    }

    private void CaptureBaselineNow()
    {
        baselineJuiceCountMap = BuildCurrentJuiceCountMap();
        baselineMdOwnedMap = BuildCurrentMdOwnedMap();

        baselineJuiceDefinitionIndices.Clear();
        foreach (KeyValuePair<int, int> pair in baselineJuiceCountMap)
        {
            for (int i = 0; i < pair.Value; i++)
            {
                baselineJuiceDefinitionIndices.Add(pair.Key);
            }
        }

        baselineMdKeys.Clear();
        foreach (KeyValuePair<string, MdOwnedInfo> pair in baselineMdOwnedMap)
        {
            baselineMdKeys.Add(pair.Key);
        }

        baselineCaptured = true;

        Debug.Log(
            "[StageResultSession] Baseline captured. Juice=" +
            baselineJuiceDefinitionIndices.Count +
            " MD=" +
            baselineMdKeys.Count
        );
    }

    private void BuildJuiceResult(ResultSnapshot snapshot)
    {
        Dictionary<int, int> currentJuiceCounts = BuildCurrentJuiceCountMap();

        foreach (KeyValuePair<int, int> pair in currentJuiceCounts)
        {
            int definitionIndex = pair.Key;
            int currentCount = pair.Value;

            int startCount = 0;
            baselineJuiceCountMap.TryGetValue(definitionIndex, out startCount);

            int carriedNewCount = currentCount - startCount;

            if (carriedNewCount <= 0)
                continue;

            JuiceInventory inventory = JuiceInventory.Instance;
            if (inventory == null)
                continue;

            JuiceInventory.JuiceDefinition definition = inventory.GetDefinitionAt(definitionIndex);
            if (definition == null)
                continue;

            Sprite icon = definition.icon;
            if (icon == null)
            {
                icon = inventory.ResolveFallbackIconForJuiceId(definition.id);
            }

            snapshot.juiceItems.Add(new ResultItemData(
                string.IsNullOrWhiteSpace(definition.displayName) ? definition.id : definition.displayName,
                icon,
                carriedNewCount
            ));
        }
    }

    private void BuildMdResult(ResultSnapshot snapshot)
    {
        HashSet<string> addedMdNames = new HashSet<string>();

        Dictionary<string, MdOwnedInfo> currentMdMap = BuildCurrentMdOwnedMap();

        foreach (KeyValuePair<string, MdOwnedInfo> pair in currentMdMap)
        {
            if (baselineMdOwnedMap.ContainsKey(pair.Key))
                continue;

            MdOwnedInfo info = pair.Value;
            string title = string.IsNullOrWhiteSpace(info.title) ? "MD" : info.title;

            snapshot.mdItems.Add(new ResultItemData(title, info.icon, 1));

            if (!addedMdNames.Contains(title))
                addedMdNames.Add(title);
        }

        for (int i = 0; i < manuallyRegisteredMdItems.Count; i++)
        {
            ResultItemData item = manuallyRegisteredMdItems[i];
            if (item == null)
                continue;

            string title = string.IsNullOrWhiteSpace(item.itemName) ? "MD" : item.itemName;

            if (addedMdNames.Contains(title))
                continue;

            snapshot.mdItems.Add(new ResultItemData(title, item.icon, item.count));
            addedMdNames.Add(title);
        }
    }

    private Dictionary<int, int> BuildCurrentJuiceCountMap()
    {
        Dictionary<int, int> counts = new Dictionary<int, int>();

        JuiceInventory inventory = JuiceInventory.Instance;
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

    private Dictionary<string, MdOwnedInfo> BuildCurrentMdOwnedMap()
    {
        Dictionary<string, MdOwnedInfo> map = new Dictionary<string, MdOwnedInfo>();

        MdDeskPageController[] controllers = FindObjectsOfType<MdDeskPageController>(true);
        if (controllers == null || controllers.Length == 0)
            return map;

        if (mdDisksField == null)
            return map;

        for (int controllerIndex = 0; controllerIndex < controllers.Length; controllerIndex++)
        {
            MdDeskPageController controller = controllers[controllerIndex];
            if (controller == null)
                continue;

            object rawList = mdDisksField.GetValue(controller);
            System.Collections.IList diskList = rawList as System.Collections.IList;
            if (diskList == null)
                continue;

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

        return map;
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