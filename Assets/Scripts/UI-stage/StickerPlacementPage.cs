using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class StickerPlacementPage : MonoBehaviour
{
    public enum PlacementPart
    {
        Hand = 0,
        Arm = 1
    }

    enum PageState
    {
        None,
        MoveSticker,
        ConfirmDialog,
        ReplaceSelect
    }

    enum ConfirmMode
    {
        None,
        Place,
        ReplaceAsk
    }

    [Serializable]
    public class PartView
    {
        [Header("Root")]
        public GameObject root;

        [Header("Placement Area")]
        public RectTransform moveArea;
        public RectTransform placedRoot;

        [Header("Part Button (Optional)")]
        public Button partButton;

        [Header("Grid")]
        public int columns = 4;
        public int rows = 4;

        [Header("Limit")]
        public int maxPlaced = 2;
    }

    [Serializable]
    class PlacementRecord
    {
        public string stickerId;
        public int col;
        public int row;
        public int order;
    }

    [Serializable]
    class PlacementRecordList
    {
        public List<PlacementRecord> items = new List<PlacementRecord>();
    }

    class PlacedVisual
    {
        public GameObject go;
        public RectTransform rect;
        public Image image;
    }

    [Header("Roots")]
    public GameObject partSelectRoot;

    [Header("Parts")]
    public PartView handView;
    public PartView armView;

    [Header("Preview")]
    public Image currentStickerPreview;
    public float previewAlpha = 0.85f;
    public float previewStickerScale = 2.0f;
    public float placedStickerScale = 2.0f;

    [Header("Texts")]
    public TMP_Text currentStickerNameText;
    public TMP_Text currentStickerEffectText;
    public TMP_Text helpText;
    public TMP_Text countText;

    [Header("Confirm")]
    public GameObject confirmRoot;
    public TMP_Text confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;
    [TextArea(2, 4)] public string placeConfirmMessage = "ここにはりますか？";
    [TextArea(2, 4)] public string replaceConfirmMessage = "張り替えますか？";

    [Header("Input (Optional)")]
    public PlayerInput playerInput;
    public string uiActionMapName = "UI";
    public string navigateActionName = "Navigate";
    public string submitActionName = "Submit";
    public string cancelActionName = "Cancel";

    [Header("Navigate Repeat")]
    public float navigateDeadZone = 0.5f;
    public float firstRepeatDelay = 0.25f;
    public float repeatInterval = 0.12f;

    [Header("Messages")]
    [TextArea(2, 4)] public string moveMessage = "十字キーで位置を動かしてください";
    [TextArea(2, 4)] public string switchPartMessage = "LB/RB または Q/E で部位を切り替え";
    [TextArea(2, 4)] public string replaceSelectMessage = "はがすステッカーを選んでください";
    [TextArea(2, 4)] public string handFullMessage = "手の甲はこれ以上貼れません";
    [TextArea(2, 4)] public string armFullMessage = "腕の部分はこれ以上貼れません";
    [TextArea(2, 4)] public string removedMessage = "ステッカーをはがしました";
    [TextArea(2, 4)] public string placedMessage = "ステッカーを貼りました";

    StickerBookPage ownerBookPage;

    string currentStickerId;
    string currentStickerName;
    string currentStickerEffect;
    Sprite currentStickerSprite;

    PageState pageState = PageState.None;
    ConfirmMode confirmMode = ConfirmMode.None;

    PlacementPart currentPart = PlacementPart.Hand;

    Vector2Int handGridPos = Vector2Int.zero;
    Vector2Int armGridPos = Vector2Int.zero;

    int handReplaceIndex = 0;
    int armReplaceIndex = 0;
    int confirmIndex = 0; // 0 = Yes, 1 = No

    InputAction navigateAction;
    InputAction submitAction;
    InputAction cancelAction;

    Vector2Int lastMoveDir = Vector2Int.zero;
    float nextMoveTime = 0f;
    bool blockSubmitUntilRelease = false;

    readonly Dictionary<PlacementPart, List<PlacedVisual>> spawnedPlacedObjects =
        new Dictionary<PlacementPart, List<PlacedVisual>>();

    public static event Action OnPlacementsChanged;

    void Awake()
    {
        spawnedPlacedObjects[PlacementPart.Hand] = new List<PlacedVisual>();
        spawnedPlacedObjects[PlacementPart.Arm] = new List<PlacedVisual>();

        WireButtons();
        CacheInputActions();
        EnsurePreviewExists();
        HideEverything();
        RefreshAllPlacedImages();
    }

    void OnEnable()
    {
        CacheInputActions();
        EnsurePreviewExists();
        RefreshAllPlacedImages();
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (pageState == PageState.None)
            return;

        HandleInput();
    }

    bool IsUsablePlayerInput(PlayerInput pi)
    {
        return pi != null &&
               pi.enabled &&
               pi.gameObject.activeInHierarchy &&
               pi.actions != null;
    }

    PlayerInput ResolveUsablePlayerInput()
    {
        if (IsUsablePlayerInput(playerInput))
            return playerInput;

        PlayerInput[] all = FindObjectsOfType<PlayerInput>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (IsUsablePlayerInput(all[i]))
            {
                playerInput = all[i];
                return playerInput;
            }
        }

        return null;
    }

    void EnsureUiActionMap()
    {
        PlayerInput pi = ResolveUsablePlayerInput();
        if (pi == null || pi.actions == null)
            return;

        try
        {
            InputActionMap uiMap = pi.actions.FindActionMap(uiActionMapName, false);
            if (uiMap != null)
                uiMap.Enable();
        }
        catch
        {
        }
    }

    void CacheInputActions()
    {
        PlayerInput pi = ResolveUsablePlayerInput();

        navigateAction = null;
        submitAction = null;
        cancelAction = null;

        if (pi == null || pi.actions == null)
            return;

        InputActionMap uiMap = pi.actions.FindActionMap(uiActionMapName, false);
        if (uiMap == null)
            return;

        navigateAction = uiMap.FindAction(navigateActionName, false);
        submitAction = uiMap.FindAction(submitActionName, false);
        cancelAction = uiMap.FindAction(cancelActionName, false);
    }

    void WireButtons()
    {
        if (handView.partButton != null)
        {
            handView.partButton.onClick.RemoveAllListeners();
            handView.partButton.onClick.AddListener(() => SwitchPart(PlacementPart.Hand));
        }

        if (armView.partButton != null)
        {
            armView.partButton.onClick.RemoveAllListeners();
            armView.partButton.onClick.AddListener(() => SwitchPart(PlacementPart.Arm));
        }

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveAllListeners();
            confirmYesButton.onClick.AddListener(OnConfirmYes);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveAllListeners();
            confirmNoButton.onClick.AddListener(OnConfirmNo);
        }
    }

    void EnsurePreviewExists()
    {
        if (currentStickerPreview != null)
            return;

        GameObject go = new GameObject("CurrentStickerPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        currentStickerPreview = go.GetComponent<Image>();
        currentStickerPreview.raycastTarget = false;
        currentStickerPreview.preserveAspect = true;
    }

    public void OpenWithSticker(StickerBookPage bookPage, string stickerId, string stickerName, Sprite stickerSprite, string effectText)
    {
        ownerBookPage = bookPage;
        currentStickerId = stickerId;
        currentStickerName = stickerName;
        currentStickerSprite = stickerSprite;
        currentStickerEffect = effectText;

        BeginPlacement();
    }

    public void OpenWithStickerId(string stickerId)
    {
        ownerBookPage = FindObjectOfType<StickerBookPage>(true);

        currentStickerId = stickerId;
        currentStickerName = string.Empty;
        currentStickerEffect = string.Empty;
        currentStickerSprite = null;

        if (StickerBookPage.TryGetStickerRuntimeData(stickerId, out StickerBookPage.StickerRuntimeData data))
        {
            currentStickerName = data.stickerName;
            currentStickerEffect = data.effectText;
            currentStickerSprite = data.stickerSprite;
        }

        BeginPlacement();
    }

    void BeginPlacement()
    {
        CacheInputActions();
        EnsurePreviewExists();

        if (string.IsNullOrWhiteSpace(currentStickerId) || currentStickerSprite == null)
            return;

        RefreshCurrentStickerInfo();
        RefreshAllPlacedImages();

        if (partSelectRoot != null)
            partSelectRoot.SetActive(true);

        HideConfirmImmediate();

        currentPart = PlacementPart.Hand;
        ClampStoredGrid(ref handGridPos, handView);
        ClampStoredGrid(ref armGridPos, armView);

        ShowCurrentPart();
        pageState = PageState.MoveSticker;
        confirmMode = ConfirmMode.None;

        if (helpText != null)
            helpText.text = moveMessage + "\n" + switchPartMessage;

        RefreshCountText();
        RefreshPreviewVisual();
        RefreshReplaceSelectionVisual();

        lastMoveDir = Vector2Int.zero;
        nextMoveTime = 0f;
        blockSubmitUntilRelease = true;
    }

    void HandleInput()
    {
        if (pageState == PageState.ConfirmDialog)
        {
            HandleConfirmNavigate();

            if (ReadSubmitPressed())
            {
                if (confirmIndex == 0) OnConfirmYes();
                else OnConfirmNo();
            }

            if (ReadCancelPressed())
                OnConfirmNo();

            return;
        }

        if (pageState == PageState.ReplaceSelect)
        {
            HandleReplaceSelect();

            if (ReadSubmitPressed())
                RemoveSelectedPlacedSticker();

            if (ReadCancelPressed())
            {
                pageState = PageState.MoveSticker;
                RefreshReplaceSelectionVisual();
                RefreshPreviewVisual();
                if (helpText != null)
                    helpText.text = moveMessage + "\n" + switchPartMessage;

                blockSubmitUntilRelease = true;
            }

            return;
        }

        if (pageState != PageState.MoveSticker)
            return;

        if (ReadSwitchPrevPartPressed())
        {
            SwitchPart(PlacementPart.Hand);
            return;
        }

        if (ReadSwitchNextPartPressed())
        {
            SwitchPart(PlacementPart.Arm);
            return;
        }

        HandleMoveSticker();

        if (ReadSubmitPressed())
            TryOpenPlaceFlow();

        if (ReadCancelPressed())
            CloseToBook();
    }

    void TryOpenPlaceFlow()
    {
        PartView view = GetCurrentView();
        if (view == null)
            return;

        if (GetPlacementCount(currentPart) >= view.maxPlaced)
        {
            OpenConfirm(ConfirmMode.ReplaceAsk, replaceConfirmMessage);
        }
        else
        {
            OpenConfirm(ConfirmMode.Place, placeConfirmMessage);
        }
    }

    void OpenConfirm(ConfirmMode mode, string message)
    {
        confirmMode = mode;
        pageState = PageState.ConfirmDialog;
        confirmIndex = 0;
        blockSubmitUntilRelease = true;
        lastMoveDir = Vector2Int.zero;
        nextMoveTime = 0f;

        if (confirmRoot != null)
        {
            confirmRoot.SetActive(true);
            confirmRoot.transform.SetAsLastSibling();
        }

        if (confirmText != null)
            confirmText.text = message;

        RefreshConfirmSelection();
    }

    void HandleMoveSticker()
    {
        Vector2Int moveDir = ReadDigitalNavigate();
        if (moveDir == Vector2Int.zero)
            return;

        Vector2Int pos = GetCurrentGridPos();
        PartView view = GetCurrentView();

        if (view == null)
            return;

        pos.x += moveDir.x;
        pos.y -= moveDir.y;

        pos.x = Mathf.Clamp(pos.x, 0, Mathf.Max(0, view.columns - 1));
        pos.y = Mathf.Clamp(pos.y, 0, Mathf.Max(0, view.rows - 1));

        SetCurrentGridPos(pos);
        RefreshPreviewVisual();
    }

    void HandleReplaceSelect()
    {
        Vector2Int moveDir = ReadDigitalNavigate();
        if (moveDir == Vector2Int.zero)
            return;

        MoveReplaceSelection(moveDir.x, moveDir.y);
    }

    void HandleConfirmNavigate()
    {
        Vector2Int moveDir = ReadDigitalNavigate();
        if (moveDir == Vector2Int.zero)
            return;

        if (moveDir.x != 0 || moveDir.y != 0)
        {
            confirmIndex = 1 - confirmIndex;
            RefreshConfirmSelection();
        }
    }

    Vector2Int ReadDigitalNavigate()
    {
        Vector2 input = ReadNavigateVector();
        Vector2Int moveDir = ConvertNavigateToDigital(input);

        if (moveDir == Vector2Int.zero)
        {
            lastMoveDir = Vector2Int.zero;
            return Vector2Int.zero;
        }

        float now = Time.unscaledTime;
        bool firstPress = lastMoveDir == Vector2Int.zero;
        bool changedDir = moveDir != lastMoveDir;
        bool repeatReady = now >= nextMoveTime;

        if (firstPress || changedDir || repeatReady)
        {
            lastMoveDir = moveDir;
            nextMoveTime = now + (firstPress || changedDir ? firstRepeatDelay : repeatInterval);
            return moveDir;
        }

        return Vector2Int.zero;
    }

    Vector2 ReadNavigateVector()
    {
        if (navigateAction != null)
        {
            Vector2 actionValue = navigateAction.ReadValue<Vector2>();
            if (actionValue.sqrMagnitude > 0.0001f)
                return actionValue;
        }

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 dpad = pad.dpad.ReadValue();
            if (dpad.sqrMagnitude > 0.0001f)
                return dpad;

            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.0001f)
                return stick;
        }

        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            float x = 0f;
            float y = 0f;

            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) x -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) x += 1f;
            if (kb.downArrowKey.isPressed || kb.sKey.isPressed) y -= 1f;
            if (kb.upArrowKey.isPressed || kb.wKey.isPressed) y += 1f;

            return new Vector2(x, y);
        }

        return Vector2.zero;
    }

    Vector2Int ConvertNavigateToDigital(Vector2 input)
    {
        if (input.magnitude < navigateDeadZone)
            return Vector2Int.zero;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return input.x < 0f ? new Vector2Int(-1, 0) : new Vector2Int(1, 0);

        return input.y > 0f ? new Vector2Int(0, 1) : new Vector2Int(0, -1);
    }

    bool IsSubmitHeld()
    {
        if (submitAction != null && submitAction.IsPressed())
            return true;

        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonSouth.isPressed)
            return true;

        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.enterKey.isPressed || kb.spaceKey.isPressed))
            return true;

        return false;
    }

    bool ReadSubmitPressed()
    {
        if (blockSubmitUntilRelease)
        {
            if (!IsSubmitHeld())
                blockSubmitUntilRelease = false;

            return false;
        }

        if (submitAction != null && submitAction.WasPressedThisFrame())
            return true;

        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonSouth.wasPressedThisFrame)
            return true;

        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            return true;

        return false;
    }

    bool ReadCancelPressed()
    {
        if (cancelAction != null && cancelAction.WasPressedThisFrame())
            return true;

        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonEast.wasPressedThisFrame)
            return true;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
            return true;

        return false;
    }

    bool ReadSwitchPrevPartPressed()
    {
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.leftShoulder.wasPressedThisFrame)
            return true;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.qKey.wasPressedThisFrame)
            return true;

        return false;
    }

    bool ReadSwitchNextPartPressed()
    {
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.rightShoulder.wasPressedThisFrame)
            return true;

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
            return true;

        return false;
    }

    void SwitchPart(PlacementPart part)
    {
        currentPart = part;
        ShowCurrentPart();
        RefreshCountText();
        RefreshPreviewVisual();
        RefreshReplaceSelectionVisual();

        if (helpText != null)
            helpText.text = moveMessage + "\n" + switchPartMessage;

        lastMoveDir = Vector2Int.zero;
        nextMoveTime = 0f;
        blockSubmitUntilRelease = true;
    }

    void ShowCurrentPart()
    {
        if (handView.root != null)
            handView.root.SetActive(currentPart == PlacementPart.Hand);

        if (armView.root != null)
            armView.root.SetActive(currentPart == PlacementPart.Arm);

        RefreshPartButtonColors();
    }

    void RefreshPartButtonColors()
    {
        ApplyButtonHighlight(handView.partButton, currentPart == PlacementPart.Hand);
        ApplyButtonHighlight(armView.partButton, currentPart == PlacementPart.Arm);
    }

    void ApplyButtonHighlight(Button button, bool selected)
    {
        if (button == null) return;

        ColorBlock cb = button.colors;
        cb.normalColor = selected ? new Color(1f, 1f, 0.6f, 1f) : Color.white;
        button.colors = cb;
    }

    void OnConfirmYes()
    {
        if (confirmMode == ConfirmMode.Place)
        {
            PlaceCurrentSticker();
            return;
        }

        if (confirmMode == ConfirmMode.ReplaceAsk)
        {
            EnterReplaceSelectMode();
            return;
        }
    }

    void OnConfirmNo()
    {
        HideConfirmImmediate();
        pageState = PageState.MoveSticker;
        confirmMode = ConfirmMode.None;
        blockSubmitUntilRelease = true;
    }

    void EnterReplaceSelectMode()
    {
        HideConfirmImmediate();

        PlacementRecordList data = LoadPartData(currentPart);
        if (data.items.Count <= 0)
        {
            pageState = PageState.MoveSticker;
            if (helpText != null)
                helpText.text = moveMessage + "\n" + switchPartMessage;
            return;
        }

        pageState = PageState.ReplaceSelect;
        confirmMode = ConfirmMode.None;

        int index = GetCurrentReplaceIndex();
        index = Mathf.Clamp(index, 0, data.items.Count - 1);
        SetCurrentReplaceIndex(index);

        RefreshPreviewVisual();
        RefreshReplaceSelectionVisual();

        if (helpText != null)
            helpText.text = replaceSelectMessage;
    }

    void PlaceCurrentSticker()
    {
        PartView view = GetCurrentView();
        if (view == null)
        {
            OnConfirmNo();
            return;
        }

        if (GetPlacementCount(currentPart) >= view.maxPlaced)
        {
            if (helpText != null)
                helpText.text = currentPart == PlacementPart.Hand ? handFullMessage : armFullMessage;

            OnConfirmNo();
            return;
        }

        PlacementRecordList data = LoadPartData(currentPart);
        Vector2Int pos = GetCurrentGridPos();

        PlacementRecord record = new PlacementRecord
        {
            stickerId = currentStickerId,
            col = pos.x,
            row = pos.y,
            order = data.items.Count
        };

        data.items.Add(record);
        SavePartData(currentPart, data);

        RefreshAllPlacedImages();
        RefreshCountText();

        if (helpText != null)
            helpText.text = placedMessage;

        HideConfirmImmediate();
        CloseToBook();
    }

    void RemoveSelectedPlacedSticker()
    {
        PlacementRecordList data = LoadPartData(currentPart);
        if (data.items.Count <= 0)
        {
            pageState = PageState.MoveSticker;
            RefreshReplaceSelectionVisual();
            RefreshPreviewVisual();
            return;
        }

        int index = Mathf.Clamp(GetCurrentReplaceIndex(), 0, data.items.Count - 1);
        data.items.RemoveAt(index);

        for (int i = 0; i < data.items.Count; i++)
            data.items[i].order = i;

        SavePartData(currentPart, data);
        RefreshAllPlacedImages();
        RefreshCountText();

        int newIndex = Mathf.Clamp(index, 0, Mathf.Max(0, data.items.Count - 1));
        SetCurrentReplaceIndex(newIndex);

        pageState = PageState.MoveSticker;
        blockSubmitUntilRelease = true;

        if (helpText != null)
            helpText.text = removedMessage + "\n" + moveMessage;

        RefreshPreviewVisual();
        RefreshReplaceSelectionVisual();
    }

    void HideConfirmImmediate()
    {
        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        confirmIndex = 0;

        if (confirmYesButton != null)
        {
            ColorBlock cb = confirmYesButton.colors;
            cb.normalColor = Color.white;
            confirmYesButton.colors = cb;
        }

        if (confirmNoButton != null)
        {
            ColorBlock cb = confirmNoButton.colors;
            cb.normalColor = Color.white;
            confirmNoButton.colors = cb;
        }

        EventSystem.current?.SetSelectedGameObject(null);
    }

    void RefreshConfirmSelection()
    {
        if (EventSystem.current != null)
        {
            if (confirmIndex == 0 && confirmYesButton != null)
                EventSystem.current.SetSelectedGameObject(confirmYesButton.gameObject);
            else if (confirmIndex == 1 && confirmNoButton != null)
                EventSystem.current.SetSelectedGameObject(confirmNoButton.gameObject);
        }

        if (confirmYesButton != null)
        {
            ColorBlock cb = confirmYesButton.colors;
            cb.normalColor = (confirmIndex == 0) ? new Color(1f, 1f, 0.6f, 1f) : Color.white;
            confirmYesButton.colors = cb;
        }

        if (confirmNoButton != null)
        {
            ColorBlock cb = confirmNoButton.colors;
            cb.normalColor = (confirmIndex == 1) ? new Color(1f, 1f, 0.6f, 1f) : Color.white;
            confirmNoButton.colors = cb;
        }
    }

    void RefreshCurrentStickerInfo()
    {
        if (currentStickerNameText != null)
            currentStickerNameText.text = currentStickerName;

        if (currentStickerEffectText != null)
            currentStickerEffectText.text = currentStickerEffect;
    }

    void RefreshPreviewVisual()
    {
        EnsurePreviewExists();

        PartView view = GetCurrentView();
        if (view == null || view.placedRoot == null || currentStickerSprite == null)
        {
            currentStickerPreview.enabled = false;
            return;
        }

        currentStickerPreview.transform.SetParent(view.placedRoot, false);
        currentStickerPreview.transform.SetAsLastSibling();
        currentStickerPreview.sprite = currentStickerSprite;
        currentStickerPreview.enabled = true;
        currentStickerPreview.preserveAspect = true;
        currentStickerPreview.raycastTarget = false;
        currentStickerPreview.color = new Color(1f, 1f, 1f, previewAlpha);

        RectTransform rt = currentStickerPreview.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = GetCellSize(view) * previewStickerScale;
        rt.anchoredPosition = GridToAnchoredPosition(view, GetCurrentGridPos());

        currentStickerPreview.transform.SetAsLastSibling();
    }

    void RefreshAllPlacedImages()
    {
        RefreshPlacedImagesForPart(PlacementPart.Hand);
        RefreshPlacedImagesForPart(PlacementPart.Arm);
        RefreshReplaceSelectionVisual();
    }

    void RefreshPlacedImagesForPart(PlacementPart part)
    {
        ClearSpawnedPlacedObjects(part);

        PartView view = GetView(part);
        if (view == null || view.placedRoot == null)
            return;

        PlacementRecordList data = LoadPartData(part);
        if (data == null || data.items == null)
            return;

        for (int i = 0; i < data.items.Count; i++)
        {
            PlacementRecord record = data.items[i];
            if (record == null || string.IsNullOrWhiteSpace(record.stickerId))
                continue;

            if (!StickerBookPage.TryGetStickerRuntimeData(record.stickerId, out StickerBookPage.StickerRuntimeData stickerData))
                continue;

            if (stickerData.stickerSprite == null)
                continue;

            GameObject go = new GameObject("PlacedSticker_" + part + "_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(view.placedRoot, false);

            Image img = go.GetComponent<Image>();
            img.sprite = stickerData.stickerSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = GetCellSize(view) * placedStickerScale;
            rt.anchoredPosition = GridToAnchoredPosition(view, new Vector2Int(record.col, record.row));

            PlacedVisual visual = new PlacedVisual
            {
                go = go,
                rect = rt,
                image = img
            };

            spawnedPlacedObjects[part].Add(visual);
        }
    }

    void RefreshReplaceSelectionVisual()
    {
        foreach (var kv in spawnedPlacedObjects)
        {
            List<PlacedVisual> list = kv.Value;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null || list[i].image == null || list[i].rect == null)
                    continue;

                bool selected =
                    pageState == PageState.ReplaceSelect &&
                    kv.Key == currentPart &&
                    i == GetCurrentReplaceIndex();

                list[i].image.color = selected ? new Color(1f, 1f, 0.45f, 1f) : Color.white;
                list[i].rect.localScale = selected ? Vector3.one * 1.15f : Vector3.one;
            }
        }
    }

    void MoveReplaceSelection(int dirX, int dirY)
    {
        if (!spawnedPlacedObjects.TryGetValue(currentPart, out List<PlacedVisual> list))
            return;

        if (list == null || list.Count == 0)
            return;

        int currentIndex = Mathf.Clamp(GetCurrentReplaceIndex(), 0, list.Count - 1);
        RectTransform currentRect = list[currentIndex].rect;
        if (currentRect == null)
            return;

        Vector3 currentPos = currentRect.position;

        float bestScore = float.MaxValue;
        int bestIndex = currentIndex;

        for (int i = 0; i < list.Count; i++)
        {
            if (i == currentIndex) continue;
            if (list[i] == null || list[i].rect == null) continue;

            Vector3 delta = list[i].rect.position - currentPos;

            if (dirX < 0 && delta.x >= -0.01f) continue;
            if (dirX > 0 && delta.x <= 0.01f) continue;
            if (dirY < 0 && delta.y >= -0.01f) continue;
            if (dirY > 0 && delta.y <= 0.01f) continue;

            float primary = dirX != 0 ? Mathf.Abs(delta.x) : Mathf.Abs(delta.y);
            float secondary = dirX != 0 ? Mathf.Abs(delta.y) : Mathf.Abs(delta.x);
            float score = primary * 10f + secondary;

            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        SetCurrentReplaceIndex(bestIndex);
        RefreshReplaceSelectionVisual();
    }

    void ClearSpawnedPlacedObjects(PlacementPart part)
    {
        if (!spawnedPlacedObjects.TryGetValue(part, out List<PlacedVisual> list))
            return;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].go != null)
                Destroy(list[i].go);
        }

        list.Clear();
    }

    void RefreshCountText()
    {
        if (countText == null)
            return;

        PartView view = GetCurrentView();
        if (view == null)
        {
            countText.text = string.Empty;
            return;
        }

        string partName = currentPart == PlacementPart.Hand ? "手の甲" : "腕の部分";
        countText.text = partName + " " + GetPlacementCount(currentPart) + "/" + view.maxPlaced;
    }

    PartView GetView(PlacementPart part)
    {
        return part == PlacementPart.Hand ? handView : armView;
    }

    PartView GetCurrentView()
    {
        return GetView(currentPart);
    }

    Vector2Int GetCurrentGridPos()
    {
        return currentPart == PlacementPart.Hand ? handGridPos : armGridPos;
    }

    void SetCurrentGridPos(Vector2Int value)
    {
        if (currentPart == PlacementPart.Hand)
            handGridPos = value;
        else
            armGridPos = value;
    }

    int GetCurrentReplaceIndex()
    {
        return currentPart == PlacementPart.Hand ? handReplaceIndex : armReplaceIndex;
    }

    void SetCurrentReplaceIndex(int value)
    {
        if (currentPart == PlacementPart.Hand)
            handReplaceIndex = value;
        else
            armReplaceIndex = value;
    }

    void ClampStoredGrid(ref Vector2Int pos, PartView view)
    {
        if (view == null)
        {
            pos = Vector2Int.zero;
            return;
        }

        pos.x = Mathf.Clamp(pos.x, 0, Mathf.Max(0, view.columns - 1));
        pos.y = Mathf.Clamp(pos.y, 0, Mathf.Max(0, view.rows - 1));
    }

    Vector2 GetCellSize(PartView view)
    {
        if (view == null || view.moveArea == null)
            return new Vector2(64f, 64f);

        Rect rect = view.moveArea.rect;
        float width = view.columns > 0 ? rect.width / view.columns : rect.width;
        float height = view.rows > 0 ? rect.height / view.rows : rect.height;
        return new Vector2(width, height);
    }

    Vector2 GridToAnchoredPosition(PartView view, Vector2Int grid)
    {
        if (view == null || view.moveArea == null || view.placedRoot == null)
            return Vector2.zero;

        Rect moveRect = view.moveArea.rect;
        Rect placedRect = view.placedRoot.rect;

        float cellWidth = view.columns > 0 ? moveRect.width / view.columns : moveRect.width;
        float cellHeight = view.rows > 0 ? moveRect.height / view.rows : moveRect.height;

        float xInMove = -moveRect.width * 0.5f + cellWidth * 0.5f + cellWidth * grid.x;
        float yInMove = moveRect.height * 0.5f - cellHeight * 0.5f - cellHeight * grid.y;

        Vector2 movePivotOffset = view.moveArea.anchoredPosition;
        Vector2 placedPivotOffset = view.placedRoot.anchoredPosition;

        return new Vector2(
            xInMove + (movePivotOffset.x - placedPivotOffset.x),
            yInMove + (movePivotOffset.y - placedPivotOffset.y)
        );
    }

    int GetPlacementCount(PlacementPart part)
    {
        PlacementRecordList data = LoadPartData(part);
        return data.items.Count;
    }

    void HideEverything()
    {
        pageState = PageState.None;
        confirmMode = ConfirmMode.None;

        if (partSelectRoot != null)
            partSelectRoot.SetActive(false);

        if (handView.root != null)
            handView.root.SetActive(false);

        if (armView.root != null)
            armView.root.SetActive(false);

        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        if (currentStickerPreview != null)
            currentStickerPreview.enabled = false;

        RefreshReplaceSelectionVisual();
    }

    void CloseToBook()
    {
        HideEverything();
        gameObject.SetActive(false);

        StickerBookPage book = ownerBookPage != null ? ownerBookPage : FindObjectOfType<StickerBookPage>(true);
        if (book != null)
            book.ReturnFromPlacement();
    }

    static string GetSaveKey(PlacementPart part)
    {
        return "StickerPlacementRecords_" + part;
    }

    PlacementRecordList LoadPartData(PlacementPart part)
    {
        string json = PlayerPrefs.GetString(GetSaveKey(part), string.Empty);

        if (string.IsNullOrWhiteSpace(json))
            return new PlacementRecordList();

        PlacementRecordList data = JsonUtility.FromJson<PlacementRecordList>(json);
        if (data == null || data.items == null)
            return new PlacementRecordList();

        return data;
    }

    void SavePartData(PlacementPart part, PlacementRecordList data)
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(GetSaveKey(part), json);
        PlayerPrefs.Save();
        OnPlacementsChanged?.Invoke();
    }
}