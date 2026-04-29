using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageResultPanelController : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Header("Info UI")]
    [SerializeField] private TextMeshProUGUI stageNameText;
    [SerializeField] private TextMeshProUGUI clearTimeText;

    [Header("MD Section")]
    [SerializeField] private GameObject mdSectionRoot;
    [SerializeField] private Transform mdContentRoot;

    [Header("Juice Section")]
    [SerializeField] private GameObject juiceSectionRoot;
    [SerializeField] private Transform juiceContentRoot;

    [Header("Item Prefab Optional")]
    [SerializeField] private bool useGeneratedCells = true;
    [SerializeField] private StageResultItemView itemViewPrefab;

    [Header("Generated Cell Settings")]
    [SerializeField] private Vector2 generatedCellSize = new Vector2(96f, 120f);
    [SerializeField] private Vector2 generatedIconSize = new Vector2(64f, 64f);
    [SerializeField] private bool showItemName = true;
    [SerializeField] private bool showItemCount = true;

    [Header("Generated Text Font")]
    [SerializeField] private TMP_FontAsset generatedTextFont;
    [SerializeField] private bool copyFontFromStageNameText = true;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;

    [Header("Scene Transition")]
    [SerializeField] private bool loadStageSelectOnConfirm = true;
    [SerializeField] private string stageSelectSceneName = "StageSelect";

    [Header("Behavior")]
    [SerializeField] private bool pauseGameWhileOpen = true;
    [SerializeField] private bool hidePanelOnStart = true;
    [SerializeField] private bool clearRecordedItemsOnConfirm = true;

    [Header("Debug")]
    [SerializeField] private bool verboseLog = true;

    private bool isOpen = false;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmPressed);
            confirmButton.onClick.AddListener(OnConfirmPressed);
        }

        if (hidePanelOnStart && panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmPressed);
        }
    }

    public void ShowCurrentResult()
    {
        StageResultSession session = StageResultSession.EnsureInstance();
        StageResultSession.ResultSnapshot snapshot = session.CreateSnapshot();
        Show(snapshot);
    }

    public void Show(StageResultSession.ResultSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogError("[StageResultPanelController] snapshot is null.");
            return;
        }

        isOpen = true;

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (pauseGameWhileOpen)
        {
            Time.timeScale = 0f;
        }

        if (stageNameText != null)
        {
            stageNameText.text = string.IsNullOrWhiteSpace(snapshot.stageName)
                ? "ステージ名なし"
                : snapshot.stageName;
        }

        if (clearTimeText != null)
        {
            clearTimeText.text = StageResultSession.FormatTime(snapshot.clearSeconds);
        }

        BuildSection(mdSectionRoot, mdContentRoot, snapshot.mdItems, "MD");
        BuildSection(juiceSectionRoot, juiceContentRoot, snapshot.juiceItems, "Juice");

        Canvas.ForceUpdateCanvases();

        if (verboseLog)
        {
            Debug.Log(
                "[StageResultPanelController] Show Result " +
                "MD=" + snapshot.mdItems.Count +
                " Juice=" + snapshot.juiceItems.Count +
                " mdChildren=" + GetChildCount(mdContentRoot) +
                " juiceChildren=" + GetChildCount(juiceContentRoot)
            );
        }

        StartCoroutine(SelectConfirmButtonNextFrame());
    }

    public void Hide()
    {
        isOpen = false;

        if (pauseGameWhileOpen)
        {
            Time.timeScale = 1f;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void BuildSection(
        GameObject sectionRoot,
        Transform contentRoot,
        List<StageResultSession.ResultItemData> items,
        string sectionName
    )
    {
        if (contentRoot == null)
        {
            Debug.LogError("[StageResultPanelController] " + sectionName + " ContentRoot が未設定です。");
            return;
        }

        ClearChildren(contentRoot);

        bool hasItems = items != null && items.Count > 0;

        if (sectionRoot != null)
        {
            sectionRoot.SetActive(hasItems);
        }

        if (!hasItems)
        {
            if (verboseLog)
            {
                Debug.Log("[StageResultPanelController] " + sectionName + " section has no acquired items.");
            }

            return;
        }

        contentRoot.gameObject.SetActive(true);

        EnsureContentRootLayout(contentRoot);

        for (int i = 0; i < items.Count; i++)
        {
            StageResultSession.ResultItemData data = items[i];
            if (data == null)
                continue;

            if (useGeneratedCells || itemViewPrefab == null)
            {
                CreateGeneratedCell(contentRoot, data, i, sectionName);
            }
            else
            {
                StageResultItemView view = Instantiate(itemViewPrefab, contentRoot);
                view.gameObject.SetActive(true);
                view.transform.localScale = Vector3.one;

                RectTransform viewRect = view.GetComponent<RectTransform>();
                if (viewRect != null)
                {
                    viewRect.sizeDelta = generatedCellSize;
                }

                view.SetData(data.itemName, data.icon, data.count);
            }
        }

        Canvas.ForceUpdateCanvases();

        if (verboseLog)
        {
            Debug.Log(
                "[StageResultPanelController] " +
                sectionName +
                " generated cells=" +
                contentRoot.childCount
            );
        }
    }

    private void EnsureContentRootLayout(Transform contentRoot)
    {
        if (contentRoot == null)
            return;

        RectTransform rect = contentRoot.GetComponent<RectTransform>();
        if (rect != null)
        {
            if (rect.sizeDelta.x < generatedCellSize.x)
            {
                rect.sizeDelta = new Vector2(420f, Mathf.Max(rect.sizeDelta.y, 260f));
            }

            if (rect.sizeDelta.y < generatedCellSize.y)
            {
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 420f), 260f);
            }

            rect.localScale = Vector3.one;
        }

        GridLayoutGroup grid = contentRoot.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = contentRoot.gameObject.AddComponent<GridLayoutGroup>();
        }

        grid.cellSize = generatedCellSize;
        grid.spacing = new Vector2(8f, 8f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
    }

    private void CreateGeneratedCell(
        Transform parent,
        StageResultSession.ResultItemData data,
        int index,
        string sectionName
    )
    {
        GameObject cell = new GameObject(sectionName + "_ResultItemCell_" + index);
        cell.transform.SetParent(parent, false);
        cell.transform.localScale = Vector3.one;
        cell.SetActive(true);

        RectTransform cellRect = cell.AddComponent<RectTransform>();
        cellRect.sizeDelta = generatedCellSize;

        Image frameImage = cell.AddComponent<Image>();
        frameImage.color = new Color(1f, 1f, 1f, 0.22f);
        frameImage.raycastTarget = false;

        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(cell.transform, false);
        iconObj.transform.localScale = Vector3.one;

        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -8f);
        iconRect.sizeDelta = generatedIconSize;

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;

        if (data.icon != null)
        {
            iconImage.sprite = data.icon;
            iconImage.enabled = true;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.enabled = true;
            iconImage.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        }

        if (showItemName)
        {
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(cell.transform, false);
            nameObj.transform.localScale = Vector3.one;

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0f);
            nameRect.pivot = new Vector2(0.5f, 0f);
            nameRect.anchoredPosition = new Vector2(0f, 24f);
            nameRect.sizeDelta = new Vector2(-8f, 34f);

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            ApplyGeneratedFont(nameText);

            nameText.raycastTarget = false;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontSize = 13f;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 7f;
            nameText.fontSizeMax = 13f;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            nameText.color = Color.white;
            nameText.text = string.IsNullOrWhiteSpace(data.itemName) ? "Item" : data.itemName;
        }

        if (showItemCount)
        {
            GameObject countObj = new GameObject("CountText");
            countObj.transform.SetParent(cell.transform, false);
            countObj.transform.localScale = Vector3.one;

            RectTransform countRect = countObj.AddComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0f, 0f);
            countRect.anchorMax = new Vector2(1f, 0f);
            countRect.pivot = new Vector2(0.5f, 0f);
            countRect.anchoredPosition = new Vector2(0f, 4f);
            countRect.sizeDelta = new Vector2(-8f, 22f);

            TextMeshProUGUI countText = countObj.AddComponent<TextMeshProUGUI>();
            ApplyGeneratedFont(countText);

            countText.raycastTarget = false;
            countText.alignment = TextAlignmentOptions.Center;
            countText.fontSize = 14f;
            countText.enableAutoSizing = false;
            countText.enableWordWrapping = false;
            countText.overflowMode = TextOverflowModes.Overflow;
            countText.color = Color.white;
            countText.text = "x" + Mathf.Max(1, data.count).ToString();
        }

        if (verboseLog)
        {
            Debug.Log(
                "[StageResultPanelController] Created " +
                sectionName +
                " cell: " +
                data.itemName +
                " icon=" +
                (data.icon == null ? "NULL" : data.icon.name)
            );
        }
    }

    private void ApplyGeneratedFont(TextMeshProUGUI text)
    {
        if (text == null)
            return;

        TMP_FontAsset font = ResolveGeneratedFont();
        if (font != null)
        {
            text.font = font;
        }

        Material material = ResolveGeneratedFontMaterial(font);
        if (material != null)
        {
            text.fontSharedMaterial = material;
        }
    }

    private TMP_FontAsset ResolveGeneratedFont()
    {
        if (generatedTextFont != null)
            return generatedTextFont;

        if (copyFontFromStageNameText && stageNameText != null && stageNameText.font != null)
            return stageNameText.font;

        if (clearTimeText != null && clearTimeText.font != null)
            return clearTimeText.font;

        return TMP_Settings.defaultFontAsset;
    }

    private Material ResolveGeneratedFontMaterial(TMP_FontAsset font)
    {
        if (font == null)
            return null;

        if (copyFontFromStageNameText && stageNameText != null && stageNameText.font == font && stageNameText.fontSharedMaterial != null)
            return stageNameText.fontSharedMaterial;

        if (clearTimeText != null && clearTimeText.font == font && clearTimeText.fontSharedMaterial != null)
            return clearTimeText.fontSharedMaterial;

        return null;
    }

    private void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Destroy(root.GetChild(i).gameObject);
        }
    }

    private int GetChildCount(Transform root)
    {
        return root != null ? root.childCount : -1;
    }

    private IEnumerator SelectConfirmButtonNextFrame()
    {
        yield return null;
        yield return null;

        if (confirmButton == null)
            yield break;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
        }
    }

    private void OnConfirmPressed()
    {
        if (!isOpen)
            return;

        isOpen = false;

        if (pauseGameWhileOpen)
        {
            Time.timeScale = 1f;
        }

        if (clearRecordedItemsOnConfirm && StageResultSession.Instance != null)
        {
            StageResultSession.Instance.ClearRecordedItemsOnly();
        }

        if (loadStageSelectOnConfirm)
        {
            SceneManager.LoadScene(stageSelectSceneName);
            return;
        }

        Hide();
    }
}