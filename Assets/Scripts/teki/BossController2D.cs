using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BossController2D : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public Transform muzzle;
    public GameObject bossBulletPrefab;

    [Header("Targets 2人対応")]
    public Transform secondTarget;
    public string secondTargetName = "mawaru13";
    public bool preferNearestTarget = true;

    [Header("Activation / Guard")]
    public float activationRange = 10f;
    public bool showActivationGizmo = true;

    [Header("HP")]
    public int maxHP = 20;
    public int currentHP = 20;

    [Tooltip("撃破後、最終的に移動するシーン名。今回は ImpactRunScene")]
    public string conversationSceneName = "ImpactRunScene";

    bool conversationTriggered = false;

    [Header("Defeat Dialogue")]
    [Tooltip("HP0時に再生する会話。BossDefeatTalkなどに付けた BossIntroDialogueCutscene を入れる")]
    public BossIntroDialogueCutscene defeatDialogue;

    [Tooltip("撃破後会話が設定されていない場合、すぐ conversationSceneName に移動する")]
    public bool loadSceneDirectlyIfNoDefeatDialogue = true;

    [Tooltip("撃破後、会話開始までの待ち時間")]
    public float defeatDialogueStartDelay = 0.15f;

    [Tooltip("会話終了後、シーン移動までの待ち時間")]
    public float sceneLoadDelayAfterDefeatDialogue = 0.15f;

    [Tooltip("撃破後にボスのColliderをOFFにして追加ヒットを防ぐ")]
    public bool disableBossColliderOnDefeat = true;

    [Header("Move / Jump")]
    public float runSpeed = 6f;
    public float jumpForwardForce = 8f;
    public LayerMask groundMask;
    public float groundCheckRadius = 0.08f;
    public Transform groundCheck;

    [Header("Shoot")]
    public float shotSpeed = 10f;
    public float afterShootFreeze = 2f;

    [Header("Misc")]
    public float thinkInterval = 0.35f;
    public float patternPause = 0.2f;

    [Header("Intro Dialogue")]
    public bool playIntroOnFirstApproach = true;
    public DialogueLine[] introLines;
    public bool freezePlayersDuringIntro = true;
    bool introDone = false;

    int face = 1;
    [SerializeField] bool spriteFacesRight = true;

    Rigidbody2D rb;
    Collider2D col;
    Animator anim;

    float CharUnit => Mathf.Max(col.bounds.size.x, col.bounds.size.y);

    bool IsGrounded => groundCheck
        ? Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask)
        : false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();

        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (!player)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (!secondTarget && !string.IsNullOrEmpty(secondTargetName))
        {
            var go = GameObject.Find(secondTargetName);
            if (go) secondTarget = go.transform;
        }

        ApplyVisualFacing();
    }

    void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(MainLoop());
    }

    IEnumerator MainLoop()
    {
        yield return WaitUntilTargetInRange();

        if (playIntroOnFirstApproach && !introDone && introLines != null && introLines.Length > 0)
        {
            yield return PlayIntroDialogue();
            introDone = true;
        }

        yield return AI();
    }

    IEnumerator AI()
    {
        (int type, int count)[] cycle =
        {
            (0, 2),
            (1, 1),
            (2, 1),
            (0, 1),
            (-1, 2),
        };

        int idx = 0;
        int lastRandom = -1;

        while (true)
        {
            if (!IsAnyTargetInRange())
            {
                rb.velocity = Vector2.zero;
                if (anim) anim.SetTrigger("Idle");
                yield return WaitUntilTargetInRange();
            }

            var step = cycle[idx];

            for (int i = 0; i < step.count; i++)
            {
                yield return new WaitForSeconds(thinkInterval);

                if (!IsAnyTargetInRange())
                {
                    rb.velocity = Vector2.zero;
                    if (anim) anim.SetTrigger("Idle");
                    break;
                }

                int pick;

                if (step.type < 0)
                {
                    var candidates = new List<int>() { 0, 1, 2 };

                    if (lastRandom >= 0)
                        candidates.Remove(lastRandom);

                    pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                    lastRandom = pick;
                }
                else
                {
                    pick = step.type;
                }

                switch (pick)
                {
                    case 0:
                        yield return Pattern1_ChargeThenHop();
                        break;

                    case 1:
                        yield return Pattern2_HighJump();
                        break;

                    case 2:
                        yield return Pattern3_StopAndShoot();
                        break;
                }

                yield return new WaitForSeconds(patternPause);
            }

            idx = (idx + 1) % cycle.Length;
        }
    }

    IEnumerator WaitUntilTargetInRange()
    {
        rb.velocity = Vector2.zero;

        if (anim) anim.SetTrigger("Idle");

        while (!IsAnyTargetInRange())
        {
            rb.velocity = Vector2.zero;
            yield return null;
        }
    }

    bool IsAnyTargetInRange()
    {
        var t = GetBestTarget();
        if (!t) return false;

        float r2 = activationRange * activationRange;
        return (t.position - transform.position).sqrMagnitude <= r2;
    }

    Transform GetBestTarget()
    {
        Transform a = player;
        Transform b = secondTarget;

        if (a == null && b == null)
            return null;

        if (!preferNearestTarget)
            return a != null ? a : b;

        if (a != null && b != null)
        {
            float da = (a.position - transform.position).sqrMagnitude;
            float db = (b.position - transform.position).sqrMagnitude;

            return da <= db ? a : b;
        }

        return a != null ? a : b;
    }

    IEnumerator PlayIntroDialogue()
    {
        rb.velocity = Vector2.zero;

        if (anim) anim.SetTrigger("Idle");

        if (freezePlayersDuringIntro)
        {
            var cm = CharacterSwitchManager.Instance;
            if (cm)
            {
                if (cm.mawaruInput) cm.mawaruInput.enabled = false;
                if (cm.playerInput) cm.playerInput.enabled = false;
            }
        }

        bool finished = false;

        var seq = DialogSequenceManager.Instance ?? FindObjectOfType<DialogSequenceManager>(true);
        if (seq != null)
        {
            seq.PlaySequence(introLines, () => finished = true);
            yield return new WaitUntil(() => finished);
        }
        else
        {
            foreach (var _ in introLines)
                yield return new WaitForSeconds(1.2f);
        }

        if (freezePlayersDuringIntro)
        {
            var cm = CharacterSwitchManager.Instance;
            if (cm)
            {
                if (cm.mawaruInput) cm.mawaruInput.enabled = true;
                if (cm.playerInput) cm.playerInput.enabled = true;
            }
        }
    }

    IEnumerator Pattern1_ChargeThenHop()
    {
        FaceToTarget();
        int dir = FacingDir();

        float targetDist = 5f * CharUnit;
        float moved = 0f;
        Vector2 last = rb.position;

        if (anim) anim.SetTrigger("Run");

        while (moved < targetDist)
        {
            rb.velocity = new Vector2(dir * runSpeed, rb.velocity.y);
            yield return null;

            moved += Vector2.Distance(rb.position, last);
            last = rb.position;
        }

        rb.velocity = new Vector2(0, rb.velocity.y);

        Flip();
        dir = FacingDir();

        if (IsGrounded)
        {
            if (anim) anim.SetTrigger("Jump");

            rb.velocity = new Vector2(dir * runSpeed * 0.9f, rb.velocity.y);

            rb.AddForce(
                new Vector2(dir * jumpForwardForce, JumpVelocityForHeight(1.0f * CharUnit)),
                ForceMode2D.Impulse
            );
        }

        float hopTarget = 3f * CharUnit;
        float hopMoved = 0f;
        last = rb.position;

        while (hopMoved < hopTarget)
        {
            yield return null;

            hopMoved += Vector2.Distance(rb.position, last);
            last = rb.position;
        }

        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    IEnumerator Pattern2_HighJump()
    {
        if (!IsGrounded) yield break;

        if (anim) anim.SetTrigger("Jump");

        float height = 3f * CharUnit;
        float vy = JumpVelocityForHeight(height);

        rb.velocity = new Vector2(0, vy);

        yield return new WaitUntil(() => rb.velocity.y <= 0f);
        yield return new WaitUntil(() => IsGrounded);
    }

    IEnumerator Pattern3_StopAndShoot()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);

        if (anim) anim.SetTrigger("Idle");

        yield return new WaitForSeconds(afterShootFreeze);

        FaceToTarget();

        var target = GetBestTarget();

        if (bossBulletPrefab && muzzle)
        {
            var b = Instantiate(bossBulletPrefab, muzzle.position, Quaternion.identity);

            Vector2 dir = target
                ? ((Vector2)(target.position - muzzle.position)).normalized
                : (FacingDir() >= 0 ? Vector2.right : Vector2.left);

            var rb2 = b.GetComponent<Rigidbody2D>();
            if (rb2) rb2.velocity = dir * shotSpeed;

            var dd = b.GetComponent<DamageDealer>();
            if (dd) dd.owner = gameObject;

            var bCol = b.GetComponent<Collider2D>();
            var myCol = GetComponent<Collider2D>();

            if (bCol && myCol)
                Physics2D.IgnoreCollision(bCol, myCol, true);
        }

        rb.velocity = new Vector2(FacingDir() * runSpeed * 0.2f, rb.velocity.y);

        if (anim) anim.SetTrigger("Special");

        yield return new WaitForSeconds(0.1f);
    }

    public void ApplyDamage(int dmg)
    {
        if (conversationTriggered) return;
        if (dmg <= 0) return;

        currentHP = Mathf.Max(0, currentHP - dmg);

        if (anim) anim.SetTrigger("Hurt");

        var hpBar = FindObjectOfType<BossHpBarController>();
        if (hpBar) hpBar.SetHp(currentHP);

        if (currentHP <= 0 && !conversationTriggered)
        {
            conversationTriggered = true;

            StopAllCoroutines();

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;

            StartCoroutine(DefeatFlow());
        }
    }

    IEnumerator DefeatFlow()
    {
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (anim) anim.SetTrigger("Idle");

        if (disableBossColliderOnDefeat && col)
            col.enabled = false;

        yield return new WaitForSecondsRealtime(defeatDialogueStartDelay);

        string nextSceneName = conversationSceneName;

        if (defeatDialogue != null)
        {
            bool finished = false;

            Action onFinished = () => finished = true;
            defeatDialogue.CutsceneFinished += onFinished;

            defeatDialogue.StartCutsceneFrom(GetDefeatDialogueStarter());

            yield return new WaitUntil(() => finished);

            defeatDialogue.CutsceneFinished -= onFinished;

            yield return new WaitForSecondsRealtime(sceneLoadDelayAfterDefeatDialogue);

            yield return LoadSceneIfExists(nextSceneName);
        }
        else
        {
            if (loadSceneDirectlyIfNoDefeatDialogue)
            {
                yield return LoadSceneIfExists(nextSceneName);
            }
            else
            {
                Debug.LogWarning("[Boss] defeatDialogue が設定されていないため、撃破後会話を再生できません。");
            }
        }
    }

    BossIntroDialogueCutscene.Starter GetDefeatDialogueStarter()
    {
        Transform best = GetBestTarget();

        if (best != null)
        {
            if (best.GetComponentInParent<MawaruController>() != null)
                return BossIntroDialogueCutscene.Starter.Mawaru;

            if (best.GetComponentInParent<PlayerController>() != null)
                return BossIntroDialogueCutscene.Starter.Player;
        }

        if (secondTarget != null && secondTarget.gameObject.activeInHierarchy)
            return BossIntroDialogueCutscene.Starter.Mawaru;

        return BossIntroDialogueCutscene.Starter.Player;
    }

    IEnumerator LoadSceneIfExists(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[Boss] 移動先シーン名が空です。conversationSceneName に ImpactRunScene を入れてください。");
            yield break;
        }

        bool sceneExists = false;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);

            if (Path.GetFileNameWithoutExtension(path) == sceneName)
            {
                sceneExists = true;
                break;
            }
        }

        if (!sceneExists)
        {
            Debug.LogWarning("[Boss] Scene '" + sceneName + "' がBuild Settingsに登録されていません。");
            yield break;
        }

        Debug.Log("[Boss] Load scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (conversationTriggered) return;

        var dd = other.GetComponent<DamageDealer>();
        if (dd) ApplyDamage(dd.damage);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (conversationTriggered) return;

        var dd = other.collider.GetComponent<DamageDealer>();
        if (dd) ApplyDamage(dd.damage);
    }

    void FaceToTarget()
    {
        var t = GetBestTarget();
        if (!t) return;

        int want = (t.position.x - transform.position.x) >= 0f ? 1 : -1;

        if (want != face)
            Flip();
    }

    int FacingDir()
    {
        return face;
    }

    void Flip()
    {
        face *= -1;
        ApplyVisualFacing();
    }

    void ApplyVisualFacing()
    {
        var s = transform.localScale;

        float baseSign = spriteFacesRight ? 1f : -1f;
        s.x = Mathf.Abs(s.x) * baseSign * face;

        transform.localScale = s;
    }

    float JumpVelocityForHeight(float h)
    {
        float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        return Mathf.Sqrt(2f * g * h);
    }

    void OnDrawGizmosSelected()
    {
        if (!showActivationGizmo) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRange);

        if (groundCheck)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}