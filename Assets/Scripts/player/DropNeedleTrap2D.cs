using UnityEngine;

[DisallowMultipleComponent]
public class DropNeedleTrap2D : MonoBehaviour
{
    [Header("反応する対象Tag")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string mawaruTag = "Mawaru13";

    [Header("発動条件")]
    [SerializeField] private float detectRadius = 3.0f;
    [SerializeField] private float dropDelay = 1.0f;

    [Header("落下設定")]
    [SerializeField] private float dropSpeed = 8.0f;
    [SerializeField] private bool useWorldDown = true;

    [Header("落下終了設定")]
    [SerializeField] private bool stopAfterDistance = false;
    [SerializeField] private float maxDropDistance = 10.0f;

    [Header("一度だけ発動")]
    [SerializeField] private bool oneShot = true;

    [Header("デバッグ")]
    [SerializeField] private bool drawDetectRange = true;
    [SerializeField] private bool debugLog = false;

    private Vector3 startPosition;
    private bool hasDetected;
    private bool isDropping;
    private bool hasFinished;
    private float detectedTime;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (hasFinished && oneShot)
        {
            return;
        }

        if (!hasDetected)
        {
            CheckTargetNear();
            return;
        }

        if (!isDropping)
        {
            if (Time.time - detectedTime >= dropDelay)
            {
                StartDrop();
            }

            return;
        }

        DropDown();
    }

    private void CheckTargetNear()
    {
        GameObject player = FindObjectByTag(playerTag);
        GameObject mawaru = FindObjectByTag(mawaruTag);

        if (IsTargetNear(player) || IsTargetNear(mawaru))
        {
            hasDetected = true;
            detectedTime = Time.time;

            if (debugLog)
            {
                Debug.Log($"[{name}] DropNeedleTrap2D：発動待機開始");
            }
        }
    }

    private GameObject FindObjectByTag(string tagName)
    {
        if (string.IsNullOrEmpty(tagName))
        {
            return null;
        }

        try
        {
            return GameObject.FindGameObjectWithTag(tagName);
        }
        catch
        {
            return null;
        }
    }

    private bool IsTargetNear(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        float distance = Vector2.Distance(transform.position, target.transform.position);
        return distance <= detectRadius;
    }

    private void StartDrop()
    {
        isDropping = true;

        if (debugLog)
        {
            Debug.Log($"[{name}] DropNeedleTrap2D：落下開始");
        }
    }

    private void DropDown()
    {
        Vector3 direction = useWorldDown ? Vector3.down : -transform.up;

        transform.position += direction * dropSpeed * Time.deltaTime;

        if (stopAfterDistance)
        {
            float droppedDistance = Vector3.Distance(startPosition, transform.position);

            if (droppedDistance >= maxDropDistance)
            {
                StopDrop();
            }
        }
    }

    private void StopDrop()
    {
        isDropping = false;
        hasFinished = true;

        if (debugLog)
        {
            Debug.Log($"[{name}] DropNeedleTrap2D：落下終了");
        }
    }

    public void ResetTrap()
    {
        transform.position = startPosition;
        hasDetected = false;
        isDropping = false;
        hasFinished = false;
        detectedTime = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDetectRange)
        {
            return;
        }

        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}