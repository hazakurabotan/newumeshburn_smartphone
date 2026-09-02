using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Collider2D))]
public class RingoCoinHealShop : MonoBehaviour
{
    [Header("Shop Settings")]
    public int coinCost = 1;
    public int healAmount = 3;
    public float buyCooldownSeconds = 0.2f;

    [Header("Coin Source")]
    [Tooltip("コインを管理しているスクリプトを持つオブジェクト。未設定でも自動検索しますが、GameManagerなどを入れると確実です。")]
    public MonoBehaviour coinOwner;
    public bool autoFindCoinOwner = true;

    [Tooltip("どうしても既存コイン管理が見つからない時だけON。通常はOFF推奨。")]
    public bool allowPlayerPrefsFallback = false;
    public string playerPrefsCoinKey = "Coins";

    [Header("Input")]
    [Tooltip("ONにするとPlayerInputのAction名でも購入判定します。Eastボタンだけにしたい場合はOFF推奨。")]
    public bool useActionMapBuyAction = false;

    [Tooltip("useActionMapBuyActionがONの時だけ使います。")]
    public string buyActionName = "Interact";

    [Tooltip("ジョイコン / ゲームパッドのEastボタンを直接見る。")]
    public bool useGamepadButtonEast = true;

    [Tooltip("キーボード確認用。今回はジョイコンEastを使うのでOFF推奨。")]
    public bool allowKeyboardFallback = false;
    public Key keyboardFallbackKey = Key.B;

    [Header("Target")]
    [Tooltip("ONなら、リンゴ前でボタンを押したキャラだけ回復します。")]
    public bool healBuyerOnly = true;

    [Header("Optional Message")]
    public GameObject messageRoot;
    public TextMeshProUGUI messageText;
    public float messageSeconds = 1.2f;
    public string successMessage = "リンゴを買った！ HPが3回復した";
    public string noCoinMessage = "コインが足りない！";
    public string fullHpMessage = "HPはすでに満タン！";

    [Header("Debug")]
    public bool debugLog = true;

    readonly List<PlayerInput> playersInRange = new List<PlayerInput>();
    PlayerInput activeBuyer;
    float nextBuyTime;
    Coroutine messageCoroutine;

    static readonly string[] CoinFieldNames =
    {
        "coin", "coins", "coinCount", "coinCounts", "currentCoin", "currentCoins",
        "totalCoin", "totalCoins", "coinNum", "coinNumber", "money", "currentMoney"
    };

    static readonly string[] TrySpendMethodNames =
    {
        "TrySpendCoin", "TrySpendCoins", "TryUseCoin", "TryUseCoins",
        "TryConsumeCoin", "TryConsumeCoins", "TryRemoveCoin", "TryRemoveCoins"
    };

    static readonly string[] SpendMethodNames =
    {
        "SpendCoin", "SpendCoins", "UseCoin", "UseCoins",
        "ConsumeCoin", "ConsumeCoins", "RemoveCoin", "RemoveCoins",
        "SubtractCoin", "SubtractCoins"
    };

    static readonly string[] RefreshMethodNames =
    {
        "Refresh", "RefreshUI", "RefreshDisplay", "UpdateUI", "UpdateDisplay",
        "UpdateCoinUI", "UpdateCoinText", "SetCoinText"
    };

    static readonly string[] HealMethodNames =
    {
        "Heal", "Recover", "RecoverHp", "RecoverHP", "RestoreHp", "RestoreHP",
        "AddHp", "AddHP", "AddHealth", "RestoreHealth"
    };

    static readonly string[] CurrentHpNames =
    {
        "currentHP", "currentHp", "hp", "HP", "currentHealth", "health"
    };

    static readonly string[] MaxHpNames =
    {
        "maxHP", "maxHp", "maxHealth", "MaxHP", "MaxHp"
    };

    static readonly string[] HpBarFieldNames =
    {
        "hpBar", "hpBarController", "healthBar", "healthBarController"
    };

    void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    void Awake()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;

