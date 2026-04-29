// MaskItemButton.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class MaskItemButton : MonoBehaviour, ISelectHandler
{
    [SerializeField] Image icon;   // 子のImage
    [SerializeField] Image frame;  // ふち取り用（任意、無ければnullでOK）
    Button button;
    MaskItem data;
    Action onClick;

    void Awake() { button = GetComponent<Button>(); }

    public void Setup(MaskItem item, Action click)
    {
        data = item;
        onClick = click;
        if (icon) icon.sprite = item.icon;
        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    public void SetEquipped(bool equipped)
    {
        if (frame) frame.enabled = equipped;
    }

    // 選択されたらハイライト（任意）
    public void OnSelect(BaseEventData e)
    {
        if (frame) frame.enabled = true;
    }
}
