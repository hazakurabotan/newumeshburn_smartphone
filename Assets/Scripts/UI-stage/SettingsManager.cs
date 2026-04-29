using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Audio (required)")]
    public AudioMixer mixer;
    [Tooltip("Exposed parameter names on the AudioMixer")]
    public string masterParam = "MasterVol";
    public string bgmParam = "BGMVol";
    public string seParam = "SEVol";
    public string voiceParam = "VoiceVol";

    [Header("UI - Audio")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider seSlider;
    public Slider voiceSlider;

    [Header("UI - System")]
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown languageDropdown;

    [Header("Brightness (Overlay)")]
    public Slider brightnessSlider;
    public Image brightnessOverlay; // full-screen black Image, RaycastTarget = false

    // PlayerPrefs keys
    const string KEY_MASTER = "SET_master";
    const string KEY_BGM = "SET_bgm";
    const string KEY_SE = "SET_se";
    const string KEY_VOICE = "SET_voice";
    const string KEY_BRIGHT = "SET_bright";
    const string KEY_QUALITY = "SET_quality";
    const string KEY_LANG = "SET_lang";

    void Awake()
    {
        // Load saved values (defaults)
        float master = PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        float bgm = PlayerPrefs.GetFloat(KEY_BGM, 1f);
        float se = PlayerPrefs.GetFloat(KEY_SE, 1f);
        float voice = PlayerPrefs.GetFloat(KEY_VOICE, 1f);
        float bright = PlayerPrefs.GetFloat(KEY_BRIGHT, 1f);
        int q = PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
        int lang = PlayerPrefs.GetInt(KEY_LANG, 0);

        // Reflect to sliders/dropdowns
        if (masterSlider) masterSlider.value = master;
        if (bgmSlider) bgmSlider.value = bgm;
        if (seSlider) seSlider.value = se;
        if (voiceSlider) voiceSlider.value = voice;
        if (brightnessSlider) brightnessSlider.value = bright;
        if (qualityDropdown) qualityDropdown.value = q;
        if (languageDropdown) languageDropdown.value = lang;

        // Apply
        ApplyVolume(masterParam, master);
        ApplyVolume(bgmParam, bgm);
        ApplyVolume(seParam, se);
        ApplyVolume(voiceParam, voice);
        ApplyBrightness(bright);
        QualitySettings.SetQualityLevel(q, true);
        ApplyLanguage(lang);
    }

    // ====== UI events (hook these to OnValueChanged) ======
    public void OnMasterChanged(float v) { PlayerPrefs.SetFloat(KEY_MASTER, v); ApplyVolume(masterParam, v); }
    public void OnBgmChanged(float v) { PlayerPrefs.SetFloat(KEY_BGM, v); ApplyVolume(bgmParam, v); }
    public void OnSeChanged(float v) { PlayerPrefs.SetFloat(KEY_SE, v); ApplyVolume(seParam, v); }
    public void OnVoiceChanged(float v) { PlayerPrefs.SetFloat(KEY_VOICE, v); ApplyVolume(voiceParam, v); }
    public void OnBrightnessChanged(float v) { PlayerPrefs.SetFloat(KEY_BRIGHT, v); ApplyBrightness(v); }

    public void OnQualityChanged(int index)
    {
        PlayerPrefs.SetInt(KEY_QUALITY, index);
        QualitySettings.SetQualityLevel(index, true);
    }

    public void OnLanguageChanged(int index)
    {
        PlayerPrefs.SetInt(KEY_LANG, index);
        ApplyLanguage(index);
    }

    // ====== Impl ======
    void ApplyVolume(string param, float slider01)
    {
        if (!mixer)
        {
            Debug.LogError("[SettingsManager] AudioMixer is not assigned.");
            return;
        }

        // Map [0..1] -> [-80..0] dB（直感的な直線補間。対数にしたい場合は20*log10）
        float dB = Mathf.Lerp(-80f, 0f, Mathf.Clamp01(slider01));

        if (!mixer.SetFloat(param, dB))
        {
            Debug.LogError($"[SettingsManager] Exposed parameter '{param}' was not found on {mixer.name}.");
        }
    }

    void ApplyBrightness(float slider01)
    {
        if (!brightnessOverlay) return;

        slider01 = Mathf.Clamp01(slider01);
        // 1 = そのまま（透明）, 0 = 真っ黒（全暗）
        var c = brightnessOverlay.color;
        c.a = 1f - slider01;
        brightnessOverlay.color = c;
    }

    void ApplyLanguage(int index)
    {
        // ※プロジェクトのローカライズ実装に合わせてここで反映。
        // （いまはログだけ）
        Debug.Log($"[Settings] Language set index: {index}");
    }
}
