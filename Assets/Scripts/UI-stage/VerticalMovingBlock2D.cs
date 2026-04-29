using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class VerticalMovingBlock2D : MonoBehaviour
{
    [Header("上下移動設定")]
    [SerializeField] private float moveDistance = 3.0f;
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private bool startMovingUp = true;
    [SerializeField] private float waitTimeAtEnds = 0.2f;

    [Header("乗っているキャラを一緒に動かす")]
    [SerializeField] private bool carryRiders = true;

    [SerializeField]
    private string[] riderTags =
    {
        "Player",
        "Mawaru",
        "Mawaru13"
    };

    [Header("デバッグ表示")]
    [SerializeField] private bool drawGizmos = true;

    private Rigidbody2D rb;
    private Collider2D blockCollider;

    private Vector2 startPosition;
    private Vector2 endPosition;
    private Vector2 currentTarget;

    private float waitTimer;
    private bool initialized;

    private readonly HashSet<Rigidbody2D> ridingRigidbodies = new HashSet<Rigidbody2D>();
    private readonly List<Rigidbody2D> removeList = new List<Rigidbody2D>();

    private void Reset()
    {
        SetupRequiredComponents();
    }

    private void Awake()
    {
        SetupRequiredComponents();
        InitializeMovePoints();
    }

    private void OnValidate()
    {
        if (moveDistance < 0f)
        {
            moveDistance = 0f;
        }

        if (moveSpeed < 0.01f)
        {
            moveSpeed = 0.01f;
        }

        if (waitTimeAtEnds < 0f)
        {
            waitTimeAtEnds = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!initialized)
        {
            InitializeMovePoints();
        }

        Vector2 currentPosition = rb.position;
        Vector2 nextPosition = currentPosition;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
        }
        else
        {
            nextPosition = Vector2.MoveTowards(
                currentPosition,
                currentTarget,
                moveSpeed * Time.fixedDeltaTime
            );

            if (Vector2.Distance(nextPosition, currentTarget) <= 0.001f)
            {
                nextPosition = currentTarget;
                SwitchTarget();
                waitTimer = waitTimeAtEnds;
            }
        }

        Vector2 moveDelta = nextPosition - currentPosition;

        rb.MovePosition(nextPosition);

        if (carryRiders && moveDelta != Vector2.zero)
        {
            MoveRiders(moveDelta);
        }
    }

    private void SetupRequiredComponents()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        blockCollider = GetComponent<Collider2D>();

        if (blockCollider == null)
        {
            blockCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        blockCollider.isTrigger = false;
    }

    private void InitializeMovePoints()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        startPosition = rb.position;

        float direction = startMovingUp ? 1f : -1f;
        endPosition = startPosition + Vector2.up * moveDistance * direction;

        currentTarget = endPosition;
        waitTimer = 0f;
        initialized = true;
    }

    private void SwitchTarget()
    {
        if (Vector2.Distance(currentTarget, endPosition) <= 0.001f)
        {
            currentTarget = startPosition;
        }
        else
        {
            currentTarget = endPosition;
        }
    }

    private void MoveRiders(Vector2 moveDelta)
    {
        removeList.Clear();

        foreach (Rigidbody2D riderRb in ridingRigidbodies)
        {
            if (riderRb == null)
            {
                removeList.Add(riderRb);
                continue;
            }

            Vector2 riderNextPosition = riderRb.position + moveDelta;
            riderRb.MovePosition(riderNextPosition);
        }

        for (int i = 0; i < removeList.Count; i++)
        {
            ridingRigidbodies.Remove(removeList[i]);
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!carryRiders)
        {
            return;
        }

        Rigidbody2D otherRb = collision.rigidbody;

        if (otherRb == null)
        {
            return;
        }

        if (otherRb == rb)
        {
            return;
        }

        if (!IsRiderTarget(otherRb.gameObject))
        {
            return;
        }

        if (!IsObjectOnTop(collision))
        {
            return;
        }

        ridingRigidbodies.Add(otherRb);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        Rigidbody2D otherRb = collision.rigidbody;

        if (otherRb == null)
        {
            return;
        }

        if (ridingRigidbodies.Contains(otherRb))
        {
            ridingRigidbodies.Remove(otherRb);
        }
    }

    private bool IsRiderTarget(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        if (riderTags == null || riderTags.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < riderTags.Length; i++)
        {
            string tagName = riderTags[i];

            if (string.IsNullOrEmpty(tagName))
            {
                continue;
            }

            if (target.CompareTag(tagName))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsObjectOnTop(Collision2D collision)
    {
        if (blockCollider == null)
        {
            return false;
        }

        Bounds blockBounds = blockCollider.bounds;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);

            if (contact.point.y >= blockBounds.center.y)
            {
                return true;
            }
        }

        return collision.transform.position.y > transform.position.y;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Vector3 from = Application.isPlaying ? (Vector3)startPosition : transform.position;

        float direction = startMovingUp ? 1f : -1f;
        Vector3 to = from + Vector3.up * moveDistance * direction;

        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireCube(from, Vector3.one * 0.25f);
        Gizmos.DrawWireCube(to, Vector3.one * 0.25f);
    }
}