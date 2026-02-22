using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class RopeHead : MonoBehaviour
{
    private readonly List<(Collider2D player, Collider2D enemy)> _ignoredPairs = new();

    [Header("Safety after throw")]
    [SerializeField] float postThrowNoHitTime = 0.25f;
    [SerializeField] float throwSeparation = 0.5f;

    [Header("Mawaru Rule")]
    [Tooltip("ONなら Mawaru13 はスタン中の敵だけ掴める/投げれる")]
    [SerializeField] bool mawaruOnlyGrabWhenStunned = true;

    [Header("Hook Tags")]
    public string[] hookTags = new[] { "HookBlock", "GearHook" };

    public float ropeLength = 2f;
    public float returnSpeed = 20f;
    public float returnDelay = 0.2f;

    public float pullSpeed = 18f;
    public float throwPower = 15f; // 速度として扱う

    Rigidbody2D rb;
    Rigidbody2D playerRb;

    PlayerController playerController;
    MawaruController mawaruController;

    Enemy grabbedEnemy;
    EnemyShellStunnable grabbedStun;
    Behaviour[] grabbedAIs;

    float timer = 0f;
    bool returning = false;
    bool stuckOnHook = false;

    Rigidbody2D grabbedRb = null;

    public bool IsGrabbing => grabbedRb != null;
    public bool IsHooked => stuckOnHook;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        timer = 0f;
    }

    void Update()
    {
        // 掴み中：保持位置へ引っ張る
        if (grabbedRb != null)
        {
            var owner = OwnerTransform();
            float facing = Mathf.Sign(owner.localScale.x);

            Vector2 holdPos = playerRb.position + new Vector2(facing * 0.9f, 0.6f);
            Vector2 toHold = holdPos - grabbedRb.position;

            if (toHold.sqrMagnitude > 0.0001f)
                grabbedRb.velocity = toHold.normalized * pullSpeed;

            transform.position = grabbedRb.position;
            return;
        }

        if (stuckOnHook) return;

        timer += Time.deltaTime;
        if (!returning && timer >= returnDelay) returning = true;

        if (returning)
        {
            if (playerRb == null) return;

            Vector2 dirToPlayer = (playerRb.position - rb.position).normalized;
            rb.velocity = dirToPlayer * returnSpeed;

            if (Vector2.Distance(rb.position, playerRb.position) < 0.5f)
            {
                OwnerOnRopeReturned();
                Destroy(gameObject);
            }
        }
    }

    // Player 用
    public void Init(Rigidbody2D player, PlayerController controller)
    {
        playerRb = player;
        playerController = controller;
        mawaruController = null;
    }

    // Mawaru 用
    public void Init(Rigidbody2D player, MawaruController controller)
    {
        playerRb = player;
        mawaruController = controller;
        playerController = null;
    }

    bool IsHookTag(string tag)
    {
        if (hookTags == null) return false;
        for (int i = 0; i < hookTags.Length; i++)
            if (tag == hookTags[i]) return true;
        return false;
    }

    bool IsEnemyStunnedForMawaru(Enemy enemy)
    {
        var st = enemy.GetComponent<EnemyShellStunnable>();
        if (st != null) return st.IsStunned;
        return enemy.IsShellStunned;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Hookに刺さる
        if (IsHookTag(other.tag))
        {
            transform.position = other.transform.position;

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;

            OwnerSetHanging(true);
            returning = false;
            stuckOnHook = true;

            // 既存Jointを掃除（バネ/距離どっちも）
            var oldS = playerRb.GetComponent<SpringJoint2D>();
            if (oldS) Destroy(oldS);

            var oldD = playerRb.GetComponent<DistanceJoint2D>();
            if (oldD) Destroy(oldD);

            // ロープ長を固定して“振り子”にする
            var joint = playerRb.gameObject.AddComponent<DistanceJoint2D>();
            joint.connectedBody = this.rb;
            joint.autoConfigureDistance = false;
            joint.distance = ropeLength;

            // 弧を描くために距離固定（maxDistanceOnlyだと押し戻しが弱くなる）
            joint.maxDistanceOnly = false;

            joint.enableCollision = false;

            return;
        }

        // Enemy を掴む
        if (other.CompareTag("Enemy"))
        {
            var enemy = other.GetComponentInParent<Enemy>();
            if (enemy == null) return;

            // Mawaru13はスタン中のみ掴める
            if (mawaruController != null && mawaruOnlyGrabWhenStunned)
            {
                if (!IsEnemyStunnedForMawaru(enemy)) return;
            }

            grabbedRb = other.attachedRigidbody;
            if (grabbedRb == null) return;

            returning = false;
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;

            grabbedEnemy = enemy;
            grabbedStun = enemy.GetComponent<EnemyShellStunnable>();

            grabbedEnemy.isGrabbed = true;
            grabbedEnemy.isFlying = false;

            CacheAndDisableEnemyAIs(grabbedEnemy);
            IgnorePlayerEnemyCollision();
        }
    }

    void CacheAndDisableEnemyAIs(Enemy e)
    {
        var list = new List<Behaviour>();
        var a = e.GetComponent<EnemyStraightMouth>(); if (a) list.Add(a);
        var b = e.GetComponent<EnemyFloatShooter>(); if (b) list.Add(b);
        var c = e.GetComponent<EnemyBoomerangThrower>(); if (c) list.Add(c);

        grabbedAIs = list.ToArray();
        for (int i = 0; i < grabbedAIs.Length; i++)
            if (grabbedAIs[i]) grabbedAIs[i].enabled = false;
    }

    void IgnorePlayerEnemyCollision()
    {
        if (grabbedRb == null) return;

        var playerCols = OwnerColliders();
        var enemyCols = grabbedRb.GetComponentsInChildren<Collider2D>(true);
        if (playerCols == null || playerCols.Length == 0) return;

        foreach (var p in playerCols)
            foreach (var e in enemyCols)
                if (p && e)
                {
                    Physics2D.IgnoreCollision(p, e, true);
                    _ignoredPairs.Add((p, e));
                }
    }

    void RestoreIgnoredCollisions()
    {
        foreach (var pair in _ignoredPairs)
            if (pair.player && pair.enemy)
                Physics2D.IgnoreCollision(pair.player, pair.enemy, false);

        _ignoredPairs.Clear();
    }

    public bool TryThrowGrabbed(Vector2 dir)
    {
        if (grabbedRb == null) return false;

        // Mawaru13はスタン中のみ投げれる
        if (mawaruController != null && mawaruOnlyGrabWhenStunned)
        {
            if (grabbedEnemy == null) return false;
            if (!IsEnemyStunnedForMawaru(grabbedEnemy)) return false;
        }

        Vector2 n = dir.sqrMagnitude < 0.0001f ? Vector2.right : dir.normalized;

        // 少し離す
        Vector2 sepPos = playerRb.position + n * throwSeparation;
        grabbedRb.position = sepPos;
        grabbedRb.velocity = Vector2.zero;
        grabbedRb.angularVelocity = 0f;

        // ★投げる瞬間に “スタン解除” して物理を殺さない
        if (grabbedStun != null) grabbedStun.ForceEndStun();
        else if (grabbedEnemy != null) grabbedEnemy.ClearShellStun();

        // ★投げる準備：Dynamicに戻す（ここ大事）
        grabbedRb.bodyType = RigidbodyType2D.Dynamic;
        grabbedRb.simulated = true;

        if (grabbedEnemy != null)
        {
            grabbedEnemy.isGrabbed = false;
            grabbedEnemy.BeginThrow();
        }

        // 速度で投げる
        grabbedRb.velocity = n * throwPower;

        OwnerOnRopeReturned();
        StartCoroutine(ReenableAfterDelay(postThrowNoHitTime));

        grabbedRb = null;
        grabbedEnemy = null;
        grabbedStun = null;
        grabbedAIs = null;

        Destroy(gameObject);
        return true;
    }

    IEnumerator ReenableAfterDelay(float t)
    {
        yield return new WaitForSeconds(t);
        RestoreIgnoredCollisions();
        RestoreEnemyAIs();
    }

    void RestoreEnemyAIs()
    {
        if (grabbedAIs == null) return;
        for (int i = 0; i < grabbedAIs.Length; i++)
            if (grabbedAIs[i]) grabbedAIs[i].enabled = true;
    }

    // ===== Owner helpers =====
    Transform OwnerTransform()
    {
        if (playerController) return playerController.transform;
        if (mawaruController) return mawaruController.transform;
        return playerRb ? playerRb.transform : transform;
    }

    Collider2D[] OwnerColliders()
    {
        var o = OwnerTransform();
        if (!o) return null;
        return o.GetComponentsInChildren<Collider2D>(true);
    }

    void OwnerSetHanging(bool h)
    {
        if (playerController) playerController.SetHanging(h);
        if (mawaruController) mawaruController.SetHanging(h);
    }

    void OwnerOnRopeReturned()
    {
        if (playerController) playerController.OnRopeReturned();
        if (mawaruController) mawaruController.OnRopeReturned();
    }
}