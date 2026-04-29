// MaskManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MaskItem
{
    public string id;         // 一意なID（日本語でも可、被らなければOK）
    public Sprite icon;       // 表示用スプライト
}

public class MaskManager : MonoBehaviour
{
    [SerializeField] List<MaskItem> owned = new();  // インスペクタで登録
    [SerializeField] string prefsKey = "EQUIPPED_MASK_ID";

    public event Action<MaskItem> OnEquippedChanged;

    public IReadOnlyList<MaskItem> Owned => owned;
    public MaskItem Equipped { get; private set; }

    void Awake()
    {
        // 保存から読み込み
        string id = PlayerPrefs.GetString(prefsKey, "");
        Equipped = string.IsNullOrEmpty(id) ? (owned.Count > 0 ? owned[0] : null) : Find(id);
    }

    MaskItem Find(string id) => owned.Find(m => m.id == id);

    public void Equip(string id)
    {
        var item = Find(id);
        if (item == null || item == Equipped) return;
        Equipped = item;
        PlayerPrefs.SetString(prefsKey, item.id);
        OnEquippedChanged?.Invoke(item);
    }
}
