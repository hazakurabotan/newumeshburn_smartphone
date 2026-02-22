using TMPro;
using UnityEngine;

public class BeamAmmoText : MonoBehaviour
{
    [SerializeField] private BeamAmmo ammo;
    [SerializeField] private TextMeshProUGUI ammoText;

    void Awake()
    {
        if (!ammoText) ammoText = GetComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        if (!ammo) return;
        ammo.OnAmmoChanged += Refresh;
        Refresh(ammo.currentAmmo, ammo.maxAmmo);
    }

    void OnDisable()
    {
        if (!ammo) return;
        ammo.OnAmmoChanged -= Refresh;
    }

    void Refresh(int cur, int max)
    {
        if (!ammoText) return;
        ammoText.text = $"{cur}/{max}";
    }
}