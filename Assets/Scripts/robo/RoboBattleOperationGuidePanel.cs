using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RoboBattleOperationGuidePanel : MonoBehaviour
{
    [Header("Panel")]
    public GameObject guidePanelRoot;
    public bool autoCreatePanelIfMissing = true;
    public bool autoCreateContents = true;

    [Header("Controller Image")]
    [Tooltip("Project内のSprite画像を入れる場所。未設定でも、Controller Image側に画像が入っていれば消さない。")]
    public Sprite controllerSprite;

    [Header("Text Contents")]
    public string titleText = "ロボバトル 操作説明";

    [TextArea(4, 8)]
    public string commentText =
        "右上は、ボスの体力。左側には、プレイヤーの体力と、弾の残弾。\n" +
        "弾は時間が経つと回復します。\n" +
        "ミサイルは撃ち落すことができます。\n" +
        "パンチはボスが接近した時にパンチで撃退するときのみ使います。";

    [TextArea(6, 12)]
    public string operationText =
        "ジョイパッド・方向キー：弾のカーソル移動\n" +
        "South（A）ボタン：弾をうつ\n" +
        "West（X）ボタン：左パンチ\n" +
        "East（B）ボタン：右パンチ\n" +
        "Leftトリガー：左ガード\n" +
        "Rightトリガー：右ガード";

    public string holdGuideText = "Southボタン長押しでスタート";

    [Header("UI References")]
    public Image controllerImage;
    public TextMeshProUGUI titleTextUI;
    public TextMeshProUGUI commentTextUI;
    public TextMeshProUGUI operationTextUI;
    public TextMeshProUGUI holdText;
    public Slider holdSlider;

    [Header("Hold Start")]
    public float holdSeconds = 1.0f;
    public bool allowKeyboardSpaceDebug = false;

    [Header("Pause")]
    public bool pauseWithTimeScale = true;
    public MonoBehaviour[] disableWhileOpen;

    [Header("Debug")]
    public bool debugLog = true;

    float previousTimeScale = 1f;
    float holdTimer = 0f;
    bool isOpen = false;
    bool started = false;

    void Start()
    {
        OpenGuide();
    }

    void Update()
    {
        if (!isOpen || started)
            return;

        bool holdingSouth = Gamepad.current != null && Gamepad.current.buttonSouth.isPressed;
        bool holdingKeyboard = allowKeyboardSpaceDebug && Keyboard.current != null && Keyboard.current.spaceKey.isPressed;

        if (holdingSouth || holdingKeyboard)
        {
            holdTimer += Time.unscaledDeltaTime;

            float rate = holdSeconds > 0f ? Mathf.Clamp01(holdTimer / holdSeconds) : 1f;

            if (holdSlider)
                holdSlider.value = rate;

            if (holdText)
                holdText.text = holdGuideText;

            if (holdTimer >= holdSeconds)
                CloseGuideAndStart();
        }
        else
        {
            holdTimer = 0f;

            if (holdSlider)
                holdSlider.value = 0f;

            if (holdText)
                holdText.text = holdGuideText;
        }
    }

    void OpenGuide()
    {
        if (started)
            return;

        if (guidePanelRoot == null && autoCreatePanelIfMissing)
            CreateFullPanel();

        if (guidePanelRoot == null)
        {
            Debug.LogWarning("[RoboBattleOperationGuidePanel] Guide Panel Root がありません。");
            return;
        }

        if (autoCreateContents)
            CreateMissingContents();

        ApplyTextsAndImage();

        guidePanelRoot.SetActive(true);

        previousTimeScale = Time.timeScale;

        if (pauseWithTimeScale)
            Time.timeScale = 0f;

        if (disableWhileOpen != null)
        {
            foreach (MonoBehaviour mb in disableWhileOpen)
            {
                if (mb)
                    mb.enabled = false;
            }
        }

        holdTimer = 0f;
        isOpen = true;

        if (holdSlider)
            holdSlider.value = 0f;

        if (debugLog)
            Debug.Log("[RoboBattleOperationGuidePanel] ロボバトル説明パネルを表示しました。");
    }

    void CloseGuideAndStart()
    {
        if (started)
            return;

        started = true;
        isOpen = false;

        if (guidePanelRoot)
            guidePanelRoot.SetActive(false);

        if (pauseWithTimeScale)
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;

        if (disableWhileOpen != null)
        {
            foreach (MonoBehaviour mb in disableWhileOpen)
            {
                if (mb)
                    mb.enabled = true;
            }
        }

        if (debugLog)
            Debug.Log("[RoboBattleOperationGuidePanel] 説明パネルを閉じてロボバトルを開始しました。");
    }

    void ApplyTextsAndImage()
    {
        if (titleTextUI)
            titleTextUI.text = titleText;

        if (commentTextUI)
            commentTextUI.text = commentText;

        if (operationTextUI)
            operationTextUI.text = operationText;

        if (holdText)
            holdText.text = holdGuideText;

        if (controllerImage)
        {
            if (controllerSprite != null)
                controllerImage.sprite = controllerSprite;

            controllerImage.preserveAspect = true;

            if (controllerImage.sprite != null)
                controllerImage.enabled = true;
        }
    }

    void CreateFullPanel()
    {
        GameObject canvasObj = new GameObject("RoboBattleOperationGuideCanvas");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("RoboBattleOperationGuidePanel");
        panel.transform.SetParent(canvasObj.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.78f);

        guidePanelRoot = panel;
    }

    void CreateMissingContents()
    {
        RectTransform rootRect = guidePanelRoot.GetComponent<RectTransform>();
        if (rootRect == null)
            rootRect = guidePanelRoot.AddComponent<RectTransform>();

        Image rootImage = guidePanelRoot.GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = guidePanelRoot.AddComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.78f);
        }

        if (titleTextUI == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "RoboBattleGuideTitleText");
            titleTextUI = found ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        if (titleTextUI == null)
            titleTextUI = CreateTMP("RoboBattleGuideTitleText", guidePanelRoot.transform, new Vector2(0f, 270f), new Vector2(1000f, 70f), 42, TextAlignmentOptions.Center);

        if (commentTextUI == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "RoboBattleGuideCommentText");
            commentTextUI = found ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        if (commentTextUI == null)
            commentTextUI = CreateTMP("RoboBattleGuideCommentText", guidePanelRoot.transform, new Vector2(0f, 165f), new Vector2(1100f, 160f), 25, TextAlignmentOptions.Center);

        if (controllerImage == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "RoboBattleGuideControllerImage");
            controllerImage = found ? found.GetComponent<Image>() : null;
        }

        if (controllerImage == null)
        {
            GameObject obj = new GameObject("RoboBattleGuideControllerImage");
            obj.transform.SetParent(guidePanelRoot.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-270f, -35f);
            rect.sizeDelta = new Vector2(420f, 310f);

            controllerImage = obj.AddComponent<Image>();
            controllerImage.preserveAspect = true;

            if (controllerSprite != null)
                controllerImage.sprite = controllerSprite;
        }

        if (operationTextUI == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "RoboBattleGuideOperationText");
            operationTextUI = found ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        if (operationTextUI == null)
            operationTextUI = CreateTMP("RoboBattleGuideOperationText", guidePanelRoot.transform, new Vector2(300f, -25f), new Vector2(530f, 330f), 27, TextAlignmentOptions.Left);

        if (holdText == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "RoboBattleGuideHoldText");
            holdText = found ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        if (holdText == null)
            holdText = CreateTMP("RoboBattleGuideHoldText", guidePanelRoot.transform, new Vector2(0f, -275f), new Vector2(800f, 60f), 30, TextAlignmentOptions.Center);

        if (holdSlider == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "RoboBattleGuideHoldSlider");
            holdSlider = found ? found.GetComponent<Slider>() : null;
        }

        if (holdSlider == null)
            holdSlider = CreateSlider("RoboBattleGuideHoldSlider", guidePanelRoot.transform, new Vector2(0f, -325f), new Vector2(650f, 22f));
    }

    TextMeshProUGUI CreateTMP(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;

        return tmp;
    }

    Slider CreateSlider(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject sliderObj = new GameObject(objectName);
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = anchoredPosition;
        sliderRect.sizeDelta = size;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.interactable = false;

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);

        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.2f);

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);

        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(3f, 3f);
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);

        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(1f, 1f, 1f, 0.85f);

        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;

        return slider;
    }

    Transform FindChildDeep(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindChildDeep(child, childName);
            if (found)
                return found;
        }

        return null;
    }
}