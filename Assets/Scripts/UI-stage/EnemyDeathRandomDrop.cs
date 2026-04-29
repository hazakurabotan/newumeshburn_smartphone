using System;
using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyDeathRandomDrop : MonoBehaviour
{
    [Header("Drop Prefabs")]
    public GameObject coinPrefab;
    public GameObject healPrefab;

    [Header("Drop Settings")]
    [Range(0f, 1f)] public float dropChance = 1f;
    [Range(0f, 1f)] public float healDropRate = 0.25f;

    [Header("Spawn")]
    public Vector2 spawnOffset = new Vector2(0f, 0.15f);
    public float scatterX = 0.25f;

    private Component enemyComponent;
    private FieldInfo hpField;
    private PropertyInfo hpProperty;
    private bool dropped;

    private static bool isQuitting;

    private static readonly string[] HpMemberNames =
    {
        "hp",
        "HP",
        "currentHp",
        "currentHP",
        "currentHealth",
        "health"
    };

    private void Awake()
    {
        CacheEnemyComponent();
    }

    private void LateUpdate()
    {
        if (dropped) return;

        if (IsDead())
        {
            SpawnDrop();
        }
    }

    private void OnDestroy()
    {
        if (dropped) return;
        if (!Application.isPlaying) return;
        if (isQuitting) return;

        if (IsDead())
        {
            SpawnDrop();
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void CacheEnemyComponent()
    {
        if (enemyComponent != null) return;

        enemyComponent = GetComponent("Enemy");
        if (enemyComponent == null) return;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        Type type = enemyComponent.GetType();

        for (int i = 0; i < HpMemberNames.Length; i++)
        {
            hpField = type.GetField(HpMemberNames[i], flags);
            if (hpField != null) return;
        }

        for (int i = 0; i < HpMemberNames.Length; i++)
        {
            hpProperty = type.GetProperty(HpMemberNames[i], flags);
            if (hpProperty != null && hpProperty.CanRead) return;
        }
    }

    private bool IsDead()
    {
        CacheEnemyComponent();
        if (enemyComponent == null) return false;

        float hp = ReadHpValue();
        return hp <= 0f;
    }

    private float ReadHpValue()
    {
        try
        {
            object value = null;

            if (hpField != null)
                value = hpField.GetValue(enemyComponent);
            else if (hpProperty != null)
                value = hpProperty.GetValue(enemyComponent);

            if (value == null) return 1f;

            return Convert.ToSingle(value);
        }
        catch
        {
            return 1f;
        }
    }

    private void SpawnDrop()
    {
        if (dropped) return;
        dropped = true;

        if (UnityEngine.Random.value > dropChance) return;

        GameObject prefabToDrop = ChooseDropPrefab();
        if (prefabToDrop == null) return;

        Vector3 pos = transform.position + (Vector3)spawnOffset;
        pos.x += UnityEngine.Random.Range(-scatterX, scatterX);

        GameObject droppedItem = Instantiate(prefabToDrop, pos, Quaternion.identity);

        Rigidbody2D rb = droppedItem.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private GameObject ChooseDropPrefab()
    {
        bool hasCoin = coinPrefab != null;
        bool hasHeal = healPrefab != null;

        if (!hasCoin && !hasHeal) return null;
        if (hasCoin && !hasHeal) return coinPrefab;
        if (!hasCoin && hasHeal) return healPrefab;

        return UnityEngine.Random.value < healDropRate ? healPrefab : coinPrefab;
    }
}