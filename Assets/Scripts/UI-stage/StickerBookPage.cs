using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class StickerBookPage : MonoBehaviour
{
    [Serializable]
    public class StickerSlot
    {
        [Header("Sticker Data")]
        public string stickerId;
        public string stickerName;
        public Sprite stickerSprite;
        [TextArea(2, 4)] public string effectText;
        public bool acquiredByDefault = true;

        [Header("UI")]
        public Button slotButton;
        public Image slotImage;
    }

    public struct StickerRuntimeData
    {
        public string stickerId;
        public string stickerName;
        public Sprite stickerSprite;
        public string effectText;
    }

    static readonly Dictionary<string, StickerRuntimeData> RuntimeStickerDatabase = new Dictionary<string, StickerRuntimeData>();

    [Header("Roots")]
    public GameObject selectRoot;
    public GameObject placementPageRoot;

    [Header("Navigation")]
    public MenuManager menuManager;
    public Button backButton;

    [Header("Slots")]
    public StickerSlot[] slots;

    [Header("Cursor")]
    public RectTransform cursor;
    public Vector2 cursorOffset = new Vector2(0f, -38f);
    public Vector2 confirmCursorOffset = new Vector2(-55f, 0f);

    [Header("Selected Visual")]
    public float selectedStickerScale = 1.5f;

    [Header("Texts")]
    public TMP_Text selectedStickerNameText;
    public TMP_Text selectedStickerEffectText;

    [Header("Confirm")]
    public GameObject confirmRoot;
    public TMP_Text confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;
    [TextArea(2, 4)] public string confirmMessage = "このステッカーを\nはりますか？";

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

    int currentIndex = -1;
    bool confirmOpen;
    int confirmIndex = 0; // 0 = Yes, 1 = No

    InputAction navigateAction;
    InputAction submitAction;
    InputAction cancelAction;

    Vector2Int lastMoveDir = Vector2Int.zero;
    float nextMoveTime = 0f;

    bool blockSubmitUntilRelease = false;

    readonly Dictionary<int, Vector3> originalImageScales = new Dictionary<int, Vector3>();

    void Awake()
    {
        RepairReferences();
        ApplyInspectorSpriteFallbacks();
        CacheOriginalImageScales();
        RebuildRuntimeDatabase();
        WireButtons();
        CacheInputActions();

        if (confirmText != null && string.IsNullOrWhiteSpace(confirmText.text))
            confirmText.text = confirmMessage;

        HideConfirmImmediate();
    }

    void OnEnable()
    {
        RepairReferences();
        ApplyInspectorSpriteFallbacks();
        CacheOriginalImageScales();
        RebuildRuntimeDatabase();
        CacheInputActions();
        OpenBook();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        RepairReferences();
        ApplyInspectorSpriteFallbacks();
        CacheOriginalImageScales();
        RebuildRuntimeDatabase();
    }
#endif

    void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (selectRoot != null && !selectRoot.activeInHierarchy)
            return;

        HandleInput();
    }

    void RepairReferences()
    {
        RepairSlotReferences();
        RepairBackButtonReference();
        RepairMenuManagerReference();
    }

    void RepairMenuManagerReference()
    {
        if (menuManager == null)
            menuManager = FindObjectOfType<MenuManager>(true);
    }

    void RepairBackButtonReference()
    {
        if (backButton != null)
            return;

        if (selectRoot == null)
            return;

        Transform[] children = selectRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (!string.Equals(children[i].name, "BackButton", StringComparison.OrdinalIgnoreCase))
                continue;

            backButton = children[i].GetComponent<Button>();
            if (backButton != null)
                return;
        }

        backButton = selectRoot.GetComponentInChildren<Button>(true);
    }

    void RepairSlotReferences()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            StickerSlot slot = slots[i];
            if (slot == null)
                continue;

            Transform slotRoot = null;

            if (slot.slotImage != null)
                slotRoot = slot.slotImage.transform.parent;
            else if (slot.slotButton != null)
                slotRoot = slot.slotButton.transform.parent;

            if (slotRoot == null)
                continue;

            if (slot.slotImage == null)
            {
                Image[] images = slotRoot.GetComponentsInChildren<Image>(true);
                for (int j = 0; j < images.Length; j++)
                {
                    if (images[j] != null && images[j].gameObject != slotRoot.gameObject)
                    {
                        slot.slotImage = images[j];
                        break;
                    }
                }
            }

            if (slot.slotButton == null)
            {
                Button[] buttons = slotRoot.GetComponentsInChildren<Button>(true);
                for (int j = 0; j < buttons.Length; j++)
                {
                    if (buttons[j] != null && buttons[j].transform.parent == slotRoot)
                    {
                        slot.slotButton = buttons[j];
                        break;
                    }
                }
            }
        }
    }

    void ApplyInspectorSpriteFallbacks()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            StickerSlot slot = slots[i];
            if (slot == null)
                continue;

            if (slot.slotImage != null && slot.stickerSprite == null && slot.slotImage.sprite != null)
                slot.stickerSprite = slot.slotImage.sprite;
        }
    }

    void CacheOriginalImageScales()
    {
        originalImageScales.Clear();

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            StickerSlot slot = slots[i];
            if (slot == null || slot.slotImage == null)
                continue;

            originalImageScales[i] = slot.slotImage.rectTransform.localScale;
        }
    }

    void RebuildRuntimeDatabase()
    {
        RuntimeStickerDatabase.Clear();

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            StickerSlot slot = slots[i];
            if (slot == null)
                continue;

            if (string.IsNullOrWhiteSpace(slot.stickerId))
                continue;

            StickerRuntimeData data = new StickerRuntimeData
            {
                stickerId = slot.stickerId,
                stickerName = slot.stickerName,
                stickerSprite = slot.stickerSprite,
                effectText = slot.effectText
            };

            RuntimeStickerDatabase[slot.stickerId] = data;
        }
    }

    public static bool TryGetStickerRuntimeData(string stickerId, out StickerRuntimeData data)
    {
        if (string.IsNullOrWhiteSpace(stickerId))
        {
            data = default;
            return false;
        }

        return RuntimeStickerDatabase.TryGetValue(stickerId, out data);
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

    void HandleInput()
    {
        if (confirmOpen)
        {
            HandleConfirmNavigate();

            if (ReadSubmitPressed())
            {
                if (confirmIndex == 0) ConfirmYes();
                else ConfirmNo();
            }

            if (ReadCancelPressed())
                ConfirmNo();

            return;
        }

        HandleStickerNavigate();

        if (ReadSubmitPressed())
            PromptConfirmCurrent();

        if (ReadCancelPressed())
            CancelBook();
    }

    void HandleStickerNavigate()
    {
        Vector2 input = ReadNavigateVector();
        Vector2Int moveDir = ConvertNavigateToDigital(input);

        if (moveDir == Vector2Int.zero)
        {
            lastMoveDir = Vector2Int.zero;
            return;
        }

        float now = Time.unscaledTime;
        bool firstPress = lastMoveDir == Vector2Int.zero;
        bool changedDir = moveDir != lastMoveDir;
        bool repeatReady = now >= nextMoveTime;

        if (firstPress || changedDir || repeatReady)
        {
            MoveSelection(moveDir.x, moveDir.y);
            lastMoveDir = moveDir;
            nextMoveTime = now + (firstPress || changedDir ? firstRepeatDelay : repeatInterval);
        }
    }

    void HandleConfirmNavigate()
    {
        Vector2 input = ReadNavigateVector();
        Vector2Int moveDir = ConvertNavigateToDigital(input);

        if (moveDir == Vector2Int.zero)
        {
            lastMoveDir = Vector2Int.zero;
            return;
        }

        float now = Time.unscaledTime;
        bool firstPress = lastMoveDir == Vector2Int.zero;
        bool changedDir = moveDir != lastMoveDir;
        bool repeatReady = now >= nextMoveTime;

        if (firstPress || changedDir || repeatReady)
        {
            if (moveDir.x != 0 || moveDir.y != 0)
            {
                confirmIndex = 1 - confirmIndex;
                RefreshConfirmSelection();
                RefreshCursor();
            }

            lastMoveDir = moveDir;
            nextMoveTime = now + (firstPress || changedDir ? firstRepeatDelay : repeatInterval);
        }
    }

    Vector2Int ConvertNavigateToDigital(Vector2 input)
    {
        if (input.magnitude < navigateDeadZone)
            return Vector2Int.zero;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            return input.x < 0f ? new Vector2Int(-1, 0) : new Vector2Int(1, 0);

        return input.y > 0f ? new Vector2Int(0, 1) : new Vector2Int(0, -1);
    }

    void WireButtons()
    {
        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveListener(ConfirmYes);
            confirmYesButton.onClick.AddListener(ConfirmYes);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveListener(ConfirmNo);
            confirmNoButton.onClick.AddListener(ConfirmNo);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonPressed);
        }

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            int captured = i;

            if (slots[i] == null || slots[i].slotButton == null)
                continue;

            slots[i].slotButton.onClick.RemoveAllListeners();
            slots[i].slotButton.onClick.AddListener(() => OnSlotClicked(captured));
        }
    }

    void OnBackButtonPressed()
    {
        CancelBook();
    }

    public void OpenBook()
    {
        CacheInputActions();
        Canvas.ForceUpdateCanvases();

        if (selectRoot != null)
            selectRoot.SetActive(true);

        if (placementPageRoot != null)
            placementPageRoot.SetActive(false);

        HideConfirmImmediate();
        RefreshSlots();
        SelectFirstAvailable();
        RefreshSelectedTexts();
        RefreshCursor();
        RefreshSelectedVisuals();

        lastMoveDir = Vector2Int.zero;
        nextMoveTime = 0f;
        blockSubmitUntilRelease = false;
    }

    public void ReturnFromPlacement()
    {
        CacheInputActions();
        Canvas.ForceUpdateCanvases();

        if (placementPageRoot != null)
            placementPageRoot.SetActive(false);

        if (selectRoot != null)
            selectRoot.SetActive(true);

        HideConfirmImmediate();
        RefreshSlots();

        if (!IsSelectable(currentIndex))
            SelectFirstAvailable();
        else
            SelectIndex(currentIndex);

        RefreshSelectedTexts();
        RefreshCursor();
        RefreshSelectedVisuals();

        lastMoveDir = Vector2Int.zero;
        nextMoveTime = 0f;
        blockSubmitUntilRelease = false;
    }

    public void CancelBook()
    {
        if (confirmOpen)
        {
            ConfirmNo();
            return;
        }

        HideConfirmImmediate();

        RepairMenuManagerReference();
        if (menuManager != null)
        {
            menuManager.OpenStartPage();
            return;
        }
    }

    void RefreshSlots()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            StickerSlot slot = slots[i];
            if (slot == null)
                continue;

            bool acquired = IsStickerAcquired(slot.stickerId, slot.acquiredByDefault);

            if (slot.slotImage != null)
            {
                Sprite spriteToShow = slot.stickerSprite;

                if (spriteToShow == null && slot.slotImage.sprite != null)
                    spriteToShow = slot.slotImage.sprite;

                slot.slotImage.sprite = acquired ? spriteToShow : null;
                slot.slotImage.enabled = acquired && spriteToShow != null;
                slot.slotImage.preserveAspect = true;
            }

            if (slot.slotButton != null)
            {
                slot.slotButton.gameObject.SetActive(acquired);
                slot.slotButton.interactable = acquired;
            }
        }
    }

    void RefreshSelectedVisuals()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            StickerSlot slot = slots[i];
            if (slot == null || slot.slotImage == null)
                continue;

            RectTransform rt = slot.slotImage.rectTransform;

            if (!originalImageScales.TryGetValue(i, out Vector3 baseScale))
            {
                baseScale = rt.localScale;
                originalImageScales[i] = baseScale;
            }

            rt.localScale = (i == currentIndex && IsSelectable(i))
                ? baseScale * selectedStickerScale
                : baseScale;
        }
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

    void OnSlotClicked(int index)
    {
        if (!IsSelectable(index))
            return;

        SelectIndex(index);
        PromptConfirmCurrent();
    }

    void SelectFirstAvailable()
    {
        currentIndex = -1;

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (IsSelectable(i))
            {
                currentIndex = i;
                break;
            }
        }
    }

    void SelectIndex(int index)
    {
        if (!IsSelectable(index))
            return;

        currentIndex = index;
        RefreshSelectedTexts();
        RefreshCursor();
        RefreshSelectedVisuals();
    }

    bool IsSelectable(int index)
    {
        if (slots == null) return false;
        if (index < 0 || index >= slots.Length) return false;
        if (slots[index] == null) return false;
        if (slots[index].slotButton == null) return false;
        if (!slots[index].slotButton.gameObject.activeInHierarchy) return false;
        return slots[index].slotButton.interactable;
    }

    RectTransform GetSlotTargetRect(int index)
    {
        if (slots == null) return null;
        if (index < 0 || index >= slots.Length) return null;
        if (slots[index] == null) return null;

        if (slots[index].slotImage != null)
            return slots[index].slotImage.rectTransform;

        if (slots[index].slotButton != null)
            return slots[index].slotButton.GetComponent<RectTransform>();

        return null;
    }

    RectTransform GetCurrentConfirmTargetRect()
    {
        if (!confirmOpen)
            return null;

        if (confirmIndex == 0 && confirmYesButton != null)
            return confirmYesButton.GetComponent<RectTransform>();

        if (confirmIndex == 1 && confirmNoButton != null)
            return confirmNoButton.GetComponent<RectTransform>();

        return null;
    }

    void MoveSelection(int dirX, int dirY)
    {
        if (slots == null || slots.Length == 0)
            return;

        if (!IsSelectable(currentIndex))
        {
            SelectFirstAvailable();
            RefreshSelectedTexts();
            RefreshCursor();
            RefreshSelectedVisuals();
            return;
        }

        RectTransform currentRect = GetSlotTargetRect(currentIndex);
        if (currentRect == null)
            return;

        Vector3 currentPos = currentRect.position;

        float bestScore = float.MaxValue;
        int bestIndex = currentIndex;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i == currentIndex)
                continue;

            if (!IsSelectable(i))
                continue;

            RectTransform targetRect = GetSlotTargetRect(i);
            if (targetRect == null)
                continue;

            Vector3 delta = targetRect.position - currentPos;

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

        if (bestIndex != currentIndex)
            SelectIndex(bestIndex);
    }

    void RefreshSelectedTexts()
    {
        StickerSlot slot = GetCurrentSlot();

        if (selectedStickerNameText != null)
            selectedStickerNameText.text = slot != null ? slot.stickerName : string.Empty;

        if (selectedStickerEffectText != null)
            selectedStickerEffectText.text = slot != null ? slot.effectText : string.Empty;
    }

    void RefreshCursor()
    {
        if (cursor == null)
            return;

        if (confirmOpen)
        {
            RectTransform confirmRect = GetCurrentConfirmTargetRect();
            if (confirmRect == null)
            {
                cursor.gameObject.SetActive(false);
                return;
            }

            cursor.gameObject.SetActive(true);
            cursor.position = confirmRect.position + (Vector3)confirmCursorOffset;
            return;
        }

        RectTransform targetRect = GetSlotTargetRect(currentIndex);
        if (targetRect == null || !IsSelectable(currentIndex))
        {
            cursor.gameObject.SetActive(false);
            return;
        }

        cursor.gameObject.SetActive(true);
        cursor.position = targetRect.position + (Vector3)cursorOffset;
    }

    StickerSlot GetCurrentSlot()
    {
        if (!IsSelectable(currentIndex))
            return null;

        return slots[currentIndex];
    }

    public void PromptConfirmCurrent()
    {
        StickerSlot slot = GetCurrentSlot();
        if (slot == null)
            return;

        confirmOpen = true;
        confirmIndex = 0;
        lastMoveDir = Vector2Int.zero;
        nextMoveTime = 0f;
        blockSubmitUntilRelease = true;

        if (confirmRoot != null)
        {
            confirmRoot.SetActive(true);
            confirmRoot.transform.SetAsLastSibling();
        }

        if (confirmText != null)
            confirmText.text = confirmMessage;

        RefreshConfirmSelection();
        RefreshCursor();
    }

    public void ConfirmYes()
    {
        StickerSlot slot = GetCurrentSlot();
        if (slot == null)
        {
            HideConfirmImmediate();
            return;
        }

        HideConfirmImmediate();

        if (selectRoot != null)
            selectRoot.SetActive(false);

        if (placementPageRoot != null)
        {
            placementPageRoot.SetActive(true);
            placementPageRoot.SendMessage("OpenWithStickerId", slot.stickerId, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void ConfirmNo()
    {
        HideConfirmImmediate();
        RefreshCursor();
        RefreshSelectedVisuals();
    }

    void HideConfirmImmediate()
    {
        confirmOpen = false;
        confirmIndex = 0;

        if (confirmRoot != null)
            confirmRoot.SetActive(false);

        EventSystem.current?.SetSelectedGameObject(null);
    }

    bool IsStickerAcquired(string stickerId, bool acquiredByDefault)
    {
        if (string.IsNullOrWhiteSpace(stickerId))
            return acquiredByDefault;

        string key = "StickerAcquired_" + stickerId;
        if (PlayerPrefs.HasKey(key))
            return PlayerPrefs.GetInt(key, 0) == 1;

        return acquiredByDefault;
    }

    public static void SetStickerAcquired(string stickerId, bool acquired)
    {
        if (string.IsNullOrWhiteSpace(stickerId))
            return;

        PlayerPrefs.SetInt("StickerAcquired_" + stickerId, acquired ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool GetStickerAcquired(string stickerId)
    {
        if (string.IsNullOrWhiteSpace(stickerId))
            return false;

        return PlayerPrefs.GetInt("StickerAcquired_" + stickerId, 0) == 1;
    }
}