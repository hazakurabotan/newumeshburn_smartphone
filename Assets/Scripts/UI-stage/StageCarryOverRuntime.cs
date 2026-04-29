using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class StageCarryOverRuntime : MonoBehaviour
{
    [Serializable]
    public class ResultSnapshot
    {
        public int gainedMdCount;
        public int ownedMdCount;
        public int ownedJuiceCount;
        public int gainedCoinCount;
        public int ownedCoinCount;
    }

    private static StageCarryOverRuntime instance;

    public static StageCarryOverRuntime Instance
    {
        get
        {
            if (instance == null)
            {
                StageCarryOverRuntime found = FindObjectOfType<StageCarryOverRuntime>(true);
                if (found != null)
                {
                    instance = found;
                    instance.InitializeSingleton();
                }
                else
                {
                    GameObject go = new GameObject(nameof(StageCarryOverRuntime));
                    instance = go.AddComponent<StageCarryOverRuntime>();
                }
            }

            return instance;
        }
    }

    [Header("MD Carry Over")]
    [SerializeField] private List<string> ownedMdKeys = new List<string>();

    [Header("Coin Carry Over")]
    [SerializeField] private int carriedCoinCount = 0;
    [SerializeField] private bool hasCarriedCoinCount = false;

    [Header("Stage Start Baseline")]
    [SerializeField] private int stageStartMdOwnedCount = 0;
    [SerializeField] private int stageStartCoinCount = 0;
    [SerializeField] private bool stageTrackingStarted = false;

    [Header("Last Result Snapshot")]
    [SerializeField] private ResultSnapshot lastResultSnapshot = new ResultSnapshot();

    private bool initialized = false;

    public IReadOnlyList<string> OwnedMdKeys => ownedMdKeys;
    public ResultSnapshot LastResultSnapshot => lastResultSnapshot;
    public int CurrentOwnedMdCount => ownedMdKeys != null ? ownedMdKeys.Count : 0;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        InitializeSingleton();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void InitializeSingleton()
    {
        if (initialized)
            return;

        initialized = true;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (ownedMdKeys == null)
            ownedMdKeys = new List<string>();
    }

    public void ResetAllData()
    {
        if (ownedMdKeys == null)
            ownedMdKeys = new List<string>();

        ownedMdKeys.Clear();
        carriedCoinCount = 0;
        hasCarriedCoinCount = false;
        stageStartMdOwnedCount = 0;
        stageStartCoinCount = 0;
        stageTrackingStarted = false;
        lastResultSnapshot = new ResultSnapshot();
    }

    public void BeginStageTracking()
    {
        ApplyCarryOverToScene();

        if (!TryCaptureOwnedMdKeysFromScene())
        {
            // 既存保持値をそのまま使う
        }

        stageStartMdOwnedCount = CurrentOwnedMdCount;
        stageStartCoinCount = GetCurrentCoinCount();
        stageTrackingStarted = true;
    }

    public ResultSnapshot CaptureResultSnapshot()
    {
        if (!TryCaptureOwnedMdKeysFromScene())
        {
            // 既存保持値をそのまま使う
        }

        int ownedMdCount = CurrentOwnedMdCount;
        int ownedJuiceCount = GetCurrentJuiceCount();
        int ownedCoinCount = GetCurrentCoinCount();

        carriedCoinCount = ownedCoinCount;
        hasCarriedCoinCount = true;

        int gainedMdCount = stageTrackingStarted
            ? Mathf.Max(0, ownedMdCount - stageStartMdOwnedCount)
            : ownedMdCount;

        int gainedCoinCount = stageTrackingStarted
            ? Mathf.Max(0, ownedCoinCount - stageStartCoinCount)
            : ownedCoinCount;

        lastResultSnapshot = new ResultSnapshot
        {
            gainedMdCount = gainedMdCount,
            ownedMdCount = ownedMdCount,
            ownedJuiceCount = ownedJuiceCount,
            gainedCoinCount = gainedCoinCount,
            ownedCoinCount = ownedCoinCount
        };

        return lastResultSnapshot;
    }

    public void ApplyCarryOverToScene()
    {
        ApplyOwnedMdKeysToAllControllers();

        if (hasCarriedCoinCount)
        {
            TrySetCurrentCoinCount(carriedCoinCount);
        }
    }

    public void AddOwnedMdKey(string diskKey)
    {
        if (string.IsNullOrWhiteSpace(diskKey))
            return;

        if (ownedMdKeys == null)
            ownedMdKeys = new List<string>();

        if (!ownedMdKeys.Contains(diskKey))
        {
            ownedMdKeys.Add(diskKey);
        }
    }

    public bool ContainsOwnedMdKey(string diskKey)
    {
        if (string.IsNullOrWhiteSpace(diskKey) || ownedMdKeys == null)
            return false;

        return ownedMdKeys.Contains(diskKey);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCarryOverToScene();
    }

    private int GetCurrentJuiceCount()
    {
        if (JuiceInventory.Instance == null)
            return 0;

        return Mathf.Max(0, JuiceInventory.Instance.Count);
    }

    private bool TryCaptureOwnedMdKeysFromScene()
    {
        MdDeskPageController[] controllers = FindObjectsOfType<MdDeskPageController>(true);
        if (controllers == null || controllers.Length == 0)
            return false;

        HashSet<string> merged = new HashSet<string>(StringComparer.Ordinal);
        bool foundAnyDiskList = false;

        for (int controllerIndex = 0; controllerIndex < controllers.Length; controllerIndex++)
        {
            MdDeskPageController controller = controllers[controllerIndex];
            if (controller == null)
                continue;

            IList diskList = GetMdDiskList(controller);
            if (diskList == null)
                continue;

            foundAnyDiskList = true;

            for (int i = 0; i < diskList.Count; i++)
            {
                object diskObject = diskList[i];
                if (diskObject == null)
                    continue;

                if (!GetDiskOwnedValue(diskObject))
                    continue;

                string key = BuildDiskKey(diskObject, i);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    merged.Add(key);
                }
            }
        }

        if (!foundAnyDiskList)
            return false;

        ownedMdKeys.Clear();
        ownedMdKeys.AddRange(merged);
        return true;
    }

    private void ApplyOwnedMdKeysToAllControllers()
    {
        MdDeskPageController[] controllers = FindObjectsOfType<MdDeskPageController>(true);
        if (controllers == null || controllers.Length == 0)
            return;

        HashSet<string> ownedKeySet = new HashSet<string>(ownedMdKeys, StringComparer.Ordinal);

        for (int controllerIndex = 0; controllerIndex < controllers.Length; controllerIndex++)
        {
            MdDeskPageController controller = controllers[controllerIndex];
            if (controller == null)
                continue;

            IList diskList = GetMdDiskList(controller);
            if (diskList == null)
                continue;

            for (int i = 0; i < diskList.Count; i++)
            {
                object diskObject = diskList[i];
                if (diskObject == null)
                    continue;

                string key = BuildDiskKey(diskObject, i);
                bool shouldOwn = ownedKeySet.Contains(key);
                SetDiskOwnedValue(diskObject, shouldOwn);
            }

            controller.SetSelectedIndex(controller.SelectedIndex, true);
        }
    }

    private IList GetMdDiskList(MdDeskPageController controller)
    {
        if (controller == null)
            return null;

        FieldInfo field = typeof(MdDeskPageController).GetField("mdDisks", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null)
            return null;

        return field.GetValue(controller) as IList;
    }

    private string BuildDiskKey(object diskObject, int index)
    {
        if (diskObject == null)
            return string.Empty;

        Type type = diskObject.GetType();

        string id = ReadStringField(type, diskObject, "id");
        if (!string.IsNullOrWhiteSpace(id))
            return id.Trim();

        string title = ReadStringField(type, diskObject, "title");
        if (!string.IsNullOrWhiteSpace(title))
            return "__TITLE__" + title.Trim();

        return "__INDEX__" + index;
    }

    private string ReadStringField(Type type, object target, string fieldName)
    {
        if (type == null || target == null || string.IsNullOrWhiteSpace(fieldName))
            return string.Empty;

        FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field == null)
            return string.Empty;

        object value = field.GetValue(target);
        return value as string ?? string.Empty;
    }

    private bool GetDiskOwnedValue(object diskObject)
    {
        if (diskObject == null)
            return false;

        Type type = diskObject.GetType();
        FieldInfo ownedField = type.GetField("owned", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (ownedField == null || ownedField.FieldType != typeof(bool))
            return false;

        return (bool)ownedField.GetValue(diskObject);
    }

    private void SetDiskOwnedValue(object diskObject, bool value)
    {
        if (diskObject == null)
            return;

        Type type = diskObject.GetType();
        FieldInfo ownedField = type.GetField("owned", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (ownedField == null || ownedField.FieldType != typeof(bool))
            return;

        ownedField.SetValue(diskObject, value);
    }

    private int GetCurrentCoinCount()
    {
        if (TryGetGameCurrencyCoins(out int coins))
            return Mathf.Max(0, coins);

        return hasCarriedCoinCount ? Mathf.Max(0, carriedCoinCount) : 0;
    }

    private bool TrySetCurrentCoinCount(int value)
    {
        value = Mathf.Max(0, value);

        Type gameCurrencyType = FindTypeByName("GameCurrency");
        if (gameCurrencyType == null)
            return false;

        InvokeStaticMethodIfExists(gameCurrencyType, "EnsureInstance");

        object instanceObject = GetStaticMemberValue(gameCurrencyType, "Instance");
        if (instanceObject == null)
            return false;

        Type instanceType = instanceObject.GetType();

        PropertyInfo coinsProperty = instanceType.GetProperty("Coins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (coinsProperty != null)
        {
            if (coinsProperty.CanWrite)
            {
                coinsProperty.SetValue(instanceObject, value, null);
                return true;
            }

            FieldInfo backingField = instanceType.GetField("<Coins>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (backingField != null && backingField.FieldType == typeof(int))
            {
                backingField.SetValue(instanceObject, value);
                return true;
            }
        }

        FieldInfo coinsField = instanceType.GetField("coins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            ?? instanceType.GetField("Coins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            ?? instanceType.GetField("coin", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (coinsField != null && coinsField.FieldType == typeof(int))
        {
            coinsField.SetValue(instanceObject, value);
            return true;
        }

        MethodInfo setCoinsMethod = instanceType.GetMethod("SetCoins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (setCoinsMethod != null)
        {
            setCoinsMethod.Invoke(instanceObject, new object[] { value });
            return true;
        }

        return false;
    }

    private bool TryGetGameCurrencyCoins(out int coins)
    {
        coins = 0;

        Type gameCurrencyType = FindTypeByName("GameCurrency");
        if (gameCurrencyType == null)
            return false;

        InvokeStaticMethodIfExists(gameCurrencyType, "EnsureInstance");

        object instanceObject = GetStaticMemberValue(gameCurrencyType, "Instance");
        if (instanceObject == null)
            return false;

        Type instanceType = instanceObject.GetType();

        PropertyInfo coinsProperty = instanceType.GetProperty("Coins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (coinsProperty != null && coinsProperty.CanRead)
        {
            object value = coinsProperty.GetValue(instanceObject, null);
            if (value is int intValue)
            {
                coins = intValue;
                return true;
            }
        }

        FieldInfo coinsField = instanceType.GetField("coins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            ?? instanceType.GetField("Coins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            ?? instanceType.GetField("coin", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (coinsField != null && coinsField.FieldType == typeof(int))
        {
            coins = (int)coinsField.GetValue(instanceObject);
            return true;
        }

        return false;
    }

    private void InvokeStaticMethodIfExists(Type type, string methodName)
    {
        if (type == null || string.IsNullOrWhiteSpace(methodName))
            return;

        MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null)
            return;

        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length == 0)
        {
            method.Invoke(null, null);
        }
    }

    private object GetStaticMemberValue(Type type, string memberName)
    {
        if (type == null || string.IsNullOrWhiteSpace(memberName))
            return null;

        PropertyInfo property = type.GetProperty(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.CanRead)
        {
            return property.GetValue(null, null);
        }

        FieldInfo field = type.GetField(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            return field.GetValue(null);
        }

        return null;
    }

    private Type FindTypeByName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            Assembly assembly = assemblies[i];
            if (assembly == null)
                continue;

            Type type = assembly.GetType(typeName, false);
            if (type != null)
                return type;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            if (types == null)
                continue;

            for (int t = 0; t < types.Length; t++)
            {
                Type candidate = types[t];
                if (candidate == null)
                    continue;

                if (candidate.Name == typeName)
                    return candidate;
            }
        }

        return null;
    }
}