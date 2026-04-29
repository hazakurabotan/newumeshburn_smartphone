using UnityEngine;
using UnityEngine.SceneManagement;

public class StageEdgeReturn : MonoBehaviour
{
    [Header("–ß‚èæƒV[ƒ“–¼")]
    public string targetSceneName = "StageSelect";

    [Header("ŠJn’¼Œã‚ÌŒë”š–h~(•b)")]
    public float ignoreTimeAfterStart = 0.3f;

    bool _loading;
    float _startTime;

    void Awake()
    {
        _startTime = Time.time;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_loading) return;
        if (Time.time - _startTime < ignoreTimeAfterStart) return;

        // PlayerController ‚© MawaruController ‚ğ‚Á‚Ä‚é•¨‘Ì‚¾‚¯”½‰
        if (other.GetComponentInParent<PlayerController>() != null ||
            other.GetComponentInParent<MawaruController>() != null)
        {
            _loading = true;
            SceneManager.LoadScene(targetSceneName);
        }
    }
}