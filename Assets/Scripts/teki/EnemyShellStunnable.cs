using UnityEngine;

public class EnemyShellStunnable : MonoBehaviour
{
    [SerializeField] float defaultStunSeconds = 2.0f;

    Enemy enemy;

    public bool IsStunned => enemy != null && enemy.IsShellStunned;

    void Awake()
    {
        enemy = GetComponent<Enemy>();
        if (enemy == null) enemy = GetComponentInParent<Enemy>();
    }

    public void ApplyShellStun(float seconds = -1f)
    {
        if (enemy == null) return;
        if (seconds <= 0f) seconds = defaultStunSeconds;
        enemy.ApplyShellStun(seconds);
    }

    // ★ RopeHead から呼ぶ用（投げる瞬間にスタンを解除して物理を邪魔しない）
    public void ForceEndStun()
    {
        if (enemy == null) return;
        enemy.ClearShellStun();
    }
}