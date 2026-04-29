using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class MenuInputBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private PlayerInput playerInput;

    [Header("Action Map Names")]
    [SerializeField] private string gameplayActionMapName = "Mawaru";
    [SerializeField] private string uiActionMapName = "UI";
    [SerializeField] private string menuActionName = "Menu";

    [Header("Fallback")]
    [SerializeField] private bool allowRawGamepadFallback = true;

    [Header("Debounce")]
    [SerializeField] private float toggleCooldownSeconds = 0.15f;

    private float nextToggleAllowedTime = 0f;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        ResolveReferences();

        if (menuManager == null)
            return;

        if (Time.unscaledTime < nextToggleAllowedTime)
            return;

        if (WasMenuPressedThisFrame())
        {
            menuManager.ToggleMenu();
            nextToggleAllowedTime = Time.unscaledTime + toggleCooldownSeconds;
        }
    }

    private void ResolveReferences()
    {
        if (menuManager == null)
            menuManager = GetComponent<MenuManager>();

        if (playerInput == null)
            playerInput = FindObjectOfType<PlayerInput>(true);
    }

    private bool WasMenuPressedThisFrame()
    {
        if (playerInput != null && playerInput.actions != null)
        {
            if (TryReadMenuPressedOnMap(playerInput.currentActionMap))
                return true;

            InputActionMap gameplayMap = playerInput.actions.FindActionMap(gameplayActionMapName, false);
            if (TryReadMenuPressedOnMap(gameplayMap))
                return true;

            InputActionMap uiMap = playerInput.actions.FindActionMap(uiActionMapName, false);
            if (TryReadMenuPressedOnMap(uiMap))
                return true;
        }

        if (allowRawGamepadFallback && Gamepad.current != null)
        {
            if (Gamepad.current.selectButton.wasPressedThisFrame)
                return true;
        }

        return false;
    }

    private bool TryReadMenuPressedOnMap(InputActionMap map)
    {
        if (map == null)
            return false;

        InputAction menuAction = map.FindAction(menuActionName, false);
        if (menuAction == null)
            return false;

        if (!menuAction.enabled && playerInput != null && playerInput.currentActionMap != map)
            return false;

        return menuAction.WasPressedThisFrame();
    }
}