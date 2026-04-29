using System.Collections.Generic;
using UnityEngine;

public class PunchKnockbackLock : MonoBehaviour
{
    Rigidbody2D rb;
    float timer;
    Vector2 forcedVelocity;
    bool active;

    readonly List<Behaviour> disabled = new List<Behaviour>();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Apply(Vector2 velocity, float seconds, bool disableAI)
    {
        forcedVelocity = velocity;
        timer = Mathf.Max(timer, seconds);
        active = true;

        if (disableAI) DisableEnemyAIScripts();
    }

    void FixedUpdate()
    {
        if (!active) return;

        if (rb != null) rb.velocity = forcedVelocity;

        timer -= Time.fixedDeltaTime;
        if (timer <= 0f)
        {
            active = false;
            RestoreScripts();
        }
    }

    void DisableEnemyAIScripts()
    {
        disabled.Clear();

        // Enemyñ{ëÃÇ…ïtÇ¢ÇƒÇÈAIÇ¡Ç€Ç¢Ç‡ÇÃÇæÇØé~ÇﬂÇÈ
        foreach (var b in GetComponents<Behaviour>())
        {
            if (b == null || b == this) continue;

            string n = b.GetType().Name;

            // Ç±ÇÍÇÕé~ÇﬂÇ»Ç¢
            if (n == "Enemy") continue;
            if (n == "EnemyShellStunnable") continue;
            if (n == "EnemyPunchGrabWindow") continue;

            // EnemyÅ`ånÇÕé~ÇﬂÇÈÅiEnemyStraightMouth / EnemyFloatShooter / EnemyBoomerangThrower Ç»Ç«Åj
            if (n.StartsWith("Enemy"))
            {
                if (b.enabled)
                {
                    b.enabled = false;
                    disabled.Add(b);
                }
            }
        }
    }

    void RestoreScripts()
    {
        foreach (var b in disabled)
            if (b) b.enabled = true;
        disabled.Clear();
    }

    public static void ApplyTo(GameObject enemyRoot, Vector2 velocity, float seconds, bool disableAI = true)
    {
        if (enemyRoot == null) return;

        var k = enemyRoot.GetComponent<PunchKnockbackLock>();
        if (k == null) k = enemyRoot.AddComponent<PunchKnockbackLock>();
        k.Apply(velocity, seconds, disableAI);
    }
}