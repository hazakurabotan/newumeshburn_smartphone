using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JuicePopupUI : MonoBehaviour
{
    [Header("Root")]
    public GameObject rootPanel;

    [Header("UI")]
    public Image itemIcon;
    public TMP_Text titleTextTMP;
    public TMP_Text descriptionTextTMP;
    public Text titleTextLegacy;
    public Text descriptionTextLegacy;

    [Header("Timing")]
    public float displaySeconds = 2.0f;

    private Coroutine popupCoroutine;

    private void Awake()
    {
        if (rootPanel == null)
            rootPanel = gameObject;

        HideImmediate();
    }

    public void ShowJuice(JuiceInventory.JuiceDefinition definition)
    {
        if (definition == null) return;

        string title = definition.displayName + " ÇéËÇ…ì¸ÇÍÇΩÅI";
        string description = definition.description;
        Show(definition.icon, title, description);
    }

    public void ShowMessage(string title, string description)
    {
        Show(null, title, description);
    }

    public void Show(Sprite icon, string title, string description)
    {
        if (rootPanel == null)
            rootPanel = gameObject;

        if (itemIcon != null)
        {
            if (icon != null)
            {
                itemIcon.sprite = icon;
                itemIcon.enabled = true;
            }
            else
            {
                itemIcon.enabled = false;
            }
        }

        if (titleTextTMP != null) titleTextTMP.text = title;
        if (descriptionTextTMP != null) descriptionTextTMP.text = description;

        if (titleTextLegacy != null) titleTextLegacy.text = title;
        if (descriptionTextLegacy != null) descriptionTextLegacy.text = description;

        rootPanel.SetActive(true);

        if (popupCoroutine != null)
            StopCoroutine(popupCoroutine);

        popupCoroutine = StartCoroutine(AutoHideRoutine());
    }

    public void HideImmediate()
    {
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
            popupCoroutine = null;
        }

        if (rootPanel == null)
            rootPanel = gameObject;

        rootPanel.SetActive(false);
    }

    private IEnumerator AutoHideRoutine()
    {
        yield return new WaitForSeconds(displaySeconds);
        HideImmediate();
    }
}