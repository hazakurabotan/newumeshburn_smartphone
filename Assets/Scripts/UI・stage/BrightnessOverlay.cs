using UnityEngine;
using UnityEngine.UI;

public class BrightnessOverlay : MonoBehaviour
{
    [SerializeField] Image overlay;   // 黒幕Image
    [SerializeField] Slider slider;   // UIのスライダー
    const string Key = "BrightnessAlpha";

    void Awake()
    {
        if (!overlay) overlay = GetComponent<Image>();
        if (!slider) slider = GetComponent<Slider>();

        // スライダー範囲を0～0.6くらいにすると自然
        slider.minValue = 0f;
        slider.maxValue = 0.6f;

        float saved = PlayerPrefs.GetFloat(Key, 0f);
        slider.value = saved;
        Apply(saved);

        slider.onValueChanged.AddListener(Apply);
    }

    void Apply(float a)
    {
        if (overlay)
        {
            var c = overlay.color;
            c.a = a;
            overlay.color = c;
            PlayerPrefs.SetFloat(Key, a);
        }
    }
}
