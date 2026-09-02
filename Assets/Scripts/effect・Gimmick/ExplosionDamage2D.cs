using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ExplosionDamage2D : MonoBehaviour
{
    [Header("ダメージ量")]
    [SerializeField] private int damage = 1;

    [Header("爆風の当たり判定半径")]
    [SerializeField] private float damageRadius = 1.5f;

    [Header("爆風ダメージが有効な時間")]
    [SerializeField] private float activeDamageTime = 0.35f;

    [Header("同じ相手に1回だけ当てる")]
    [SerializeField] private bool damageOncePerTarget = true;

    [Header("対象Tag")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string mawaruTag = "Mawaru13";

    [Header("Colliderを自動設定する")]
    [SerializeField] private bool autoSetupCollider = true;

    [Header("デバッグ")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private bool debugLog = false;

    private CircleCollider2D circleCollider;
    private readonly HashSet<GameObject> damagedTargets = new HashSet<GameObject>();
    private bool isDamageActive;

    private void Awake()
    {
        if (autoSetupCollider)
        {
            SetupCollider();
        }
    }

    private void OnEnable()
    {
        damagedTargets.Clear();
        isDamageActive = true;
        StartCoroutine(DamageActiveRoutine());
    }

    private void SetupCollider()
    {
        circleCollider = GetComponent<CircleCollider2D>();

        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        circleCollider.isTrigger = true;
        circleCollider.radius = damageRadius;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.simulated = true;
    }

    private IEnumerator DamageActiveRoutine()
    {
        float timer = 0f;

        while (timer < activeDamageTime)
        {
            CheckOverlapDamage();

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isDamageActive = false;
    }

    private void CheckOverlapDamage()
    {
        if (!isDamageActive)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, damageRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            TryDamage(hits[i]);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (!isDamageActive)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        GameObject target = GetTargetRoot(other);

        if (target == null)
        {
            return;
        }

        if (!IsDamageTarget(target))
        {
            return;
        }

        if (damageOncePerTarget && damagedTargets.Contains(target))
        {
            return;
        }

        damagedTargets.Add(target);

        float attackerX = transform.position.x;

        PlayerController player = target.GetComponent<PlayerController>();

        if (player == null)
        {
            player = target.GetComponentInChildren<PlayerController>();
        }

        if (player == null)
        {
            player = target.GetComponentInParent<PlayerController>();
        }

        if (player != null)
        {
            player.TakeDamage(damage, attackerX);

            if (debugLog)
            {
                Debug.Log($"[{name}] 爆風でPlayerに {damage} ダメージ");
            }

            return;
        }

        MawaruController mawaru = target.GetComponent<MawaruController>();

        if (mawaru == null)
        {
            mawaru = target.GetComponentInChildren<MawaruController>();
        }

        if (mawaru == null)
        {
            mawaru = target.GetComponentInParent<MawaruController>();
        }

        if (mawaru != null)
        {
            int knockDir = target.transform.position.x < transform.position.x ? -1 : 1;
            mawaru.TakeDamage(damage, knockDir);

            if (debugLog)
            {
                Debug.Log($"[{name}] 爆風でmawaru13に {damage} ダメージ");
            }
        }
    }

    private GameObject GetTargetRoot(Collider2D other)
    {
        if (other.attachedRigidbody != null)
        {
            return other.attachedRigidbody.gameObject;
        }

        return other.transform.root.gameObject;
    }

    private bool IsDamageTarget(GameObject target)
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

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}