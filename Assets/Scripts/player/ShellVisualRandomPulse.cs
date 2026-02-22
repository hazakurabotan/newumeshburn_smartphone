using UnityEngine;

public class ShellVisualRandomPulse : MonoBehaviour
{
    [Header("Sprite")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite[] randomSprites; // IMG_7777 / 7781 / 7786

    [Header("Pulse")]
    [SerializeField] private bool pulse = true;
    [SerializeField, Range(0f, 1f)] private float amplitude = 0.25f;
    [SerializeField] private float speed = 12f;

    private Vector3 baseScale;
    private float startTime;

    void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        if (sr != null && randomSprites != null && randomSprites.Length > 0)
            sr.sprite = randomSprites[Random.Range(0, randomSprites.Length)];

        baseScale = transform.localScale;
        startTime = Time.time;
    }

    void Update()
    {
        if (!pulse) return;

        float s = 1f + amplitude * Mathf.Sin((Time.time - startTime) * speed);
        if (s < 0.05f) s = 0.05f;

        transform.localScale = baseScale * s;
    }
}