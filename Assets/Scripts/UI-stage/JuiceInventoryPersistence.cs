using System;
using System.Collections.Generic;
using UnityEngine;

public class JuiceInventoryPersistence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private JuiceInventory juiceInventory;

    [Header("Save")]
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool saveOnInventoryChanged = true;
    [SerializeField] private string saveKey = "GirlsGear_OwnedJuices";

    [Header("Debug")]
    [SerializeField] private string savedPreview = "";

    private bool isLoading = false;
    private bool isHooked = false;

    private void Awake()
    {
        ResolveInventory();
    }

    private void OnEnable()
    {
        ResolveInventory();
        Hook();
    }

    private void Start()
    {
        ResolveInventory();
        Hook();

        if (loadOnStart)
        {
            LoadOwnedJuices();
        }
    }

    private void OnDisable()
    {
        Unhook();
    }

    private void ResolveInventory()
    {
        if (juiceInventory != null)
            return;

        juiceInventory = GetComponent<JuiceInventory>();

        if (juiceInventory == null)
            juiceInventory = JuiceInventory.Instance;

        if (juiceInventory == null)
            juiceInventory = FindObjectOfType<JuiceInventory>();
    }

    private void Hook()
    {
        if (isHooked)
            return;

        if (juiceInventory == null)
            return;

        juiceInventory.OnInventoryChanged += HandleInventoryChanged;
        isHooked = true;
    }

    private void Unhook()
    {
        if (!isHooked)
            return;

        if (juiceInventory != null)
            juiceInventory.OnInventoryChanged -= HandleInventoryChanged;

        isHooked = false;
    }

    private void HandleInventoryChanged()
    {
        if (isLoading)
            return;

        if (!saveOnInventoryChanged)
            return;

        SaveOwnedJuices();
    }

    public void SaveOwnedJuices()
    {
        ResolveInventory();

        if (juiceInventory == null)
        {
            Debug.LogWarning("[JuiceInventoryPersistence] JuiceInventory が見つからないので保存できません。", this);
            return;
        }

        List<string> ids = new List<string>();

        for (int i = 0; i < juiceInventory.Count; i++)
        {
            JuiceInventory.JuiceDefinition def = juiceInventory.GetOwnedDefinitionAt(i);
            if (def == null)
                continue;

            if (string.IsNullOrWhiteSpace(def.id))
                continue;

            ids.Add(def.id);
        }

        string joined = string.Join("|", ids);
        savedPreview = joined;

        PlayerPrefs.SetString(saveKey, joined);
        PlayerPrefs.Save();

        Debug.Log($"[JuiceInventoryPersistence] 保存しました。Count={ids.Count} Data={joined}", this);
    }

    public void LoadOwnedJuices()
    {
        ResolveInventory();

        if (juiceInventory == null)
        {
            Debug.LogWarning("[JuiceInventoryPersistence] JuiceInventory が見つからないので読み込みできません。", this);
            return;
        }

        if (!PlayerPrefs.HasKey(saveKey))
        {
            savedPreview = "";
            Debug.Log("[JuiceInventoryPersistence] 保存データがまだ無いので読み込みません。", this);
            return;
        }

        string joined = PlayerPrefs.GetString(saveKey, string.Empty);
        savedPreview = joined;

        isLoading = true;

        juiceInventory.ClearAll();

        if (!string.IsNullOrWhiteSpace(joined))
        {
            string[] ids = joined.Split('|');

            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[i];

                if (string.IsNullOrWhiteSpace(id))
                    continue;

                JuiceInventory.JuiceDefinition obtainedDefinition;
                int obtainedDefinitionIndex;

                bool added = juiceInventory.TryAddById(id, out obtainedDefinition, out obtainedDefinitionIndex);

                if (!added)
                {
                    Debug.LogWarning($"[JuiceInventoryPersistence] 読み込み時に追加できないジュースIDがありました: {id}", this);
                }
            }
        }

        isLoading = false;

        Debug.Log($"[JuiceInventoryPersistence] 読み込みました。Count={juiceInventory.Count} Data={joined}", this);
    }

    [ContextMenu("Save Owned Juices Now")]
    private void ContextSaveOwnedJuicesNow()
    {
        SaveOwnedJuices();
    }

    [ContextMenu("Load Owned Juices Now")]
    private void ContextLoadOwnedJuicesNow()
    {
        LoadOwnedJuices();
    }

    [ContextMenu("Delete Saved Inventory")]
    private void ContextDeleteSavedInventory()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();

        savedPreview = "";

        Debug.Log("[JuiceInventoryPersistence] 保存済みの缶ジュース所持データを削除しました。", this);
    }
}