using UnityEngine;
using UnityEngine.Events;

public class SpecialGauge : MonoBehaviour
{
    [Range(0, 100)] public int value = 0;
    public int max = 100;

    public UnityEvent<int, int> onChanged;   // (value,max)
    public UnityEvent onMaxed;

    public bool IsMax => value >= max;

    public void Add(int amount)
    {
        int before = value;
        value = Mathf.Clamp(value + amount, 0, max);
        if (value != before) onChanged?.Invoke(value, max);
        if (before < max && value >= max) onMaxed?.Invoke();
    }

    public bool ConsumeAll()
    {
        if (!IsMax) return false;
        value = 0;
        onChanged?.Invoke(value, max);
        return true;
    }
}
