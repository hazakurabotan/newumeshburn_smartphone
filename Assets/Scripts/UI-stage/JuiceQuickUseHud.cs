using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class JuiceQuickUseHud : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private JuiceInventory juiceInventory;
    [SerializeField] private JuiceUseManager juiceUseManager;

    [Header("HUD")]
    [SerializeField] private Image juiceIconImage;
    [SerializeField] private GameObject selectionFrameObject;
    [SerializeField] private TMP_Text juiceNameText;
    [SerializeField] private TMP_Text resultText;

    [Header("Input Names")]
    [SerializeField] private string gameplayActionMapName = "Mawaru";
    [SerializeField] private string primaryDrinkActionName = "drink";
    [SerializeField] private string fallbackDrinkActionName = "";
    [SerializeField] private string useActionName = "Interact";

    [Header("Behavior")]
    [SerializeField] private float holdToCloseSeconds = 0.5f;
    [SerializeField] private bool requireCurrentMapMatch = true;
    [SerializeField] private bool keepFrameOpenAfterUse = true;
    [SerializeField] private bool showDebugLog = true;

    [Header("Optional Result Text")]
    [SerializeField] private float resultTextSeconds = 1.5f;

    private InputAction drinkAction;
    private InputAction useAction;

    private int selectedOwnedSlotIndex = 0;
    private bool frameVisible = false;

    private bool drinkHolding = false;
    private float drinkHoldTimer = 0f;
    private bool longHoldHandled = false;

    private float resultTimer = 0f;

    private void Awake()
    {
        ResolveReferences();
        BindActions();
        RefreshHudImmediate();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindActions();

        if (juiceInventory != null)
            juiceInventory.OnInventoryChanged += HandleInventoryChanged;

        if (juiceUseManager != null)
            juiceUseManager.OnStateChanged += HandleUseStateChanged;

        RefreshHudImmediate();
    }

    private void OnDisable()
    {
        if (juiceInventory != null)
            juiceInventory.OnInventoryChanged -= HandleInventoryChanged;

        if (juiceUseManager != null)
            juiceUseManager.OnStateChanged -= HandleUseStateChanged;
    }

    private void Update()
    {
        ResolveReferences();
        BindActions();
        UpdateResultTextTimer();

        if (!CanReadGameplayInput())
            return;

        if (juiceInventory == null || juiceInventory.Count <= 0)
        {
            frameVisible = false;
            RefreshHudImmediate();
            return;
        }

        HandleDrinkInput();
        HandleUseInput();
    }

    private void ResolveReferences()
    {
        if (playerInput == null)
            playerInput = FindObjectOfType<PlayerInput>(true);

        if (juiceInventory == null)
            juiceInventory = JuiceInventory.Instance;

        if (juiceUseManager == null)
            juiceUseManager = JuiceUseManager.Instance;
    }

    private void BindActions()
    {
        if (playerInput == null || playerInput.actions == null)
            return;

        InputActionMap map = playerInput.actions.FindActionMap(gameplayActionMapName, false);
        if (map == null)
            return;

        if (drinkAction == null)
        {
            if (!string.IsNullOrWhiteSpace(primaryDrinkActionName))
                drinkAction = map.FindAction(primaryDrinkActionName, false);

            if (drinkAction == null && !string.IsNullOrWhiteSpace(fallbackDrinkActionName))
                drinkAction = map.FindAction(fallbackDrinkActionName, false);
        }

        if (useAction == null && !string.IsNullOrWhiteSpace(useActionName))
            useAction = map.FindAction(useActionName, false);
    }

    private bool CanReadGameplayInput()
    {
        if (playerInput == null || playerInput.actions == null)
            return false;

        if (requireCurrentMapMatch)
        {
            if (playerInput.currentActionMap == null)
                return false;

            if (playerInput.currentActionMap.name != gameplayActionMapName)
                return false;
        }

        return drinkAction != null && useAction != null;
    }

    private void HandleDrinkInput()
    {
        if (drinkAction.WasPressedThisFrame())
        {
            drinkHolding = true;
            drinkHoldTimer = 0f;
            longHoldHandled = false;
        }

        if (drinkHolding && drinkAction.IsPressed())
        {
            drinkHoldTimer += Time.unscaledDeltaTime;

            if (frameVisible && !longHoldHandled && drinkHoldTimer >= holdToCloseSeconds)
            {
                frameVisible = false;
                longHoldHandled = true;
                RefreshHudImmediate();
            }
        }

        if (drinkHolding && drinkAction.WasReleasedThisFrame())
        {
            if (!longHoldHandled)
            {
                HandleShortDrinkPress();
            }

            drinkHolding = false;
            drinkHoldTimer = 0f;
            longHoldHandled = false;
        }
    }

    private void HandleUseInput()
    {
        if (!frameVisible)
            return;

        if (!useAction.WasPressedThisFrame())
            return;

        TryUseSelectedJuice();
    }

    private void HandleShortDrinkPress()
    {
        if (juiceInventory == null || juiceInventory.Count <= 0)
        {
            frameVisible = false;
            RefreshHudImmediate();
            return;
        }

        ClampSelectedIndex();

        if (!frameVisible)
        {
            frameVisible = true;
            RefreshHudImmediate();
            return;
        }

        if (juiceInventory.Count > 1)
        {
            selectedOwnedSlotIndex++;
            if (selectedOwnedSlotIndex >= juiceInventory.Count)
                selectedOwnedSlotIndex = 0;
        }

        RefreshHudImmediate();
    }

    private void TryUseSelectedJuice()
    {
        if (juiceUseManager == null || juiceInventory == null)
            return;

        ClampSelectedIndex();

        if (!juiceUseManager.TryUseJuiceAt(selectedOwnedSlotIndex, out JuiceUseManager.UseResult result))
        {
            if (result != null)
                ShowResult(result.message, false);

            return;
        }

        if (showDebugLog)
            Debug.Log($"[JuiceQuickUseHud] {result.title} / {result.message}");

        ShowResult(result.message, true);

        if (juiceInventory.Count <= 0)
        {
            frameVisible = false;
        }
        else
        {
            ClampSelectedIndex();

            if (!keepFrameOpenAfterUse)
                frameVisible = false;
        }

        RefreshHudImmediate();
    }

    private void HandleInventoryChanged()
    {
        if (juiceInventory == null || juiceInventory.Count <= 0)
        {
            selectedOwnedSlotIndex = 0;
            frameVisible = false;
        }
        else
        {
            ClampSelectedIndex();
        }

        RefreshHudImmediate();
    }

    private void HandleUseStateChanged()
    {
        if (juiceInventory == null || juiceInventory.Count <= 0)
        {
            selectedOwnedSlotIndex = 0;
            frameVisible = false;
        }
        else
        {
            ClampSelectedIndex();
        }

        RefreshHudImmediate();
    }

    private void ClampSelectedIndex()
    {
        if (juiceInventory == null || juiceInventory.Count <= 0)
        {
            selectedOwnedSlotIndex = 0;
            return;
        }

        if (selectedOwnedSlotIndex < 0)
            selectedOwnedSlotIndex = 0;

        if (selectedOwnedSlotIndex >= juiceInventory.Count)
            selectedOwnedSlotIndex = juiceInventory.Count - 1;
    }

    private void RefreshHudImmediate()
    {
        if (juiceInventory == null || juiceInventory.Count <= 0)
        {
            if (juiceIconImage != null)
            {
                juiceIconImage.enabled = false;
                juiceIconImage.sprite = null;
            }

            if (selectionFrameObject != null)
                selectionFrameObject.SetActive(false);

            if (juiceNameText != null)
                juiceNameText.text = string.Empty;

            return;
        }

        ClampSelectedIndex();

        JuiceInventory.JuiceDefinition current = juiceInventory.GetOwnedDefinitionAt(selectedOwnedSlotIndex);
        Sprite displayIcon = juiceInventory.GetDisplayIconForOwnedSlot(selectedOwnedSlotIndex);

        if (juiceIconImage != null)
        {
            juiceIconImage.sprite = displayIcon;
            juiceIconImage.enabled = displayIcon != null;
        }

        if (selectionFrameObject != null)
            selectionFrameObject.SetActive(frameVisible);

        if (juiceNameText != null)
        {
            if (juiceUseManager != null)
                juiceNameText.text = juiceUseManager.BuildQuickUseLabel(current);
            else
                juiceNameText.text = current != null ? current.displayName : string.Empty;
        }
    }

    private void ShowResult(string message, bool success)
    {
        if (resultText == null)
            return;

        resultText.text = message;
        resultText.color = success ? Color.white : Color.red;
        resultTimer = resultTextSeconds;
    }

    private void UpdateResultTextTimer()
    {
        if (resultText == null)
            return;

        if (resultTimer <= 0f)
            return;

        resultTimer -= Time.unscaledDeltaTime;
        if (resultTimer <= 0f)
        {
            resultText.text = string.Empty;
        }
    }
}