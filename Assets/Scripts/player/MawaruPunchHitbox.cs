using System.Collections;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MawaruPunchHitbox : MonoBehaviour
{
    [Header("与ダメージ")]
    public int damage = 2;

    [Header("パンチ後つかめる猶予（秒）")]
    public float grabWindowSeconds = 2f;

    [Header("ノックバック")]
    public bool enableKnockback = true;
    public Vector2 knockbackVel = new Vector2(6f, 3f); // X,Y

    [Header("敵AI上書き対策（Enemy系）")]
    public float knockbackLockSeconds = 0.12f;          // AI上書きを防ぐ時間
    public bool disableEnemyAIScriptsDuringKnockback = true;

    [Header("ボスAI上書き対策（BossController2D）")]
    public bool disableBossAIForKnockback = true;
    public float bossKnockbackDisableSeconds = 0.12f;

    [Header("Safety (パンチ成功時の体当たりダメージ防止)")]
    public bool ignoreOwnerEnemyCollisionOnHit = true;
    public float ignoreOwnerEnemyCollisionSeconds = 0.25f;

    [Header("Safety (パンチ成功時にMawaruへ短い無敵を付与)")]
    public bool grantOwnerIFramesOnHit = true;
    public float ownerIFramesSecondsOnHit = 0.25f;

    [Header("敵弾をはじく（false=消す）")]
    public bool reflectEnemyBullets = true;

    [Header("弾として扱うLayer（EnemyBulletを入れてね）")]
    public LayerMask bulletLayers;

    MawaruController owner;

    void Awake()
    {
        owner = GetComponentInParent<MawaruController>();

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // Square を弾扱いにしない
        gameObject.tag = "Untagged";

        // 物理的には PlayerBullet レイヤーでOK（弾と当てたいなら）
        int lb = LayerMask.NameToLayer("PlayerBullet");
        if (lb >= 0) gameObject.layer = lb;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // =========================
        // 0) ボス（BossController2D）
        // =========================
        var boss = other.GetComponentInParent<BossController2D>();
        if (boss != null)
        {
            // ★パンチ成功時にMawaruへ短い無敵（接触ダメージを確実に潰す）
            if (grantOwnerIFramesOnHit && owner != null)
                owner.GrantIFrames(ownerIFramesSecondsOnHit);

            // ★ボスにダメージ（ApplyDamage/TakeDamage どちらでも拾えるよう反射的に呼ぶ）
            TryApplyDamageToBoss(boss, damage);

            // ★物理的にも「Mawaru ⇄ ボス」の衝突を短時間OFF
            if (ignoreOwnerEnemyCollisionOnHit && owner != null && ignoreOwnerEnemyCollisionSeconds > 0f)
                StartCoroutine(TempIgnoreOwnerEnemyCollision(boss.gameObject, ignoreOwnerEnemyCollisionSeconds));

            // ★ボスノックバック
            if (enableKnockback)
                StartCoroutine(BossKnockbackRoutine(boss, other));

            return;
        }

        // =========================
        // 1) 通常敵（Enemy）
        // =========================
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            // ★パンチ成功時にMawaruへ短い無敵
            if (grantOwnerIFramesOnHit && owner != null)
                owner.GrantIFrames(ownerIFramesSecondsOnHit);

            // ★敵へダメージ
            enemy.TakeDamage(damage, "MawaruPunch");

            // ★パンチ命中後 2秒つかめる（掴み判定側がこのコンポを見る前提）
            var win = enemy.GetComponent<EnemyPunchGrabWindow>();
            if (win == null) win = enemy.gameObject.AddComponent<EnemyPunchGrabWindow>();
            win.Open(grabWindowSeconds);

            // ★物理的にも「Mawaru ⇄ この敵」の衝突を短時間OFF
            if (ignoreOwnerEnemyCollisionOnHit && owner != null && ignoreOwnerEnemyCollisionSeconds > 0f)
                StartCoroutine(TempIgnoreOwnerEnemyCollision(enemy.gameObject, ignoreOwnerEnemyCollisionSeconds));

            // ★ノックバック（AI上書き対策込み）
            if (enableKnockback)
            {
                float dir = Mathf.Sign(other.bounds.center.x - transform.position.x);
                if (dir == 0) dir = 1f;

                Vector2 vel = new Vector2(knockbackVel.x * dir, knockbackVel.y);
                PunchKnockbackLock.ApplyTo(enemy.gameObject, vel, knockbackLockSeconds, disableEnemyAIScriptsDuringKnockback);
            }

            return;
        }

        // =========================
        // 2) 敵弾（反射）
        // =========================
        if (!reflectEnemyBullets) return;

        bool isBulletLayer = (bulletLayers.value & (1 << other.gameObject.layer)) != 0;
        bool isBulletTag = other.CompareTag("EnemyBullet");
        bool hasEnemyBulletComp = other.GetComponentInParent<EnemyBullet>() != null;
        bool hasBossBulletComp = other.GetComponentInParent<BossBullet>() != null;
        bool hasDamageDealer = other.GetComponentInParent<DamageDealer>() != null;

        if (!isBulletLayer && !isBulletTag && !hasEnemyBulletComp && !hasBossBulletComp && !hasDamageDealer)
            return;

        var bulletRb = other.GetComponentInParent<Rigidbody2D>();
        if (bulletRb == null) return;

        // 速度反転
        bulletRb.velocity = new Vector2(-bulletRb.velocity.x, bulletRb.velocity.y);

        // DamageDealer の owner を mawaru にして「本人には当たらない」扱いにする
        var dd = other.GetComponentInParent<DamageDealer>();
        if (dd != null && owner != null) dd.owner = owner.gameObject;

        // タグ/レイヤーをプレイヤー弾へ（任意）
        other.tag = "PlayerBullet";
        int pl = LayerMask.NameToLayer("PlayerBullet");
        if (pl >= 0) other.gameObject.layer = pl;

        // mawaru と Player には当たらないよう IgnoreCollision（保険）
        if (owner != null)
        {
            var bulletCols = other.GetComponentsInChildren<Collider2D>(true);
            var mawaruCols = owner.GetComponentsInChildren<Collider2D>(true);

            foreach (var b in bulletCols)
                foreach (var m in mawaruCols)
                    if (b && m) Physics2D.IgnoreCollision(b, m, true);

            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                var playerCols = player.GetComponentsInChildren<Collider2D>(true);
                foreach (var b in bulletCols)
                    foreach (var p in playerCols)
                        if (b && p) Physics2D.IgnoreCollision(b, p, true);
            }
        }
    }

    // ===== ボスにダメージ（ApplyDamage / TakeDamage のどちらでも対応）
    static void TryApplyDamageToBoss(BossController2D boss, int dmg)
    {
        if (boss == null) return;

        // 1) ApplyDamage(int)
        var t = boss.GetType();
        var m = t.GetMethod("ApplyDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int))
        {
            m.Invoke(boss, new object[] { dmg });
            return;
        }

        // 2) TakeDamage(int)
        m = t.GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (m != null && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int))
        {
            m.Invoke(boss, new object[] { dmg });
            return;
        }

        // 3) 何も無い場合の保険
        boss.SendMessage("ApplyDamage", dmg, SendMessageOptions.DontRequireReceiver);
        boss.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

    // ===== ボスノックバック（AI上書き対策：短時間だけBossController2Dを止める）
    IEnumerator BossKnockbackRoutine(BossController2D boss, Collider2D hitCol)
    {
        var rb = boss.GetComponent<Rigidbody2D>();
        if (rb == null) yield break;

        float dir = Mathf.Sign(hitCol.bounds.center.x - transform.position.x);
        if (dir == 0) dir = 1f;

        Vector2 vel = new Vector2(knockbackVel.x * dir, Mathf.Max(rb.velocity.y, knockbackVel.y));

        bool prevEnabled = boss.enabled;
        if (disableBossAIForKnockback) boss.enabled = false;

        float t = bossKnockbackDisableSeconds;
        while (t > 0f)
        {
            rb.velocity = vel;
            t -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        if (disableBossAIForKnockback) boss.enabled = prevEnabled;
    }

    // ===== Mawaru⇄敵（ボス含む）衝突を短時間無効化
    IEnumerator TempIgnoreOwnerEnemyCollision(GameObject enemyRoot, float seconds)
    {
        if (owner == null || enemyRoot == null) yield break;

        var enemyCols = enemyRoot.GetComponentsInChildren<Collider2D>(true);
        var ownerCols = owner.GetComponentsInChildren<Collider2D>(true);

        foreach (var e in enemyCols)
            foreach (var o in ownerCols)
                if (e && o) Physics2D.IgnoreCollision(e, o, true);

        yield return new WaitForSeconds(seconds);

        foreach (var e in enemyCols)
            foreach (var o in ownerCols)
                if (e && o) Physics2D.IgnoreCollision(e, o, false);
    }
}