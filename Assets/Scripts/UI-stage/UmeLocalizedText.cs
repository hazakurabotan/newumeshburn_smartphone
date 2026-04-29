using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class UmeLocalizedText : MonoBehaviour
{
    public string japanese;
    public string english;

    TMP_Text _text;

    void Awake()
    {
        _text = GetComponent<TMP_Text>();
        Apply(UmeLanguage.Index);
        UmeLanguage.Changed += Apply;
    }

    void OnDestroy()
    {
        UmeLanguage.Changed -= Apply;
    }

    void Apply(int idx)
    {
        _text.text = (idx == 0) ? japanese : english;
    }
}
