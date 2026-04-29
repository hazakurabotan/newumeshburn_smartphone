using UnityEngine;

[DisallowMultipleComponent]
public class ImpactRunWolfEnemy : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer spriteRenderer;
    public Camera targetCamera;

    [Header("Sprites")]
    public Sprite ookami1;
    public Sprite ookami2;
    public Sprite ookami3;

    [Header("Animation")]
    [Min(1f)] public float animationFPS = 8f;
    public bool faceLeft = true;

    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 4.5f;
    [Min(0f)] public float hopHeight = 0.35f;
    [Min(0f)] public float hopFrequency = 4.5f;

    [Header("Cleanup")]
    public bool offscreenLeftSafety = true;
    [Min(0f)] public float leftDespawnMargin = 2f;

    private Sprite[] frames;
    private float baseY;
    private float lifeTime;
    private float frameTimer;
    private int frameIndex;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        frames = new Sprite[] { ookami1, ookami2, ookami3 };
        baseY = transform.position.y;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = faceLeft;

            if (frames[0] != null)
                spriteRenderer.sprite = frames[0];
        }
    }

    private void Update()
    {
        lifeTime += Time.deltaTime;

        MoveLeftWithHop();
        AnimateWolf();

        if (offscreenLeftSafety)
            CheckOffscreenLeft();
    }

    private void MoveLeftWithHop()
    {
        Vector3 p = transform.position;
        p.x -= moveSpeed * Time.deltaTime;
        p.y = baseY + Mathf.Abs(Mathf.Sin(lifeTime * hopFrequency)) * hopHeight;
        transform.position = p;
    }

    private void AnimateWolf()
    {
        if (spriteRenderer == null || frames == null || frames.Length == 0)
            return;

        float frameDuration = 1f / Mathf.Max(1f, animationFPS);
        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex = (frameIndex + 1) % frames.Length;

            Sprite next = frames[frameIndex];
            if (next != null)
                spriteRenderer.sprite = next;
        }
    }

    private void CheckOffscreenLeft()
    {
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        float halfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        float leftEdge = targetCamera.transform.position.x - halfWidth - leftDespawnMargin;

        if (transform.position.x < leftEdge)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPusherWall(other.gameObject))
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsPusherWall(collision.collider.gameObject))
            Destroy(gameObject);
    }

    private bool IsPusherWall(GameObject go)
    {
        return go.GetComponent<ImpactRunPusherWall>() != null
            || go.GetComponentInParent<ImpactRunPusherWall>() != null;
    }
}