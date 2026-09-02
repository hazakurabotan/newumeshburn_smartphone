using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ImpactRunStartGuidePanel : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("説明パネル本体。既にCanvas内に作っているPanelを入れてOK。")]
    public GameObject guidePanelRoot;

    [Tooltip("Guide Panel Root が空ならCanvasごと自動生成します。")]
    public bool autoCreatePanelIfMissing = true;

    [Tooltip("Guide Panel Root の中に、タイトル・説明文・画像・操作説明が無い場合、自動で作ります。")]
    public bool autoCreateContents = true;

    [Header("Controller Image")]
    [Tooltip("説明パネルに表示するコントローラー画像。Texture TypeをSpriteにしてから入れてください。")]
    public Sprite controllerSprite;

    [Header("Text Contents")]
    public string titleText = "Impact Run 操作説明";

    [TextArea(3, 6)]
    public string commentText =
        "ゴールまでの１５０ｍの間、敵を倒してエネルギーをためつつ駆け抜ける。\n" +
        "道に落ちたり敵にぶつかると、左上のＨＰ５０が減った状態でロボ戦になる。";

    [TextArea(4, 8)]
    public string operationText =
        "左スティック / 十字キー：移動\n" +
        "A / South：ジャンプ\n" +
        "X / West：攻撃\n" +
        "B / East：ダッシュ";

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

    [Tooltip("TimeScaleだけで止まらないスクリプトがあれば入れる。基本は空でOK。")]
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
            {
                CloseGuideAndStart();
            }
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
            Debug.LogWarning("[ImpactRunStartGuidePanel] Guide Panel Root がありません。");
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
            Debug.Log("[ImpactRunStartGuidePanel] 開始説明パネルを表示しました。");
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
            Debug.Log("[ImpactRunStartGuidePanel] 説明パネルを閉じてImpactRunを開始しました。");
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
            controllerImage.sprite = controllerSprite;
            controllerImage.enabled = controllerSprite != null;
            controllerImage.preserveAspect = true;
        }
    }

    void CreateFullPanel()
    {
        GameObject canvasObj = new GameObject("ImpactRunStartGuideCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("ImpactRunStartGuidePanel");
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
            Transform found = FindChildDeep(guidePanelRoot.transform, "GuideTitleText");
            titleTextUI = found ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        if (titleTextUI == null)
            titleTextUI = CreateTMP("GuideTitleText", guidePanelRoot.transform, new Vector2(0f, 270f), new Vector2(900f, 70f), 42, TextAlignmentOptions.Center);

        if (commentTextUI == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "GuideCommentText");
            commentTextUI = found ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        if (commentTextUI == null)
            commentTextUI = CreateTMP("GuideCommentText", guidePanelRoot.transform, new Vector2(0f, 180f), new Vector2(960f, 120f), 25, TextAlignmentOptions.Center);

        if (controllerImage == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "GuideControllerImage");
            controllerImage = found ? found.GetComponent<Image>() : null;
        }

        if (controllerImage == null)
        {
            GameObject obj = new GameObject("GuideControllerImage");
            obj.transform.SetParent(guidePanelRoot.transform, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-260f, -40f);
            rect.sizeDelta = new Vector2(420f, 310f);

            controllerImage = obj.AddComponent<Image>();
            controllerImage.preserveAspect = true;
        }

        if (operationTextUI == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "GuideOperationText");
            operationTextUI = found ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        if (operationTextUI == null)
            operationTextUI = CreateTMP("GuideOperationText", guidePanelRoot.transform, new Vector2(270f, -35f), new Vector2(430f, 270f), 27, TextAlignmentOptions.Left);

        if (holdText == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "GuideHoldText");
            holdText = found ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        if (holdText == null)
            holdText = CreateTMP("GuideHoldText", guidePanelRoot.transform, new Vector2(0f, -275f), new Vector2(800f, 60f), 30, TextAlignmentOptions.Center);

        if (holdSlider == null)
        {
            Transform found = FindChildDeep(guidePanelRoot.transform, "GuideHoldSlider");
            holdSlider = found ? found.GetComponent<Slider>() : null;
        }

        if (holdSlider == null)
            holdSlider = CreateSlider("GuideHoldSlider", guidePanelRoot.transform, new Vector2(0f, -325f), new Vector2(650f, 22f));
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