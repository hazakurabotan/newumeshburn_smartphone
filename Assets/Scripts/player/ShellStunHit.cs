using UnityEngine;

public class ShellStunHit : MonoBehaviour
{
    [SerializeField] float stunSeconds = 1.0f;

    void OnTriggerEnter2D(Collider2D other) => Hit(other);
    void OnCollisionEnter2D(Collision2D col) => Hit(col.collider);

    void Hit(Collider2D col)
    {
        // Enemy本体を拾う
        var enemy = col.GetComponentInParent<Enemy>();
        if (!enemy) return;

        // ★優先：EnemyShellStunnable があるならそっちにスタンを入れる
        var stunnable = enemy.GetComponent<EnemyShellStunnable>();
        if (stunnable != null)
        {
            stunnable.ApplyShellStun(stunSeconds);
            return;
        }

        // ★無い場合だけ Enemy 側のスタン
        enemy.ApplyShellStun(stunSeconds);
    }
}