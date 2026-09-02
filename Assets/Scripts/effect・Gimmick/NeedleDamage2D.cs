using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NeedleDamage2D : MonoBehaviour
{
    [Header("ダメージ量")]
    [SerializeField] private int damage = 1;

    [Header("連続ダメージ間隔")]
    [SerializeField] private float damageInterval = 0.5f;

    [Header("対象Tag")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string mawaruTag = "Mawaru13";

    [Header("接触中もダメージを入れる")]
    [SerializeField] private bool damageOnStay = true;

    private readonly Dictionary<GameObject, float> lastDamageTimes = new Dictionary<GameObject, float>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!damageOnStay)
        {
            return;
        }

        TryDamage(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!damageOnStay)
        {
            return;
        }

        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
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

        if (!CanDamageNow(target))
        {
            return;
        }

        lastDamageTimes[target] = Time.time;

        float attackerX = transform.position.x;

        PlayerController player = target.GetComponent<PlayerController>();

        if (player == null)
        {
            player = target.GetComponentInChildren<PlayerController>();
        }

        if (player != null)
        {
            player.TakeDamage(damage, attackerX);
            return;
        }

        MawaruController mawaru = target.GetComponent<MawaruController>();

        if (mawaru == null)
        {
            mawaru = target.GetComponentInChildren<MawaruController>();
        }

        if (mawaru != null)
        {
            int knockDir = target.transform.position.x < transform.position.x ? -1 : 1;
            mawaru.TakeDamage(damage, knockDir);
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
        if (target.CompareTag(playerTag))
        {
            return true;
        }

        if (target.CompareTag(mawaruTag))
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

    private bool CanDamageNow(GameObject target)
    {
        if (!lastDamageTimes.ContainsKey(target))
        {
            return true;
        }

        return Time.time - lastDamageTimes[target] >= damageInterval;
    }
}