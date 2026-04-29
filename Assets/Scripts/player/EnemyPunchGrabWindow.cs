using UnityEngine;

public class EnemyPunchGrabWindow : MonoBehaviour
{
    float _until = -1f;

    public void Open(float seconds)
    {
        _until = Time.time + Mathf.Max(0f, seconds);
    }

    public bool IsOpen => Time.time <= _until;

    public void Close() => _until = -1f;
}