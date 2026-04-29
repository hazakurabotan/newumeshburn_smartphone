using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class SettingsPageController : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject settingsPageRoot;
    [SerializeField] private GameObject mainPanelRoot;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private GameObject inventoryPageRoot;

    [Header("Buttons")]
    [SerializeField] private Button optionButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button stageSelectBackButton;
    [SerializeField] private Button titleBackButton;
    [SerializeField] private Button closeOptionButton;

    [Header("Inventory Page")]
    [SerializeField] private ItemInventoryPageController inventoryPageController;

    [Header("Option Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string bgmVolumeParameter = "BGMVolume";
    [SerializeField] private string seVolumeParameter = "SEVolume";

    [Header("Scene Names")]
    [SerializeField] private string stageSelectSceneName = "StageSelect";
    [SerializeField] private string titleSceneName = "Title";

    [Header("Selection")]
    [SerializeField] private GameObject mainFirstSelected;
    [SerializeField] private GameObject optionFirstSelected;

    [Header("Save")]
    [SerializeField] private bool saveVolumeToPlayerPrefs = true;

    private const string BgmVolumeKey = "Settings_BGMVolume";
    private const string SeVolumeKey = "Settings_SEVolume";
    private const float MinDb = -80f;
    private const float MaxDb = 0f;

    private bool isBound;

    private void Awake()
    {
        if (settingsPageRoot == null)
        {
            settingsPageRoot = gameObject;
        }

        SetupSliderDefaults();
        LoadSavedVolumesAndApply();
        ShowMainPageInstant();
    }

    private void OnEnable()
    {
        BindEvents();
        LoadSavedVolumesAndApply();
        ShowMainPageInstant();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    private void SetupSliderDefaults()
    {
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
        }

        if (seSlider != null)
        {
            seSlider.minValue = 0f;
            seSlider.maxValue = 1f;
        }
    }

    private void BindEvents()
    {
        if (isBound) return;
        isBound = true;

        if (optionButton != null)
        {
            optionButton.onClick.RemoveListener(OpenOptionPanel);
            optionButton.onClick.AddListener(OpenOptionPanel);
        }

        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveListener(OpenInventoryPage);
            inventoryButton.onClick.AddListener(OpenInventoryPage);
        }

        if (stageSelectBackButton != null)
        {
            stageSelectBackButton.onClick.RemoveListener(ReturnToStageSelect);
            stageSelectBackButton.onClick.AddListener(ReturnToStageSelect);
        }

        if (titleBackButton != null)
        {
            titleBackButton.onClick.RemoveListener(ReturnToTitle);
            titleBackButton.onClick.AddListener(ReturnToTitle);
        }

        if (closeOptionButton != null)
        {
            closeOptionButton.onClick.RemoveListener(CloseOptionPanel);
            closeOptionButton.onClick.AddListener(CloseOptionPanel);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);
            bgmSlider.onValueChanged.AddListener(OnBgmSliderChanged);
        }

        if (seSlider != null)
        {
            seSlider.onValueChanged.RemoveListener(OnSeSliderChanged);
            seSlider.onValueChanged.AddListener(OnSeSliderChanged);
        }
    }

    private void UnbindEvents()
    {
        isBound = false;

        if (optionButton != null)
        {
            optionButton.onClick.RemoveListener(OpenOptionPanel);
        }

        if (inventoryButton != null)
        {
            inventoryButton.onClick.RemoveListener(OpenInventoryPage);
        }

        if (stageSelectBackButton != null)
        {
            stageSelectBackButton.onClick.RemoveListener(ReturnToStageSelect);
        }

        if (titleBackButton != null)
        {
            titleBackButton.onClick.RemoveListener(ReturnToTitle);
        }

        if (closeOptionButton != null)
        {
            closeOptionButton.onClick.RemoveListener(CloseOptionPanel);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBgmSliderChanged);
        }

        if (seSlider != null)
        {
            seSlider.onValueChanged.RemoveListener(OnSeSliderChanged);
        }
    }

    private void LoadSavedVolumesAndApply()
    {
        float bgmValue = saveVolumeToPlayerPrefs ? PlayerPrefs.GetFloat(BgmVolumeKey, 1f) : 1f;
        float seValue = saveVolumeToPlayerPrefs ? PlayerPrefs.GetFloat(SeVolumeKey, 1f) : 1f;

        bgmValue = Mathf.Clamp01(bgmValue);
        seValue = Mathf.Clamp01(seValue);

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(bgmValue);
        }

        if (seSlider != null)
        {
            seSlider.SetValueWithoutNotify(seValue);
        }

        ApplyBgmVolume(bgmValue, false);
        ApplySeVolume(seValue, false);
    }

    private void ShowMainPageInstant()
    {
        if (settingsPageRoot != null)
        {
            settingsPageRoot.SetActive(true);
        }

        if (mainPanelRoot != null)
        {
            mainPanelRoot.SetActive(true);
        }

        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
        }

        if (inventoryPageRoot != null)
        {
            inventoryPageRoot.SetActive(false);
        }

        SetSelected(mainFirstSelected != null ? mainFirstSelected : (optionButton != null ? optionButton.gameObject : null));
    }

    public void OpenOptionPanel()
    {
        if (mainPanelRoot != null)
        {
            mainPanelRoot.SetActive(false);
        }

        if (inventoryPageRoot != null)
        {
            inventoryPageRoot.SetActive(false);
        }

        if (optionPanel != null)
        {
            optionPanel.SetActive(true);
        }

        SetSelected(optionFirstSelected != null ? optionFirstSelected : (bgmSlider != null ? bgmSlider.gameObject : null));
    }

    public void CloseOptionPanel()
    {
        ShowMainPageInstant();
    }

    public void OpenInventoryPage()
    {
        if (mainPanelRoot != null)
        {
            mainPanelRoot.SetActive(false);
        }

        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
        }

        if (inventoryPageRoot != null)
        {
            inventoryPageRoot.SetActive(true);
        }

        if (inventoryPageController != null)
        {
            inventoryPageController.OpenPage();
        }
    }

    public void ReturnFromInventoryPage()
    {
        ShowMainPageInstant();
    }

    public void ReturnToStageSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    public void OnBgmSliderChanged(float value)
    {
        ApplyBgmVolume(value, true);
    }

    public void OnSeSliderChanged(float value)
    {
        ApplySeVolume(value, true);
    }

    private void ApplyBgmVolume(float sliderValue, bool save)
    {
        SetMixerVolume(bgmVolumeParameter, sliderValue);

        if (save && saveVolumeToPlayerPrefs)
        {
            PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(sliderValue));
            PlayerPrefs.Save();
        }
    }

    private void ApplySeVolume(float sliderValue, bool save)
    {
        SetMixerVolume(seVolumeParameter, sliderValue);

        if (save && saveVolumeToPlayerPrefs)
        {
            PlayerPrefs.SetFloat(SeVolumeKey, Mathf.Clamp01(sliderValue));
            PlayerPrefs.Save();
        }
    }

    private void SetMixerVolume(string parameterName, float sliderValue)
    {
        if (audioMixer == null || string.IsNullOrEmpty(parameterName)) return;

        float db = SliderValueToDb(sliderValue);
        audioMixer.SetFloat(parameterName, db);
    }

    private float SliderValueToDb(float sliderValue)
    {
        sliderValue = Mathf.Clamp01(sliderValue);

        if (sliderValue <= 0.0001f)
        {
            return MinDb;
        }

        float db = Mathf.Log10(sliderValue) * 20f;
        return Mathf.Clamp(db, MinDb, MaxDb);
    }

    private void SetSelected(GameObject target)
    {
        if (target == null) return;
        if (EventSystem.current == null) return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target);
    }
}