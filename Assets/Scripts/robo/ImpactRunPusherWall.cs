using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class ImpactRunPusherWall : MonoBehaviour
{
    public Transform target;
    [Min(0f)] public float moveSpeed = 2.8f;
    [Min(0f)] public float startBehindDistance = 1.2f;
    public bool snapBehindTargetOnStart = true;

    [Header("Push Assist")]
    [Min(0f)] public float pushSignalDistance = 1.4f;
    [Min(0f)] public float hardSnapIfAheadDistance = 0.02f;

    [Header("Respawn Support")]
    [Min(0f)] public float defaultRespawnPause = 0.35f;
    [Min(0f)] public float defaultRespawnExtraBack = 0.5f;

    private Rigidbody2D rb;
    private ImpactRunnerController runner;
    private float pauseUntil;

    public bool IsPaused => Time.time < pauseUntil;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        if (target == null)
        {
            runner = FindObjectOfType<ImpactRunnerController>();
            if (runner != null)
                target = runner.transform;
        }
        else
        {
            runner = target.GetComponentInParent<ImpactRunnerController>();
        }

        if (snapBehindTargetOnStart && target != null)
        {
            Vector2 p = rb.position;
            p.x = target.position.x - startBehindDistance;
            rb.position = p;
        }
    }

    private void FixedUpdate()
    {
        if (target == null)
            return;

        if (runner == null)
            runner = target.GetComponentInParent<ImpactRunnerController>();

        if (IsPaused)
            return;

        float desiredBehindX = target.position.x - startBehindDistance;
        float nextX = rb.position.x + moveSpeed * Time.fixedDeltaTime;
        float finalX = Mathf.Min(nextX, desiredBehindX);

        if (rb.position.x > desiredBehindX + hardSnapIfAheadDistance)
            finalX = desiredBehindX;

        rb.MovePosition(new Vector2(finalX, rb.position.y));

        float gap = target.position.x - rb.position.x;
        if (runner != null && gap <= pushSignalDistance)
            runner.SetPusherSpeed(moveSpeed);
    }

    public void PauseWall(float duration)
    {
        pauseUntil = Mathf.Max(pauseUntil, Time.time + duration);
    }

    public void RewindBehindTarget(float extraBack = 0f)
    {
        if (target == null)
            return;

        Vector2 p = rb.position;
        p.x = target.position.x - startBehindDistance - extraBack;
        rb.position = p;
    }

    public void RewindBehindX(float targetX, float extraBack = 0f)
    {
        Vector2 p = rb.position;
        p.x = targetX - startBehindDistance - extraBack;
        rb.position = p;
    }

    public void PauseAndRewindBehindTarget(float duration, float extraBack = -1f)
    {
        PauseWall(duration);
        if (extraBack < 0f)
            extraBack = defaultRespawnExtraBack;

        RewindBehindTarget(extraBack);
    }

    public void PauseAndRewindBehindX(float targetX, float duration, float extraBack = -1f)
    {
        PauseWall(duration);
        if (extraBack < 0f)
            extraBack = defaultRespawnExtraBack;

        RewindBehindX(targetX, extraBack);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (IsPaused)
            return;

        ImpactRunnerController hitRunner = collision.collider.GetComponentInParent<ImpactRunnerController>();
        if (hitRunner != null)
            hitRunner.SetPusherSpeed(moveSpeed);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (IsPaused)
            return;

        ImpactRunnerController hitRunner = other.GetComponentInParent<ImpactRunnerController>();
        if (hitRunner != null)
            hitRunner.SetPusherSpeed(moveSpeed);
    }
}