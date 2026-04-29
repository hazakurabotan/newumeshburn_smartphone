using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private GameObject girlsGearUIRoot;

    [Header("Pages")]
    [SerializeField] private GameObject startPage;
    [SerializeField] private GameObject mixJuicePage;
    [SerializeField] private GameObject stickerBookPage;
    [SerializeField] private GameObject stickerPlacementPage;
    [SerializeField] private GameObject mdDeskPage;
    [SerializeField] private GameObject settingsPage;
    [SerializeField] private GameObject optionPage;
    [SerializeField] private GameObject inventoryPage;

    [Header("Page Controllers")]
    [SerializeField] private MixJuicePageController mixJuicePageController;

    [Header("First Selected")]
    [SerializeField] private Selectable startFirst;
    [SerializeField] private Selectable mixJuiceFirst;
    [SerializeField] private Selectable stickerBookFirst;
    [SerializeField] private Selectable stickerPlacementFirst;
    [SerializeField] private Selectable mdDeskFirst;
    [SerializeField] private Selectable settingsFirst;
    [SerializeField] private Selectable optionFirst;
    [SerializeField] private Selectable inventoryFirst;

    [Header("SettingsPage Buttons")]
    [SerializeField] private Selectable optionButton;
    [SerializeField] private Selectable inventoryButton;
    [SerializeField] private Selectable stageBackButton;
    [SerializeField] private Selectable titleBackButton;

    [Header("OptionPage Controls")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private Selectable fullScreenButton;
    [SerializeField] private Selectable windowedModeButton;
    [SerializeField] private Selectable optionBackButton;
    [SerializeField] private TMP_Text displayModeValueText;

    [Header("InventoryPage Controls")]
    [SerializeField] private Selectable inventoryBackButton;
    [SerializeField] private TMP_Text miracleDeskListText;
    [SerializeField] private TMP_Text recoveryItemListText;
    [SerializeField] private TMP_Text medalCountText;
    [SerializeField] private TMP_Text juiceListText;

    [Header("Input")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private string gameplayActionMapName = "Player";
    [SerializeField] private string uiActionMapName = "UI";
    [SerializeField] private bool pauseGameWhileUIOpen = true;

    [Header("Gameplay Maps To Restore On Close")]
    [SerializeField]
    private string[] gameplayMapsToRestore =
    {
        "Player",
        "Mawaru"
    };

    [Header("Scene Names")]
    [SerializeField] private string stageSelectSceneName = "StageSelect";
    [SerializeField] private string titleSceneName = "Title";

    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string bgmVolumeParameter = "BGMVolume";
    [SerializeField] private string seVolumeParameter = "SEVolume";

    [Header("Inventory Debug Data")]
    [SerializeField] private List<string> debugMiracleDeskNames = new List<string>();
    [SerializeField] private List<string> debugRecoveryItemNames = new List<string>();
    [SerializeField] private int debugMedalCount = 0;
    [SerializeField] private List<string> debugJuiceNames = new List<string>();

    private readonly List<string> runtimeMiracleDeskNames = new List<string>();
    private readonly List<string> runtimeRecoveryItemNames = new List<string>();
    private readonly List<string> runtimeJuiceNames = new List<string>();
    private int runtimeMedalCount = 0;
    private bool hasRuntimeInventoryData = false;

    private Coroutine selectCoroutine;
    private string lastGameplayActionMapName = string.Empty;
    private bool isMenuOpen = false;

    private const string BgmVolumeSaveKey = "GirlsGear_BGMVolume";
    private const string SeVolumeSaveKey = "GirlsGear_SEVolume";
    private const string FullScreenSaveKey = "GirlsGear_FullScreen";
    private const float MinSliderValue = 0.0001f;
    private const float MaxSliderValue = 1.0f;

    private void Awake()
    {
        RegisterSliderCallbacks();
        LoadOptionSettings();
        RefreshInventoryPage();
        RebuildNavigation();

        if (mixJuicePageController == null && mixJuicePage != null)
        {
            mixJuicePageController = mixJuicePage.GetComponent<MixJuicePageController>();
        }

        ForceClosedAtBoot();
    }

    private void Start()
    {
        ForceClosedAtBoot();
    }

    private void OnDestroy()
    {
        UnregisterSliderCallbacks();
    }

    private void ForceClosedAtBoot()
    {
        isMenuOpen = false;
        lastGameplayActionMapName = string.IsNullOrWhiteSpace(gameplayActionMapName) ? "Player" : gameplayActionMapName;

        if (pauseGameWhileUIOpen)
        {
            Time.timeScale = 1f;
        }

        if (selectCoroutine != null)
        {
            StopCoroutine(selectCoroutine);
            selectCoroutine = null;
        }

        HideAllPages();
        ClearCurrentSelection();

        if (girlsGearUIRoot != null)
        {
            girlsGearUIRoot.SetActive(false);
        }

        NotifyGameManagerMenuOpen(false);
        RestoreGameplayInput();
    }

    public void OpenGirlsGearUI()
    {
        if (isMenuOpen)
        {
            return;
        }

        CaptureCurrentGameplayMap();

        if (girlsGearUIRoot != null)
        {
            girlsGearUIRoot.SetActive(true);
        }

        if (pauseGameWhileUIOpen)
        {
            Time.timeScale = 0f;
        }

        NotifyGameManagerMenuOpen(true);
        SwitchToUIMap();
        OpenStartPage();
        isMenuOpen = true;
    }

    public void CloseGirlsGearUI()
    {
        if (!isMenuOpen && (girlsGearUIRoot == null || !girlsGearUIRoot.activeSelf))
        {
            NotifyGameManagerMenuOpen(false);
            RestoreGameplayInput();
            return;
        }

        if (pauseGameWhileUIOpen)
        {
            Time.timeScale = 1f;
        }

        if (selectCoroutine != null)
        {
            StopCoroutine(selectCoroutine);
            selectCoroutine = null;
        }

        HideAllPages();
        ClearCurrentSelection();

        if (girlsGearUIRoot != null)
        {
            girlsGearUIRoot.SetActive(false);
        }

        NotifyGameManagerMenuOpen(false);
        RestoreGameplayInput();
        isMenuOpen = false;
    }

    public void ToggleGirlsGearUI()
    {
        bool open = girlsGearUIRoot != null && girlsGearUIRoot.activeSelf;
        if (open) CloseGirlsGearUI();
        else OpenGirlsGearUI();
    }

    public void OpenMenu() => OpenGirlsGearUI();
    public void CloseMenu() => CloseGirlsGearUI();
    public void ToggleMenu() => ToggleGirlsGearUI();

    public void OpenSystemPage() => OpenSettingsPage();
    public void OpenStartPage() => ShowOnly(startPage, startFirst);

    public void OpenMixJuicePage()
    {
        HideAllPages();

        if (mixJuicePage != null)
        {
            mixJuicePage.SetActive(true);
        }

        if (mixJuicePageController != null)
        {
            mixJuicePageController.OpenFresh();
        }
        else
        {
            Selectable first = mixJuiceFirst != null ? mixJuiceFirst : FindFirstSelectableInPage(mixJuicePage);
            SelectOnNextFrame(first);
        }
    }

    public void OpenStickerBookPage() => ShowOnly(stickerBookPage, stickerBookFirst);
    public void OpenStickerPlacementPage() => ShowOnly(stickerPlacementPage, stickerPlacementFirst);
    public void OpenMdDeskPage() => ShowOnly(mdDeskPage, mdDeskFirst);

    public void OpenSettingsPage()
    {
        RebuildNavigation();
        Selectable first = settingsFirst != null ? settingsFirst : optionButton;
        ShowOnly(settingsPage, first);
    }

    public void OpenOptionPage()
    {
        RebuildNavigation();
        RefreshOptionPage();
        Selectable first = optionFirst != null ? optionFirst : bgmSlider;
        ShowOnly(optionPage, first);
    }

    public void OpenInventoryPage()
    {
        RebuildNavigation();
        RefreshInventoryPage();
        Selectable first = inventoryFirst != null ? inventoryFirst : inventoryBackButton;
        ShowOnly(inventoryPage, first);
    }

    public void BackToSettingsPage() => OpenSettingsPage();
    public void BackToRoot() => OpenStartPage();

    public void BackToStageSelect()
    {
        PrepareSceneChange();
        SceneManager.LoadScene(stageSelectSceneName);
    }

    public void BackToTitleScene()
    {
        PrepareSceneChange();
        SceneManager.LoadScene(titleSceneName);
    }

    public void SetFullscreenMode() => ApplyDisplayMode(true, true);
    public void SetWindowedMode() => ApplyDisplayMode(false, true);

    public void ToggleDisplayMode()
    {
        bool nextFullscreen = !Screen.fullScreen;
        ApplyDisplayMode(nextFullscreen, true);
    }

    public void OnBgmSliderChanged(float value)
    {
        value = Mathf.Clamp(value, MinSliderValue, MaxSliderValue);
        PlayerPrefs.SetFloat(BgmVolumeSaveKey, value);
        PlayerPrefs.Save();
        ApplyMixerVolume(bgmVolumeParameter, value);
    }

    public void OnSeSliderChanged(float value)
    {
        value = Mathf.Clamp(value, MinSliderValue, MaxSliderValue);
        PlayerPrefs.SetFloat(SeVolumeSaveKey, value);
        PlayerPrefs.Save();
        ApplyMixerVolume(seVolumeParameter, value);
    }

    public void SetInventoryData(List<string> miracleDesks, List<string> recoveryItems, int medalCount, List<string> juices)
    {
        runtimeMiracleDeskNames.Clear();
        runtimeRecoveryItemNames.Clear();
        runtimeJuiceNames.Clear();

        if (miracleDesks != null) runtimeMiracleDeskNames.AddRange(miracleDesks);
        if (recoveryItems != null) runtimeRecoveryItemNames.AddRange(recoveryItems);
        if (juices != null) runtimeJuiceNames.AddRange(juices);

        runtimeMedalCount = medalCount;
        hasRuntimeInventoryData = true;
        RefreshInventoryPage();
    }

    public void UseJuiceAtFromButton(int ownedSlotIndex)
    {
        if (TryUseJuiceAt(ownedSlotIndex, out JuiceUseManager.UseResult result))
        {
            Debug.Log($"[MenuManager] {result.title} {result.message}");
        }
        else if (result != null)
        {
            Debug.LogWarning($"[MenuManager] {result.title} {result.message}");
        }
    }

    public bool TryUseJuiceAt(int ownedSlotIndex, out JuiceUseManager.UseResult result)
    {
        result = null;

        if (JuiceUseManager.Instance == null)
            return false;

        bool success = JuiceUseManager.Instance.TryUseJuiceAt(ownedSlotIndex, out result);
        RefreshInventoryPage();
        return success;
    }

    public void ForceRefreshInventoryPage()
    {
        RefreshInventoryPage();
    }

    private void RegisterSliderCallbacks()
    {
        if (bgmSlider != null)
        {
            bgmSlider.minValue = MinSliderValue;
            bgmSlider.maxValue = MaxSliderValue;
            bgmSlider.wholeNumbers = false;
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (seSlider != null)
        {
            seSlider.minValue = MinSliderValue;
            seSlider.maxValue = MaxSliderValue;
            seSlider.wholeNumbers = false;
            seSlider.onValueChanged.AddListener(OnSeSliderChanged);
        }
    }

    private void UnregisterSliderCallbacks()
    {
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);
        if (seSlider != null) seSlider.onValueChanged.RemoveListener(OnSeSliderChanged);
    }

    private void LoadOptionSettings()
    {
        float bgmValue = PlayerPrefs.GetFloat(BgmVolumeSaveKey, 0.8f);
        float seValue = PlayerPrefs.GetFloat(SeVolumeSaveKey, 0.8f);
        bool fullscreen = PlayerPrefs.GetInt(FullScreenSaveKey, 1) == 1;

        if (bgmSlider != null)
            bgmSlider.SetValueWithoutNotify(Mathf.Clamp(bgmValue, MinSliderValue, MaxSliderValue));

        if (seSlider != null)
            seSlider.SetValueWithoutNotify(Mathf.Clamp(seValue, MinSliderValue, MaxSliderValue));

        ApplyMixerVolume(bgmVolumeParameter, bgmValue);
        ApplyMixerVolume(seVolumeParameter, seValue);
        ApplyDisplayMode(fullscreen, false);
    }

    private void RefreshOptionPage()
    {
        LoadOptionSettings();
        RefreshDisplayModeLabel();
    }

    private void RefreshInventoryPage()
    {
        SyncRuntimeInventoryFromGameSystems();

        List<string> mdList = hasRuntimeInventoryData ? runtimeMiracleDeskNames : debugMiracleDeskNames;
        List<string> recoveryList = hasRuntimeInventoryData ? runtimeRecoveryItemNames : debugRecoveryItemNames;
        List<string> juiceList = hasRuntimeInventoryData ? runtimeJuiceNames : debugJuiceNames;
        int medalValue = hasRuntimeInventoryData ? runtimeMedalCount : debugMedalCount;

        if (miracleDeskListText != null) miracleDeskListText.text = BuildListText(mdList);
        if (recoveryItemListText != null) recoveryItemListText.text = BuildListText(recoveryList);
        if (juiceListText != null) juiceListText.text = BuildListText(juiceList);
        if (medalCountText != null) medalCountText.text = medalValue.ToString();
    }

    private void SyncRuntimeInventoryFromGameSystems()
    {
        JuiceInventory inventory = JuiceInventory.Instance;
        JuiceUseManager juiceUseManager = JuiceUseManager.Instance;

        if (inventory == null && juiceUseManager == null)
            return;

        runtimeRecoveryItemNames.Clear();
        runtimeJuiceNames.Clear();

        if (inventory != null)
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                JuiceInventory.JuiceDefinition definition = inventory.GetOwnedDefinitionAt(i);
                if (definition == null)
                    continue;

                bool usedBefore = juiceUseManager != null && juiceUseManager.HasUsedBefore(definition.id);

                if (usedBefore)
                {
                    if (juiceUseManager != null)
                        runtimeRecoveryItemNames.Add(juiceUseManager.BuildRecoveryInventoryLabel(definition));
                    else
                        runtimeRecoveryItemNames.Add(definition.displayName);
                }
                else
                {
                    if (juiceUseManager != null)
                        runtimeJuiceNames.Add(juiceUseManager.BuildUnlockInventoryLabel(definition));
                    else
                        runtimeJuiceNames.Add(definition.displayName);
                }
            }
        }

        hasRuntimeInventoryData = true;
    }

    private string BuildListText(List<string> source)
    {
        if (source == null || source.Count == 0) return "なし";
        return string.Join("\n", source);
    }

    private void ApplyMixerVolume(string parameterName, float sliderValue)
    {
        if (audioMixer == null || string.IsNullOrEmpty(parameterName)) return;
        sliderValue = Mathf.Clamp(sliderValue, MinSliderValue, MaxSliderValue);
        float decibel = Mathf.Log10(sliderValue) * 20f;
        audioMixer.SetFloat(parameterName, decibel);
    }

    private void ApplyDisplayMode(bool fullscreen, bool save)
    {
        if (fullscreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
        }

        if (save)
        {
            PlayerPrefs.SetInt(FullScreenSaveKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        RefreshDisplayModeLabel();
    }

    private void RefreshDisplayModeLabel()
    {
        if (displayModeValueText != null)
            displayModeValueText.text = Screen.fullScreen ? "フルスクリーン" : "ウィンドウモード";
    }

    private void RebuildNavigation()
    {
        ApplyVerticalNavigation(optionButton, inventoryButton, stageBackButton, titleBackButton);
        ApplyVerticalNavigation(bgmSlider, seSlider, fullScreenButton, windowedModeButton, optionBackButton);
        ApplyVerticalNavigation(inventoryBackButton);
    }

    private void ApplyVerticalNavigation(params Selectable[] selectables)
    {
        List<Selectable> validList = new List<Selectable>();
        foreach (Selectable selectable in selectables)
        {
            if (selectable != null) validList.Add(selectable);
        }

        for (int i = 0; i < validList.Count; i++)
        {
            Navigation nav = validList[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnUp = i > 0 ? validList[i - 1] : null;
            nav.selectOnDown = i < validList.Count - 1 ? validList[i + 1] : null;
            nav.selectOnLeft = null;
            nav.selectOnRight = null;
            validList[i].navigation = nav;
        }
    }

    private void ShowOnly(GameObject pageToOpen, Selectable firstSelectable)
    {
        HideAllPages();

        if (pageToOpen != null)
            pageToOpen.SetActive(true);

        if (firstSelectable == null && pageToOpen != null)
            firstSelectable = FindFirstSelectableInPage(pageToOpen);

        SelectOnNextFrame(firstSelectable);
    }

    private void HideAllPages()
    {
        SetPageActive(startPage, false);
        SetPageActive(mixJuicePage, false);
        SetPageActive(stickerBookPage, false);
        SetPageActive(stickerPlacementPage, false);
        SetPageActive(mdDeskPage, false);
        SetPageActive(settingsPage, false);
        SetPageActive(optionPage, false);
        SetPageActive(inventoryPage, false);
    }

    private void SetPageActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }

    private Selectable FindFirstSelectableInPage(GameObject page)
    {
        if (page == null) return null;

        Selectable[] selectables = page.GetComponentsInChildren<Selectable>(true);
        foreach (Selectable selectable in selectables)
        {
            if (selectable != null && selectable.IsInteractable())
                return selectable;
        }

        return null;
    }

    private void SelectOnNextFrame(Selectable target)
    {
        if (selectCoroutine != null)
            StopCoroutine(selectCoroutine);

        selectCoroutine = StartCoroutine(SelectRoutine(target));
    }

    private IEnumerator SelectRoutine(Selectable target)
    {
        yield return null;

        if (EventSystem.current == null)
            yield break;

        EventSystem.current.sendNavigationEvents = true;
        EventSystem.current.SetSelectedGameObject(null);

        yield return null;

        if (target != null && target.gameObject.activeInHierarchy && target.IsInteractable())
            EventSystem.current.SetSelectedGameObject(target.gameObject);
    }

    private void ClearCurrentSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private bool EnsurePlayerInputReady()
    {
        if (playerInput == null)
            playerInput = FindObjectOfType<PlayerInput>(true);

        if (playerInput == null)
            return false;

        if (!playerInput.enabled)
            playerInput.enabled = true;

        if (!playerInput.gameObject.activeInHierarchy)
            return false;

        if (!playerInput.inputIsActive)
            playerInput.ActivateInput();

        return playerInput.actions != null;
    }

    private void CaptureCurrentGameplayMap()
    {
        if (!EnsurePlayerInputReady())
            return;

        if (playerInput.currentActionMap == null)
        {
            lastGameplayActionMapName = gameplayActionMapName;
            return;
        }

        string currentName = playerInput.currentActionMap.name;
        if (string.IsNullOrWhiteSpace(currentName) || currentName == uiActionMapName)
            return;

        lastGameplayActionMapName = currentName;
    }

    private void SwitchToUIMap()
    {
        if (!EnsurePlayerInputReady())
            return;

        InputActionMap uiMap = playerInput.actions.FindActionMap(uiActionMapName, false);
        if (uiMap == null)
            return;

        uiMap.Enable();

        for (int i = 0; i < gameplayMapsToRestore.Length; i++)
        {
            string mapName = gameplayMapsToRestore[i];
            if (string.IsNullOrWhiteSpace(mapName) || mapName == uiActionMapName)
                continue;

            InputActionMap map = playerInput.actions.FindActionMap(mapName, false);
            if (map != null)
                map.Disable();
        }

        playerInput.SwitchCurrentActionMap(uiActionMapName);
    }

    private void RestoreGameplayInput()
    {
        if (!EnsurePlayerInputReady())
            return;

        InputActionMap uiMap = playerInput.actions.FindActionMap(uiActionMapName, false);
        if (uiMap != null)
        {
            foreach (InputAction action in uiMap.actions)
            {
                if (action != null)
                    action.Disable();
            }

            uiMap.Disable();
        }

        string restoreMapName = string.IsNullOrWhiteSpace(lastGameplayActionMapName)
            ? gameplayActionMapName
            : lastGameplayActionMapName;

        InputActionMap restoreMap = playerInput.actions.FindActionMap(restoreMapName, false);
        if (restoreMap == null)
            restoreMap = playerInput.actions.FindActionMap(gameplayActionMapName, false);

        for (int i = 0; i < gameplayMapsToRestore.Length; i++)
        {
            string mapName = gameplayMapsToRestore[i];
            if (string.IsNullOrWhiteSpace(mapName) || mapName == uiActionMapName)
                continue;

            InputActionMap map = playerInput.actions.FindActionMap(mapName, false);
            if (map == null)
                continue;

            map.Enable();
            foreach (InputAction action in map.actions)
            {
                if (action != null)
                    action.Enable();
            }
        }

        if (restoreMap != null)
        {
            restoreMap.Enable();
            playerInput.SwitchCurrentActionMap(restoreMap.name);
        }
    }

    private void NotifyGameManagerMenuOpen(bool isOpen)
    {
        GameManager gm = FindObjectOfType<GameManager>(true);
        if (gm != null)
        {
            gm.SetItemPanelOpen(isOpen);
        }

        PlayerController.gameState = isOpen ? "pause" : "playing";
    }

    private void PrepareSceneChange()
    {
        if (pauseGameWhileUIOpen)
            Time.timeScale = 1f;

        ClearCurrentSelection();
        HideAllPages();

        if (girlsGearUIRoot != null)
            girlsGearUIRoot.SetActive(false);

        NotifyGameManagerMenuOpen(false);
        RestoreGameplayInput();
        isMenuOpen = false;
    }
}