        if (messageRoot != null)
            messageRoot.SetActive(false);
    }

    void Update()
    {
        CleanupMissingPlayers();

        if (playersInRange.Count <= 0)
            return;

        if (Time.unscaledTime < nextBuyTime)
            return;

        PlayerInput buyer = GetInputBuyer();
        if (buyer == null)
            return;

        if (!WasBuyPressed(buyer))
            return;

        nextBuyTime = Time.unscaledTime + buyCooldownSeconds;
        activeBuyer = buyer;
        TryBuyApple();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInput pi = other.GetComponentInParent<PlayerInput>();
        if (pi == null) return;

        if (!playersInRange.Contains(pi))
            playersInRange.Add(pi);

        activeBuyer = pi;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        PlayerInput pi = other.GetComponentInParent<PlayerInput>();
        if (pi == null) return;

        if (!playersInRange.Contains(pi))
            playersInRange.Add(pi);

        activeBuyer = pi;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PlayerInput pi = other.GetComponentInParent<PlayerInput>();
        if (pi == null) return;

        playersInRange.Remove(pi);

        if (activeBuyer == pi)
            activeBuyer = playersInRange.Count > 0 ? playersInRange[playersInRange.Count - 1] : null;
    }

    PlayerInput GetInputBuyer()
    {
        if (activeBuyer != null && playersInRange.Contains(activeBuyer))
            return activeBuyer;

        if (playersInRange.Count > 0)
        {
            activeBuyer = playersInRange[playersInRange.Count - 1];
            return activeBuyer;
        }

        return null;
    }

    bool WasBuyPressed(PlayerInput buyer)
    {
        if (useActionMapBuyAction && buyer != null && buyer.currentActionMap != null && !string.IsNullOrEmpty(buyActionName))
        {
            InputAction action = buyer.currentActionMap.FindAction(buyActionName, throwIfNotFound: false);
            if (action != null && action.WasPerformedThisFrame())
                return true;
        }

        if (useGamepadButtonEast && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            return true;

        if (allowKeyboardFallback && Keyboard.current != null)
        {
            KeyControl key = Keyboard.current[keyboardFallbackKey];
            if (key != null && key.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    void TryBuyApple()
    {
        if (!TrySpendCoins(coinCost))
        {
            ShowMessage(noCoinMessage);
            if (debugLog) Debug.Log("[RingoCoinHealShop] コイン不足で購入できません。Cost=" + coinCost);
            return;
        }

        GameObject healTarget = FindHealTarget();
        bool healed = TryHealTarget(healTarget, healAmount, out bool alreadyFull);

        if (healed)
        {
            ShowMessage(successMessage);
            if (debugLog) Debug.Log("[RingoCoinHealShop] リンゴ購入成功。HP +" + healAmount);
        }
        else
        {
            if (alreadyFull)
                ShowMessage(fullHpMessage);
            else
                ShowMessage(successMessage);

            if (debugLog)
                Debug.LogWarning("[RingoCoinHealShop] コインは消費しましたが、HP回復先を更新できませんでした。Target=" + (healTarget ? healTarget.name : "null"));
        }
    }

    GameObject FindHealTarget()
    {
        if (healBuyerOnly && activeBuyer != null)
            return activeBuyer.gameObject;

        PlayerInput buyer = GetInputBuyer();
        if (buyer != null)
            return buyer.gameObject;

        return null;
    }

    bool TrySpendCoins(int cost)
    {
        if (cost <= 0) return true;

        if (coinOwner != null)
        {
            if (TrySpendCoinsOnObject(coinOwner, cost))
                return true;
        }

        if (autoFindCoinOwner)
        {
            MonoBehaviour found = FindCoinOwnerBySceneSearch(cost);
            if (found != null)
            {
                coinOwner = found;
                if (TrySpendCoinsOnObject(coinOwner, cost))
                    return true;
            }

            if (TrySpendCoinsOnStaticCoinType(cost))
                return true;
        }

        if (allowPlayerPrefsFallback)
        {
            int coins = PlayerPrefs.GetInt(playerPrefsCoinKey, 0);
            if (coins >= cost)
            {
                PlayerPrefs.SetInt(playerPrefsCoinKey, coins - cost);
                PlayerPrefs.Save();
                return true;
            }
        }

        return false;
    }

    MonoBehaviour FindCoinOwnerBySceneSearch(int cost)
    {
        MonoBehaviour[] all = FindObjectsOfType<MonoBehaviour>(true);

        MonoBehaviour best = null;
        int bestScore = int.MinValue;

        foreach (MonoBehaviour mb in all)
        {
            if (mb == null || mb == this) continue;

            Type t = mb.GetType();
            string typeName = t.Name.ToLowerInvariant();
            string objectName = mb.gameObject.name.ToLowerInvariant();

            int score = 0;
            if (typeName.Contains("coin")) score += 100;
            if (typeName.Contains("wallet")) score += 80;
            if (typeName.Contains("money")) score += 80;
            if (typeName.Contains("inventory")) score += 40;
            if (typeName.Contains("manager")) score += 30;
            if (objectName.Contains("coin")) score += 40;
            if (objectName.Contains("gamemanager")) score += 25;
            if (objectName.Contains("manager")) score += 15;

            if (!CanReadCoinCount(mb, out int coins))
            {
                if (!HasTrySpendMethod(mb))
                    continue;
            }
            else
            {
                if (coins < cost)
                    continue;

                score += 50;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = mb;
            }
        }

        return best;
    }

    bool TrySpendCoinsOnObject(object obj, int cost)
    {
        if (obj == null) return false;

        if (TryCallTrySpendMethod(obj, cost, out bool methodResult))
        {
            if (methodResult)
            {
                RefreshCoinObject(obj);
                return true;
            }
        }

        if (CanReadCoinCount(obj, out int currentCoins))
        {
            if (currentCoins < cost)
                return false;

            if (TryCallSpendMethod(obj, cost))
            {
                RefreshCoinObject(obj);
                return true;
            }

            if (TryWriteCoinCount(obj, currentCoins - cost))
            {
                RefreshCoinObject(obj);
                return true;
            }
        }

        return false;
    }

    bool HasTrySpendMethod(object obj)
    {
        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in TrySpendMethodNames)
        {
            MethodInfo[] methods = t.GetMethods(flags);
            foreach (MethodInfo m in methods)
            {
                if (m.Name != name) continue;

                ParameterInfo[] p = m.GetParameters();
                if (p.Length == 1 && IsNumberType(p[0].ParameterType)) return true;
                if (p.Length == 0) return true;
            }
        }

        return false;
    }

    bool TryCallTrySpendMethod(object obj, int cost, out bool result)
    {
        result = false;

        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in TrySpendMethodNames)
        {
            MethodInfo[] methods = t.GetMethods(flags);
            foreach (MethodInfo m in methods)
            {
                if (m.Name != name) continue;

                ParameterInfo[] p = m.GetParameters();
                object returnValue = null;

                try
                {
                    if (p.Length == 1 && IsNumberType(p[0].ParameterType))
                    {
                        returnValue = m.Invoke(obj, new object[] { Convert.ChangeType(cost, p[0].ParameterType) });
                    }
                    else if (p.Length == 0)
                    {
                        returnValue = m.Invoke(obj, null);
                    }
                    else
                    {
                        continue;
                    }
                }
                catch
                {
                    continue;
                }

                if (m.ReturnType == typeof(bool))
                {
                    result = returnValue is bool b && b;
                    return true;
                }

                result = true;
                return true;
            }
        }

        return false;
    }

    bool TryCallSpendMethod(object obj, int cost)
    {
        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in SpendMethodNames)
        {
            MethodInfo[] methods = t.GetMethods(flags);
            foreach (MethodInfo m in methods)
            {
                if (m.Name != name) continue;

                ParameterInfo[] p = m.GetParameters();

                try
                {
                    if (p.Length == 1 && IsNumberType(p[0].ParameterType))
                    {
                        m.Invoke(obj, new object[] { Convert.ChangeType(cost, p[0].ParameterType) });
                        return true;
                    }

                    if (p.Length == 0)
                    {
                        m.Invoke(obj, null);
                        return true;
                    }
                }
                catch
                {
                }
            }
        }

        return false;
    }

    bool CanReadCoinCount(object obj, out int value)
    {
        value = 0;
        if (obj == null) return false;

        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in CoinFieldNames)
        {
            FieldInfo f = t.GetField(name, flags);
            if (f != null && IsNumberType(f.FieldType))
            {
                object raw = f.GetValue(obj);
                value = Convert.ToInt32(raw);
                return true;
            }

            PropertyInfo p = t.GetProperty(name, flags);
            if (p != null && p.CanRead && IsNumberType(p.PropertyType))
            {
                object raw = p.GetValue(obj, null);
                value = Convert.ToInt32(raw);
                return true;
            }
        }

        return false;
    }

    bool TryWriteCoinCount(object obj, int value)
    {
        if (obj == null) return false;

        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in CoinFieldNames)
        {
            FieldInfo f = t.GetField(name, flags);
            if (f != null && IsNumberType(f.FieldType) && !f.IsInitOnly)
            {
                f.SetValue(obj, Convert.ChangeType(value, f.FieldType));
                return true;
            }

            PropertyInfo p = t.GetProperty(name, flags);
            if (p != null && p.CanWrite && IsNumberType(p.PropertyType))
            {
                p.SetValue(obj, Convert.ChangeType(value, p.PropertyType), null);
                return true;
            }
        }

        return false;
    }

    void RefreshCoinObject(object obj)
    {
        if (obj == null) return;

        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in RefreshMethodNames)
        {
            MethodInfo m = t.GetMethod(name, flags, null, Type.EmptyTypes, null);
            if (m == null) continue;

            try
            {
                m.Invoke(obj, null);
                return;
            }
            catch
            {
            }
        }
    }

    bool TrySpendCoinsOnStaticCoinType(int cost)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly asm in assemblies)
        {
            Type[] types;

            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }
            catch
            {
                continue;
            }

            if (types == null) continue;

            foreach (Type t in types)
            {
                if (t == null) continue;

                string typeName = t.Name.ToLowerInvariant();
                if (!typeName.Contains("coin") && !typeName.Contains("wallet") && !typeName.Contains("money"))
                    continue;

                if (TrySpendCoinsOnStaticType(t, cost))
                    return true;
            }
        }

        return false;
    }

    bool TrySpendCoinsOnStaticType(Type t, int cost)
    {
        BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in TrySpendMethodNames)
        {
            foreach (MethodInfo m in t.GetMethods(flags))
            {
                if (m.Name != name) continue;

                ParameterInfo[] p = m.GetParameters();

                try
                {
                    object ret;

                    if (p.Length == 1 && IsNumberType(p[0].ParameterType))
                    {
                        ret = m.Invoke(null, new object[] { Convert.ChangeType(cost, p[0].ParameterType) });
                    }
                    else if (p.Length == 0)
                    {
                        ret = m.Invoke(null, null);
                    }
                    else
                    {
                        continue;
                    }

                    if (m.ReturnType == typeof(bool))
                        return ret is bool b && b;

                    return true;
                }
                catch
                {
                }
            }
        }

        foreach (string name in CoinFieldNames)
        {
            FieldInfo f = t.GetField(name, flags);
            if (f != null && IsNumberType(f.FieldType) && !f.IsInitOnly)
            {
                int current = Convert.ToInt32(f.GetValue(null));
                if (current < cost) return false;

                f.SetValue(null, Convert.ChangeType(current - cost, f.FieldType));
                return true;
            }

            PropertyInfo p = t.GetProperty(name, flags);
            if (p != null && p.CanRead && p.CanWrite && IsNumberType(p.PropertyType))
            {
                int current = Convert.ToInt32(p.GetValue(null, null));
                if (current < cost) return false;

                p.SetValue(null, Convert.ChangeType(current - cost, p.PropertyType), null);
                return true;
            }
        }

        return false;
    }

    bool TryHealTarget(GameObject target, int amount, out bool alreadyFull)
    {
        alreadyFull = false;
        if (target == null) return false;

        HashSet<MonoBehaviour> checkedComponents = new HashSet<MonoBehaviour>();

        MonoBehaviour[] selfComponents = target.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour mb in selfComponents)
        {
            if (TryHealComponent(mb, amount, out alreadyFull))
                return true;

            if (mb != null)
                checkedComponents.Add(mb);
        }

        MonoBehaviour[] parentComponents = target.GetComponentsInParent<MonoBehaviour>(true);
        foreach (MonoBehaviour mb in parentComponents)
        {
            if (mb == null || checkedComponents.Contains(mb)) continue;

            if (TryHealComponent(mb, amount, out alreadyFull))
                return true;

            checkedComponents.Add(mb);
        }

        return false;
    }

    bool TryHealComponent(MonoBehaviour mb, int amount, out bool alreadyFull)
    {
        alreadyFull = false;
        if (mb == null) return false;

        if (TryCallHealMethod(mb, amount))
        {
            RefreshHpObject(mb);
            return true;
        }

        if (!TryReadHp(mb, CurrentHpNames, out int currentHp))
            return false;

        int maxHp = currentHp + amount;
        bool hasMax = TryReadHp(mb, MaxHpNames, out maxHp);

        if (hasMax && currentHp >= maxHp)
        {
            alreadyFull = true;
            RefreshHpObject(mb);
            return true;
        }

        int newHp = hasMax ? Mathf.Min(maxHp, currentHp + amount) : currentHp + amount;

        if (!TryWriteHp(mb, CurrentHpNames, newHp))
            return false;

        RefreshHpObject(mb);
        TryRefreshHpBarField(mb, newHp, hasMax ? maxHp : newHp);

        return true;
    }

    bool TryCallHealMethod(object obj, int amount)
    {
        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in HealMethodNames)
        {
            foreach (MethodInfo m in t.GetMethods(flags))
            {
                if (m.Name != name) continue;

                ParameterInfo[] p = m.GetParameters();

                try
                {
                    if (p.Length == 1 && IsNumberType(p[0].ParameterType))
                    {
                        m.Invoke(obj, new object[] { Convert.ChangeType(amount, p[0].ParameterType) });
                        return true;
                    }

                    if (p.Length == 0)
                    {
                        m.Invoke(obj, null);
                        return true;
                    }
                }
                catch
                {
                }
            }
        }

        return false;
    }

    bool TryReadHp(object obj, string[] names, out int value)
    {
        value = 0;
        if (obj == null) return false;

        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in names)
        {
            FieldInfo f = t.GetField(name, flags);
            if (f != null && IsNumberType(f.FieldType))
            {
                value = Convert.ToInt32(f.GetValue(obj));
                return true;
            }

            PropertyInfo p = t.GetProperty(name, flags);
            if (p != null && p.CanRead && IsNumberType(p.PropertyType))
            {
                value = Convert.ToInt32(p.GetValue(obj, null));
                return true;
            }
        }

        return false;
    }

    bool TryWriteHp(object obj, string[] names, int value)
    {
        if (obj == null) return false;

        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in names)
        {
            FieldInfo f = t.GetField(name, flags);
            if (f != null && IsNumberType(f.FieldType) && !f.IsInitOnly)
            {
                f.SetValue(obj, Convert.ChangeType(value, f.FieldType));
                return true;
            }

            PropertyInfo p = t.GetProperty(name, flags);
            if (p != null && p.CanWrite && IsNumberType(p.PropertyType))
            {
                p.SetValue(obj, Convert.ChangeType(value, p.PropertyType), null);
                return true;
            }
        }

        return false;
    }

    void RefreshHpObject(object obj)
    {
        if (obj == null) return;

        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string name in new[]
        {
            "Refresh", "RefreshUI", "UpdateUI", "UpdateHpUI", "UpdateHPUI",
            "UpdateHpBar", "UpdateHPBar", "RefreshHpBar", "RefreshHPBar"
        })
        {
            MethodInfo m = t.GetMethod(name, flags, null, Type.EmptyTypes, null);
            if (m == null) continue;

            try
            {
                m.Invoke(obj, null);
                return;
            }
            catch
            {
            }
        }
    }

    void TryRefreshHpBarField(object obj, int current, int max)
    {
        if (obj == null) return;

        Type t = obj.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (string fieldName in HpBarFieldNames)
        {
            FieldInfo f = t.GetField(fieldName, flags);
            if (f == null) continue;

            object hpBar = f.GetValue(obj);
            if (hpBar == null) continue;

            if (TryCallHpBarMethod(hpBar, current, max))
                return;
        }
    }

    bool TryCallHpBarMethod(object hpBar, int current, int max)
    {
        if (hpBar == null) return false;

        Type t = hpBar.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        string[] methodNames =
        {
            "SetHp", "SetHP", "SetHealth", "SetValue", "UpdateHp", "UpdateHP", "UpdateHealth", "Refresh"
        };

        foreach (string name in methodNames)
        {
            foreach (MethodInfo m in t.GetMethods(flags))
            {
                if (m.Name != name) continue;

                ParameterInfo[] p = m.GetParameters();

                try
                {
                    if (p.Length == 2 && IsNumberType(p[0].ParameterType) && IsNumberType(p[1].ParameterType))
                    {
                        m.Invoke(hpBar, new object[]
                        {
                            Convert.ChangeType(current, p[0].ParameterType),
                            Convert.ChangeType(max, p[1].ParameterType)
                        });

                        return true;
                    }

                    if (p.Length == 1 && IsNumberType(p[0].ParameterType))
                    {
                        m.Invoke(hpBar, new object[] { Convert.ChangeType(current, p[0].ParameterType) });
                        return true;
                    }

                    if (p.Length == 0)
                    {
                        m.Invoke(hpBar, null);
                        return true;
                    }
                }
                catch
                {
                }
            }
        }

        return false;
    }

    void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        if (messageRoot != null)
        {
            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);

            messageCoroutine = StartCoroutine(MessageRoutine());
        }
    }

    IEnumerator MessageRoutine()
    {
        messageRoot.SetActive(true);
        yield return new WaitForSecondsRealtime(messageSeconds);
        messageRoot.SetActive(false);
        messageCoroutine = null;
    }

    void CleanupMissingPlayers()
    {
        for (int i = playersInRange.Count - 1; i >= 0; i--)
        {
            if (playersInRange[i] == null || !playersInRange[i].gameObject.activeInHierarchy)
                playersInRange.RemoveAt(i);
        }

        if (activeBuyer == null || !playersInRange.Contains(activeBuyer))
            activeBuyer = playersInRange.Count > 0 ? playersInRange[playersInRange.Count - 1] : null;
    }

    bool IsNumberType(Type type)
    {
        return type == typeof(int) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(long) ||
               type == typeof(short) ||
               type == typeof(byte);
    }
}