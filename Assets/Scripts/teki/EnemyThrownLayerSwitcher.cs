using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 「通常は Enemy レイヤー」「投げられ中(isFlying=true)だけ EnemyThrown レイヤー」へ切り替える。
/// これにより、Enemy×EnemyをPhysics2DでOFFにしても、投げられ中だけ敵同士に当たる。
/// </summary>
public class EnemyThrownLayerSwitcher : MonoBehaviour
{
    [Header("Layer names")]
    public string normalLayerName = "Enemy";
    public string thrownLayerName = "EnemyThrown";

    [Header("Apply to children too")]
    public bool applyToChildren = true;

    [Header("Enemy flying flag name candidates (auto)")]
    public string[] flyingNames = new string[] { "isFlying", "IsFlying", "flying", "IsThrown", "isThrown" };

    Component enemyComp;
    int normalLayer = -1;
    int thrownLayer = -1;

    bool lastFlying;

    void Awake()
    {
        // Enemyコンポーネントを探す（class Enemy を想定）
        enemyComp = GetComponent("Enemy") as Component;
        normalLayer = LayerMask.NameToLayer(normalLayerName);
        thrownLayer = LayerMask.NameToLayer(thrownLayerName);

        if (normalLayer < 0)
            Debug.LogWarning($"[EnemyThrownLayerSwitcher] normalLayer '{normalLayerName}' が見つかりません。Tags&Layersを確認してね。");
        if (thrownLayer < 0)
            Debug.LogWarning($"[EnemyThrownLayerSwitcher] thrownLayer '{thrownLayerName}' が見つかりません。Tags&Layersを確認してね。");

        lastFlying = ReadFlying(enemyComp);
        ApplyLayer(lastFlying);
    }

    void Update()
    {
        bool flying = ReadFlying(enemyComp);
        if (flying == lastFlying) return;

        lastFlying = flying;
        ApplyLayer(flying);
    }

    bool ReadFlying(Component c)
    {
        if (c == null) return false;

        var t = c.GetType();
        const BindingFlags F = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        foreach (var name in flyingNames)
        {
            // field
            var fi = t.GetField(name, F);
            if (fi != null && fi.FieldType == typeof(bool))
                return (bool)fi.GetValue(c);

            // property
            var pi = t.GetProperty(name, F);
            if (pi != null && pi.PropertyType == typeof(bool) && pi.CanRead)
                return (bool)pi.GetValue(c);
        }
        return false;
    }

    void ApplyLayer(bool flying)
    {
        int target = flying ? thrownLayer : normalLayer;
        if (target < 0) return;

        if (applyToChildren)
        {
            SetLayerRecursively(transform, target);
        }
        else
        {
            gameObject.layer = target;
        }
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }
}