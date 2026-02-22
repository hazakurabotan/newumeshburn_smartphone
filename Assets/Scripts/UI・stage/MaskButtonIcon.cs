using UnityEngine;
using UnityEngine.UI;

public class MaskButtonIcon : MonoBehaviour
{
    [SerializeField] MaskManager maskManager;
    [SerializeField] Image buttonImage;  // MaskBtn ‚Ì Image

    void OnEnable()
    {
        if (maskManager != null)
            maskManager.OnEquippedChanged += Handle;

        Refresh(); // ‰Šú”½‰f
    }

    void OnDisable()
    {
        if (maskManager != null)
            maskManager.OnEquippedChanged -= Handle;
    }

    void Handle(MaskItem item) => Refresh();

    void Refresh()
    {
        if (maskManager != null && maskManager.Equipped != null && buttonImage != null)
            buttonImage.sprite = maskManager.Equipped.icon;
    }
}
