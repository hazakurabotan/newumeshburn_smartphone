using System.Collections.Generic;
using UnityEngine;

public class DamageFloor2D : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;

    [Tooltip("G‚ê‘±‚¯‚Ä‚¢‚éŠÔ‚ÌŒp‘±ƒ_ƒ[ƒWŠÔŠui•bj")]
    public float tickInterval = 0.5f;

    // G‚ê‚Ä‚¢‚é‘Šè‚ÌŸTick
    readonly Dictionary<int, float> nextTickTime = new();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // ‡@ G‚ê‚½uŠÔ‚É1‰ñƒ_ƒ[ƒW
        ApplyDamage(other);

        // ‡A Œp‘±ƒ_ƒ[ƒW‚Ì‰‰ñTick‚ğu0.5•bŒãv‚É—\–ñ
        nextTickTime[other.GetInstanceID()] = Time.time + tickInterval;
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        int id = other.GetInstanceID();
        float now = Time.time;

        if (!nextTickTime.TryGetValue(id, out var t)) return;
        if (now < t) return;

        // ‡B G‚ê‘±‚¯‚Ä‚¢‚éŠÔ‚Í0.5•b‚²‚Æ‚É1ƒ_ƒ[ƒW
        ApplyDamage(other);
        nextTickTime[id] = now + tickInterval;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        nextTickTime.Remove(other.GetInstanceID());
    }

    void ApplyDamage(Collider2D other)
    {
        // Mawaru—Dæi•ûŒü‚Â‚«j
        var ma = other.GetComponent<MawaruController>();
        if (ma != null)
        {
            int dir = (other.transform.position.x - transform.position.x) >= 0 ? 1 : -1;
            ma.TakeDamage(damage, dir);
            return;
        }

        // PlayerController
        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.TakeDamage(damage);
            return;
        }

        // •ÛŒ¯
        other.SendMessage("Damage", damage, SendMessageOptions.DontRequireReceiver);
    }
}