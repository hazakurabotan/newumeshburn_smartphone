using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [SerializeField] AudioMixer mixer;
    [SerializeField] string exposedParam = "BGMVol"; // Ç±Ç±Ç BGMVol/SEVol/VoiceVol/MasterVol Ç…ïœÇ¶ÇÈ
    [SerializeField] Slider slider;

    const float MIN_LINEAR = 0.0001f; // -80dBëäìñÇÃâ∫å¿
    const float MAX_LINEAR = 1f;

    void Awake()
    {
        if (!slider) slider = GetComponent<Slider>();
        slider.minValue = MIN_LINEAR;
        slider.maxValue = MAX_LINEAR;

        float saved = PlayerPrefs.GetFloat(exposedParam, 0.8f);
        slider.value = saved;
        Apply(saved);

        slider.onValueChanged.AddListener(Apply);
    }

    void Apply(float linear)
    {
        // ê¸å`0.0001Å`1.0 -> dB(-80Å`0)
        float db = Mathf.Log10(Mathf.Clamp(linear, MIN_LINEAR, MAX_LINEAR)) * 20f;
        mixer.SetFloat(exposedParam, db);
        PlayerPrefs.SetFloat(exposedParam, linear);
    }
}