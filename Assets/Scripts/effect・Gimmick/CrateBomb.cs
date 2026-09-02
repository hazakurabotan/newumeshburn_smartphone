using UnityEngine;
using TMPro;

// ---------------------------------------------------
// Player / mawaru13 が乗る・触れると、指定秒数後に
// 箱の上にカウントダウン表示 → 爆発エフェクト生成 → 床が消える
// ---------------------------------------------------
public class CrateBombSimple : MonoBehaviour
{
    [Header("爆発エフェクトPrefab")]
    [SerializeField] private GameObject explosionPrefab;

    [Header("爆発までの秒数")]
    [SerializeField] private float delay = 3f;

    [Header("爆発エフェクトを少し上に出す位置補正")]
    [SerializeField] private Vector3 explosionOffset = Vector3.zero;

    [Header("爆発エフェクトを自動で消す秒数")]
    [SerializeField] private float explosionDestroyDelay = 1.0f;

    [Header("反応するTag")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string mawaruTag = "Mawaru13";

    [Header("カウントダウン表示")]
    [SerializeField] private bool showCountdown = true;

    [Tooltip("空欄でOK。空欄なら自動で箱の上にTextMeshProを作ります。")]
    [SerializeField] private TextMeshPro countdownWorldText;

    [SerializeField] private TMP_FontAsset countdownFontAsset;
    [SerializeField] private Vector3 countdownOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private float countdownFontSize = 3.0f;
    [SerializeField] private Color countdownColor = Color.red;
    [SerializeField] private string countdownSortingLayerName = "Default";
    [SerializeField] private int countdownSortingOrder = 999;
    [SerializeField] private bool showDecimal = false;

    [Header("デバッグ")]
    [SerializeField] private bool debugLog = false;

    private bool isCounting = false;
    private bool hasExploded = false;
    private float timer = 0f;

    private GameObject createdCountdownObject;

    private void Awake()
    {
        SetupCountdownText();

        if (countdownWorldText != null)
        {
            countdownWorldText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isCounting || hasExploded)
        {
            return;
        }

        timer -= Time.deltaTime;

        UpdateCountdownText();
        UpdateCountdownPosition();

        if (timer <= 0f)
        {
            ExplodeAndDestroy();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryStartCountDown(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryStartCountDown(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryStartCountDown(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryStartCountDown(collision.gameObject);
    }

    private void TryStartCountDown(GameObject other)
    {
        if (isCounting || hasExploded)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        GameObject target = GetTargetRoot(other);

        if (!IsTarget(target))
        {
            return;
        }

        isCounting = true;
        timer = delay;

        if (showCountdown && countdownWorldText != null)
        {
            countdownWorldText.gameObject.SetActive(true);
            UpdateCountdownPosition();
            UpdateCountdownText();
        }

        if (debugLog)
        {
            Debug.Log($"[{name}] 爆発カウント開始 : {target.name}");
        }
    }

    private GameObject GetTargetRoot(GameObject other)
    {
        if (other == null)
        {
            return null;
        }

        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            return rb.gameObject;
        }

        rb = other.GetComponentInParent<Rigidbody2D>();

        if (rb != null)
        {
            return rb.gameObject;
        }

        return other.transform.root.gameObject;
    }

    private bool IsTarget(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(playerTag) && target.CompareTag(playerTag))
        {
            return true;
        }

        if (!string.IsNullOrEmpty(mawaruTag) && target.CompareTag(mawaruTag))
        {
            return true;
        }

        if (target.name.Contains("Player"))
        {
            return true;
        }

        if (target.name.Contains("mawaru13") || target.name.Contains("Mawaru"))
        {
            return true;
        }

        return false;
    }

    private void SetupCountdownText()
    {
        if (!showCountdown)
        {
            return;
        }

        if (countdownWorldText == null)
        {
            createdCountdownObject = new GameObject("BombCountdownWorldText");
            createdCountdownObject.transform.position = transform.position + countdownOffset;
            createdCountdownObject.transform.rotation = Quaternion.identity;
            createdCountdownObject.transform.localScale = Vector3.one;

            countdownWorldText = createdCountdownObject.AddComponent<TextMeshPro>();
        }

        ApplyCountdownTextSettings();
    }

    private void ApplyCountdownTextSettings()
    {
        if (countdownWorldText == null)
        {
            return;
        }

        if (countdownFontAsset != null)
        {
            countdownWorldText.font = countdownFontAsset;
        }

        countdownWorldText.text = "";
        countdownWorldText.alignment = TextAlignmentOptions.Center;
        countdownWorldText.fontSize = countdownFontSize;
        countdownWorldText.color = countdownColor;
        countdownWorldText.enableWordWrapping = false;

        RectTransform rectTransform = countdownWorldText.GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(3f, 2f);
        }

        MeshRenderer meshRenderer = countdownWorldText.GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerName = countdownSortingLayerName;
            meshRenderer.sortingOrder = countdownSortingOrder;
        }
    }

    private void UpdateCountdownText()
    {
        if (countdownWorldText == null || !showCountdown)
        {
            return;
        }

        float displayTime = Mathf.Max(0f, timer);

        if (showDecimal)
        {
            countdownWorldText.text = displayTime.ToString("0.0");
        }
        else
        {
            countdownWorldText.text = Mathf.CeilToInt(displayTime).ToString();
        }
    }

    private void UpdateCountdownPosition()
    {
        if (countdownWorldText == null)
        {
            return;
        }

        countdownWorldText.transform.position = transform.position + countdownOffset;
        countdownWorldText.transform.rotation = Quaternion.identity;
        countdownWorldText.transform.localScale = Vector3.one;
    }

    private void ExplodeAndDestroy()
    {
        if (hasExploded)
        {
            return;
        }

        hasExploded = true;

        if (countdownWorldText != null)
        {
            countdownWorldText.gameObject.SetActive(false);
        }

        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(
                explosionPrefab,
                transform.position + explosionOffset,
                Quaternion.identity
            );

            explosion.SetActive(true);

            if (explosionDestroyDelay > 0f)
            {
                Destroy(explosion, explosionDestroyDelay);
            }
        }
        else
        {
            Debug.LogWarning($"[{name}] explosionPrefab が設定されていません。");
        }

        if (createdCountdownObject != null)
        {
            Destroy(createdCountdownObject);
        }

        Destroy(gameObject);
    }
}