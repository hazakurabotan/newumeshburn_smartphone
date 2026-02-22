using UnityEngine;

public class GaugePickup : MonoBehaviour
{
    public int addAmount = 15;

    void OnTriggerEnter2D(Collider2D other)
    {
        var gauge = other.GetComponentInParent<SpecialGauge>();
        if (!gauge) return;

        gauge.Add(addAmount);
        Destroy(gameObject);
    }
}