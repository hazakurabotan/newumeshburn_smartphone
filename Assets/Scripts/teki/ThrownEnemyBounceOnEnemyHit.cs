using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ThrownEnemyBounceOnEnemyHit : MonoBehaviour
{
    [Header("Thrown判定（レイヤー名で判定）")]
    public string thrownLayerName = "EnemyThrown";

    [Header("当たったら跳ね返る相手（通常はEnemyレイヤー）")]
    public LayerMask enemyLayers = 0;

    [Header("ダメージ（投げ当て）")]
    public int damageToOther = 2;
    public int damageToSelf = 0;
    public float hitCooldown = 0.08f;

    [Header("跳ね返り（壁/地面/敵 すべて共通）")]
    public float reboundMul = 1.2f;
    public float minReboundSpeed = 6f;
    public float minUpSpeedOnGround = 4f;
    public float pushOutDistance = 0.06f;

    [Header("貼り付き防止：一時的に衝突無効（超重要）")]
    public float ignoreCollisionSeconds = 0.12f;

    [Header("AI上書き対策（任意）")]
    public bool disableOtherAITemporarily = true;
    public float disableOtherAISeconds = 0.10f;

    Rigidbody2D rb;
    Collider2D[] selfCols;
    int thrownLayer = -1;

    Vector2 lastVel;
    bool hasLastVel;

    float lastHitTime = -999f;

    // 速度を数フレーム強制して「当たった瞬間に0にされる」を潰す
    Vector2 overrideVel;
    float overrideTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        selfCols = GetComponentsInChildren<Collider2D>(true);
        thrownLayer = LayerMask.NameToLayer(thrownLayerName);
    }

    bool IsThrown()
    {
        if (thrownLayer >= 0) return gameObject.layer == thrownLayer;
        return gameObject.layer == LayerMask.NameToLayer(thrownLayerName);
    }

    void FixedUpdate()
    {
        if (IsThrown())
        {
            lastVel = rb.velocity;
            hasLastVel = true;

            if (overrideTimer > 0f)
            {
                rb.velocity = overrideVel;
                overrideTimer -= Time.fixedDeltaTime;
            }
        }
        else
        {
            hasLastVel = false;
            overrideTimer = 0f;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsThrown()) return;
        if (col.contactCount == 0) return;
        if (Time.time - lastHitTime < hitCooldown) return;
        lastHitTime = Time.time;

        // ---- 1) 敵に当たったらダメージ（※投げられ中だけ） ----
        if ((enemyLayers.value & (1 << col.gameObject.layer)) != 0)
        {
            var otherEnemy = col.collider.GetComponentInParent<Enemy>();
            if (otherEnemy != null && otherEnemy.gameObject != this.gameObject)
            {
                otherEnemy.TakeDamage(damageToOther, "ThrownEnemy");

                if (damageToSelf > 0)
                {
                    var selfEnemy = GetComponent<Enemy>();
                    if (selfEnemy != null) selfEnemy.TakeDamage(damageToSelf, "ThrownSelf");
                }
            }
        }

        // ---- 2) 地面/壁/回転部屋/敵など「何に当たっても」バウンド ----
        DoBounce(col);

        // ---- 3) 貼り付き防止：短時間だけ衝突を無効化 ----
        if (ignoreCollisionSeconds > 0f)
            StartCoroutine(TempIgnoreCollision(col.collider, ignoreCollisionSeconds));

        // ---- 4) AI上書きで止められるのを防ぐ（任意） ----
        if (disableOtherAITemporarily && disableOtherAISeconds > 0f)
        {
            StartCoroutine(TempDisableKnownAI(this.gameObject, disableOtherAISeconds));
            StartCoroutine(TempDisableKnownAI(col.collider.transform.root.gameObject, disableOtherAISeconds));
        }
    }

    void DoBounce(Collision2D col)
    {
        var contact = col.GetContact(0);
        Vector2 n = contact.normal;
        if (n.sqrMagnitude < 0.0001f) n = Vector2.up;

        Vector2 inVel = hasLastVel ? lastVel : rb.velocity;
        if (inVel.sqrMagnitude < 0.0001f)
            inVel = -n * Mathf.Max(1f, minReboundSpeed);

        Vector2 outVel = Vector2.Reflect(inVel, n) * reboundMul;

        // 速度が小さすぎるなら底上げ（＝必ずバウンド）
        float sp = outVel.magnitude;
        if (sp < minReboundSpeed)
        {
            if (sp < 0.0001f) outVel = -n * minReboundSpeed;
            else outVel = outVel.normalized * minReboundSpeed;
        }

        // 地面っぽい法線なら上方向を保証
        if (n.y > 0.5f)
            outVel.y = Mathf.Max(outVel.y, minUpSpeedOnGround);

        // 押し出し（密着して停止するのを防ぐ）
        if (pushOutDistance > 0f)
            rb.position += n * pushOutDistance;

        rb.velocity = outVel;

        // 数フレーム強制（他スクリプトに0にされるのを潰す）
        overrideVel = outVel;
        overrideTimer = Mathf.Max(0.06f, hitCooldown);
    }

    IEnumerator TempIgnoreCollision(Collider2D otherCol, float sec)
    {
        if (otherCol == null) yield break;

        var otherCols = otherCol.GetComponentsInChildren<Collider2D>(true);

        foreach (var a in selfCols)
            foreach (var b in otherCols)
                if (a && b) Physics2D.IgnoreCollision(a, b, true);

        yield return new WaitForSeconds(sec);

        foreach (var a in selfCols)
            foreach (var b in otherCols)
                if (a && b) Physics2D.IgnoreCollision(a, b, false);
    }

    IEnumerator TempDisableKnownAI(GameObject root, float sec)
    {
        if (root == null) yield break;

        // 影響を最小に：よくある移動AIだけ止める（Enemy本体などは触らない）
        var targets = new List<MonoBehaviour>();
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!mb || !mb.enabled) continue;
            var t = mb.GetType().Name;
            if (t == "EnemyFloatShooter" || t == "EnemyStraightMouth" || t == "EnemyBoomerangThrower" || t == "EnemyController")
                targets.Add(mb);
        }

        if (targets.Count == 0) yield break;

        foreach (var mb in targets) if (mb) mb.enabled = false;
        yield return new WaitForSeconds(sec);
        foreach (var mb in targets) if (mb) mb.enabled = true;
    }
}