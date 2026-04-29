using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Collider2D))]
public class TrainingDummy : MonoBehaviour
{
    [Header("HP")]
    public int maxHP = 10;
    public bool infiniteHP = false; // ★ここがONだと絶対切り替わらない
    public float hitInterval = 0.05f;

    [Header("Debug (確認用)")]
    [SerializeField] int currentHP;        // Inspectorで見える
    public bool logHit = true;

    [Header("Hit Effect (毎回出す)")]
    public GameObject hitEffectPrefab;     // bakuha2 (2)
    public float hitEffectLifeTime = 0.6f;
    public Vector3 hitEffectOffset = new Vector3(0f, 0.2f, 0f);
    public int effectOrderOffset = 10;

    [Header("Death Replace (HP0で置換)")]
    public GameObject deadReplacementPrefab; // kakasi 2
    public bool keepParent = true;
    public bool destroySelfOnDeath = true;

    float _cool;
    SpriteRenderer _baseSR;

    // 追加：パンチは1回につき1ヒットにしたい
    public float punchHitInterval = 0.35f; // ★パンチ判定が出てる時間より長めに
    float _nextPunchOkTime;
    float _nextAnyOkTime;

    void Awake()
    {
        if (maxHP <= 0) maxHP = 1;
        currentHP = maxHP;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        _baseSR = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        if (_cool > 0f) _cool -= Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        int dmg = 0;
        bool isPunch = false;

        var punch = other.GetComponent<MawaruPunchHitbox>();
        if (punch != null)
        {
            dmg = Mathf.Max(1, punch.damage);
            isPunch = true;
        }
        else
        {
            var dd = other.GetComponent<DamageDealer>();
            if (dd != null) dmg = Mathf.Max(1, dd.damage);
        }

        if (dmg == 0) return;

        // ★クールタイムを分ける（Enter/Stay両方来ても1回だけ）
        if (isPunch)
        {
            if (Time.time < _nextPunchOkTime) return;
            _nextPunchOkTime = Time.time + punchHitInterval;
        }
        else
        {
            if (Time.time < _nextAnyOkTime) return;
            _nextAnyOkTime = Time.time + hitInterval;
        }

        // ここから下は今の処理（bakuha出す、HP減らす、0でkakasi2に置換）
        SpawnHitEffect();
        if (!infiniteHP)
        {
            currentHP -= dmg;
            if (currentHP <= 0) ReplaceToDeadPrefab();
        }
    }

    void SpawnHitEffect()
    {
        if (!hitEffectPrefab) return;

        var fx = Instantiate(hitEffectPrefab, transform.position + hitEffectOffset, Quaternion.identity);

        int baseOrder = _baseSR ? _baseSR.sortingOrder : 0;
        string baseLayer = _baseSR ? _baseSR.sortingLayerName : "Default";
        ApplySortingToAllRenderers(fx, baseLayer, baseOrder + effectOrderOffset);

        if (hitEffectLifeTime > 0f) Destroy(fx, hitEffectLifeTime);
    }

    void ReplaceToDeadPrefab()
    {
        if (logHit) Debug.Log("[TrainingDummy] REPLACE to kakasi2");

        if (!deadReplacementPrefab)
        {
            // 置換先が空なら、切替できないのでHPだけ戻す
            currentHP = maxHP;
            if (logHit) Debug.Log("[TrainingDummy] deadReplacementPrefab is NULL -> HP reset");
            return;
        }

        Transform parent = keepParent ? transform.parent : null;

        var newObj = Instantiate(deadReplacementPrefab, transform.position, transform.rotation, parent);
        newObj.transform.localScale = transform.localScale;
        newObj.layer = gameObject.layer;

        if (_baseSR != null)
            ApplySortingToAllRenderers(newObj, _baseSR.sortingLayerName, _baseSR.sortingOrder);

        if (destroySelfOnDeath) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    static void ApplySortingToAllRenderers(GameObject root, string sortingLayer, int order)
    {
        var sg = root.GetComponentInChildren<SortingGroup>(true);
        if (sg != null)
        {
            sg.sortingLayerName = sortingLayer;
            sg.sortingOrder = order;
        }

        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingLayerName = sortingLayer;
            r.sortingOrder = order;
        }
    }
}