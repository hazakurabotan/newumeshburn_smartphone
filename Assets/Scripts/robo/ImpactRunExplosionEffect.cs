using UnityEngine;

public class ImpactRunExplosionEffect : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;

    [Header("Frames")]
    public Sprite[] frames;

    [Header("Playback")]
    [Min(1f)] public float fps = 16f;
    public bool destroyAtEnd = true;

    private int frameIndex;
    private float timer;
    private bool playing;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        Play();
    }

    public void Play()
    {
        playing = true;
        frameIndex = 0;
        timer = 0f;

        if (spriteRenderer != null && frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        if (!playing || spriteRenderer == null || frames == null || frames.Length == 0)
            return;

        float frameDuration = 1f / Mathf.Max(1f, fps);
        timer += Time.deltaTime;

        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            frameIndex++;

            if (frameIndex >= frames.Length)
            {
                playing = false;

                if (destroyAtEnd)
                    Destroy(gameObject);

                return;
            }

            spriteRenderer.sprite = frames[frameIndex];
        }
    }
}