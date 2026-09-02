using UnityEngine;

[DisallowMultipleComponent]
public class NeedlePopUpByDistance2D : MonoBehaviour
{
    [Header("反応する対象Tag")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string mawaruTag = "Mawaru13";

    [Header("Tagが合わない時に名前でも探す")]
    [SerializeField] private bool searchByNameIfTagNotFound = true;
    [SerializeField] private string playerNameKeyword = "Player";
    [SerializeField] private string mawaruNameKeyword = "mawaru13";

    [Header("出現判定")]
    [SerializeField] private float detectRadius = 4.0f;
    [SerializeField] private Vector2 detectOffset = new Vector2(0f, 1.5f);

    [Header("針の位置")]
    [SerializeField] private bool useCurrentPositionAsShownPosition = true;
    [SerializeField] private Vector3 shownLocalPosition = Vector3.zero;

    [SerializeField] private bool autoCreateHiddenPosition = true;
    [SerializeField] private Vector3 hiddenOffset = new Vector3(0f, -0.9f, 0f);
    [SerializeField] private Vector3 hiddenLocalPosition = Vector3.zero;

    [Header("動き")]
    [SerializeField] private float moveSpeed = 6.0f;
    [SerializeField] private float appearDelay = 0.0f;
    [SerializeField] private float hideDelay = 0.0f;

    [Header("開始時の状態")]
    [SerializeField] private bool startHidden = true;

    [Header("針が出ている時だけ有効にしたいCollider")]
    [SerializeField] private bool autoFindColliders = true;
    [SerializeField] private Collider2D[] collidersToToggle;

    [Header("Colliderを有効にする距離しきい値")]
    [SerializeField] private float shownEpsilon = 0.03f;

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = false;
    [SerializeField] private bool drawGizmos = true;

    private Transform playerTarget;
    private Transform mawaruTarget;

    private Vector3 resolvedShownLocalPosition;
    private Vector3 resolvedHiddenLocalPosition;
    private Vector3 resolvedShownWorldPosition;

    private bool isNear;
    private bool desiredShown;
    private float nearStateChangedTime;

    private void Awake()
    {
        ResolvePositions();
        ResolveColliders();
        FindTargets();
    }

    private void Start()
    {
        if (startHidden)
        {
            desiredShown = false;
            transform.localPosition = resolvedHiddenLocalPosition;
        }
        else
        {
            desiredShown = true;
            transform.localPosition = resolvedShownLocalPosition;
        }

        isNear = false;
        nearStateChangedTime = Time.time;

        ApplyColliderState(false);
    }

    private void Update()
    {
        RefreshTargetsIfNeeded();

        bool currentNear = IsAnyTargetNear();

        if (currentNear != isNear)
        {
            isNear = currentNear;
            nearStateChangedTime = Time.time;

            if (debugLog)
            {
                Debug.Log($"[{name}] 近くにいる判定 = {isNear}");
            }
        }

        if (isNear)
        {
            if (Time.time - nearStateChangedTime >= appearDelay)
            {
                desiredShown = true;
            }
        }
        else
        {
            if (Time.time - nearStateChangedTime >= hideDelay)
            {
                desiredShown = false;
            }
        }

        Vector3 targetLocalPosition = desiredShown ? resolvedShownLocalPosition : resolvedHiddenLocalPosition;

        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition,
            targetLocalPosition,
            moveSpeed * Time.deltaTime
        );

        bool isFullyShown =
            desiredShown &&
            Vector3.Distance(transform.localPosition, resolvedShownLocalPosition) <= shownEpsilon;

        ApplyColliderState(isFullyShown);
    }

    private void ResolvePositions()
    {
        if (useCurrentPositionAsShownPosition)
        {
            resolvedShownLocalPosition = transform.localPosition;
        }
        else
        {
            resolvedShownLocalPosition = shownLocalPosition;
        }

        if (autoCreateHiddenPosition)
        {
            resolvedHiddenLocalPosition = resolvedShownLocalPosition + hiddenOffset;
        }
        else
        {
            resolvedHiddenLocalPosition = hiddenLocalPosition;
        }

        resolvedShownWorldPosition = transform.parent != null
            ? transform.parent.TransformPoint(resolvedShownLocalPosition)
            : resolvedShownLocalPosition;
    }

    private void ResolveColliders()
    {
        if (!autoFindColliders)
        {
            return;
        }

        collidersToToggle = GetComponents<Collider2D>();
    }

    private void FindTargets()
    {
        playerTarget = FindTarget(playerTag, playerNameKeyword);
        mawaruTarget = FindTarget(mawaruTag, mawaruNameKeyword);
    }

    private void RefreshTargetsIfNeeded()
    {
        if (playerTarget == null)
        {
            playerTarget = FindTarget(playerTag, playerNameKeyword);
        }

        if (mawaruTarget == null)
        {
            mawaruTarget = FindTarget(mawaruTag, mawaruNameKeyword);
        }
    }

    private Transform FindTarget(string tagName, string nameKeyword)
    {
        if (!string.IsNullOrEmpty(tagName))
        {
            try
            {
                GameObject taggedObject = GameObject.FindGameObjectWithTag(tagName);

                if (taggedObject != null)
                {
                    return taggedObject.transform;
                }
            }
            catch
            {
                // Tagが存在しない場合は名前検索へ進む
            }
        }

        if (!searchByNameIfTagNotFound || string.IsNullOrEmpty(nameKeyword))
        {
            return null;
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject obj = allObjects[i];

            if (obj == null || !obj.activeInHierarchy)
            {
                continue;
            }

            if (obj.name.Contains(nameKeyword))
            {
                return obj.transform;
            }
        }

        return null;
    }

    private bool IsAnyTargetNear()
    {
        Vector2 detectCenter = GetDetectCenter();

        if (IsTargetNear(playerTarget, detectCenter))
        {
            return true;
        }

        if (IsTargetNear(mawaruTarget, detectCenter))
        {
            return true;
        }

        return false;
    }

    private Vector2 GetDetectCenter()
    {
        // 針が隠れても検知範囲が下にズレないように、
        // 「針が出ている時のワールド位置」を基準にする
        return (Vector2)resolvedShownWorldPosition + detectOffset;
    }

    private bool IsTargetNear(Transform target, Vector2 detectCenter)
    {
        if (target == null)
        {
            return false;
        }

        float distance = Vector2.Distance(detectCenter, target.position);
        return distance <= detectRadius;
    }

    private void ApplyColliderState(bool enabledState)
    {
        if (collidersToToggle == null)
        {
            return;
        }

        for (int i = 0; i < collidersToToggle.Length; i++)
        {
            if (collidersToToggle[i] != null)
            {
                collidersToToggle[i].enabled = enabledState;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Vector3 basePosition;

        if (Application.isPlaying)
        {
            basePosition = resolvedShownWorldPosition;
        }
        else
        {
            Vector3 previewShownLocalPosition;

            if (useCurrentPositionAsShownPosition)
            {
                previewShownLocalPosition = transform.localPosition;
            }
            else
            {
                previewShownLocalPosition = shownLocalPosition;
            }

            basePosition = transform.parent != null
                ? transform.parent.TransformPoint(previewShownLocalPosition)
                : previewShownLocalPosition;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(basePosition + (Vector3)detectOffset, detectRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(basePosition, Vector3.one * 0.2f);

        Vector3 hiddenPreview = basePosition + hiddenOffset;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(hiddenPreview, Vector3.one * 0.2f);
    }
}