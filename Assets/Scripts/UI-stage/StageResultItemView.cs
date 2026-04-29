using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageResultItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;

    public void SetData(string itemName, Sprite icon, int count)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (nameText != null)
        {
            nameText.text = string.IsNullOrWhiteSpace(itemName) ? "" : itemName;
        }

        if (countText != null)
        {
            countText.text = "x" + Mathf.Max(1, count).ToString();
        }
    }
}