using UnityEngine;
using System.Collections;
using TMPro;

public class BossPatternController : MonoBehaviour
{
    [Header("スラッシュ関連")]
    public Sprite normalFarSprite;
    public Sprite normalCloseSprite;
    public Sprite slashReadySprite;
    public Sprite slashAttackSprite;

    [Header("のけぞりスプライト")]
    public Sprite staggerSprite;

    [Header("プレイヤーへのダメージ")]
    public int slashDamageToPlayer = 10;
    public PlayerHP playerHP;
    public MechaGuardController playerGuard;

    [Header("スラッシュ予告UI")]
    public GameObject slashNoticePanel;
    public TMP_Text slashNoticeText;
    [TextArea(2, 3)] public string leftSlashNotice = "ひだりから\nスラッシュがくるよ";
    [TextArea(2, 3)] public string rightSlashNotice = "みぎから\nスラッシュがくるよ";

    [Header("スラッシュ予告タイミング")]
    [Tooltip("BossMechaがスラッシュ攻撃を開始する何秒前に予告を出すか")]
    public float slashNoticeLeadTime = 2.0f;

    [Tooltip("スラッシュ直前の溜め時間。変更前の状態に戻すため0.5秒")]
    public float slashReadyDuration = 0.5f;

    [Tooltip("BossMechaが消える演出時間")]
    public float disappearDuration = 0.3f;

    [Tooltip("BossMechaが出現する演出時間")]
    public float appearDuration = 0.3f;

    [Header("奥側のポイント")]
    public Transform farLeft;
    public Transform farCenter;
    public Transform farRight;

    [Header("手前側のポイント")]
    public Transform closeLeft;
    public Transform closeRight;

    [Header("移動設定")]
    public float moveSpeed = 5f;
    public float waitTimeAtPoint = 0.5f;
    public float farMoveDuration = 3f;

    private BossHP bossHP;
    private SpriteRenderer sr;
    private Collider2D col;

    private bool slashCanBeInterrupted = false;
    private bool slashInterrupted = false;

    private void Awake()
    {
        bossHP = GetComponent<BossHP>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        ResolvePlayerReferences();
        ResolveSlashNoticeReferences();
        HideSlashNotice();
    }

    private void ResolvePlayerReferences()
    {
        if (playerHP == null)
        {
            GameObject playerObj = GameObject.FindWithTag("PlayerCore");

            if (playerObj != null)
            {
                playerHP =
                    playerObj.GetComponent<PlayerHP>() ??
                    playerObj.GetComponentInChildren<PlayerHP>() ??
                    playerObj.GetComponentInParent<PlayerHP>();
            }
        }

        if (playerHP == null)
        {
            playerHP = FindObjectOfType<PlayerHP>();
        }

        if (playerGuard == null && playerHP != null)
        {
            playerGuard =
                playerHP.GetComponent<MechaGuardController>() ??
                playerHP.GetComponentInChildren<MechaGuardController>() ??
                playerHP.GetComponentInParent<MechaGuardController>();
        }

        if (playerGuard == null)
        {
            playerGuard = FindObjectOfType<MechaGuardController>();
        }

        if (playerHP == null)
        {
            Debug.LogWarning("[BossPatternController] PlayerHP が見つかりません。BossMecha の Player HP に PlayerCore を入れてください。");
        }

        if (playerGuard == null)
        {
            Debug.LogWarning("[BossPatternController] MechaGuardController が見つかりません。防御判定なしで進みます。");
        }
    }

    private void ResolveSlashNoticeReferences()
    {
        if (slashNoticePanel != null && slashNoticeText == null)
        {
            slashNoticeText = slashNoticePanel.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void ShowSlashNotice(bool fromLeft)
    {
        ResolveSlashNoticeReferences();

        if (slashNoticeText != null)
        {
            slashNoticeText.text = fromLeft ? leftSlashNotice : rightSlashNotice;
        }

        if (slashNoticePanel != null)
        {
            slashNoticePanel.SetActive(true);
        }
    }

    private void HideSlashNotice()
    {
        if (slashNoticeText != null)
        {
            slashNoticeText.text = "";
        }

        if (slashNoticePanel != null)
        {
            slashNoticePanel.SetActive(false);
        }
    }

    public void OnPunchedByPlayer()
    {
        if (slashCanBeInterrupted)
        {
            slashInterrupted = true;
            Debug.Log("[BossPatternController] Slash interrupted by player punch.");
        }
    }

    private IEnumerator Start()
    {
        ResolvePlayerReferences();
        ResolveSlashNoticeReferences();
        HideSlashNotice();

        if (farCenter != null)
        {
            transform.position = farCenter.position;
        }

        if (normalFarSprite != null && sr != null)
        {
            sr.sprite = normalFarSprite;
        }

        Debug.Log("[BossPatternController] Start");

        while (true)
        {
            if (bossHP != null && bossHP.IsDead)
            {
                Debug.Log("[BossPatternController] End : boss is dead");
                HideSlashNotice();
                yield break;
            }

            bool leftPattern = Random.value < 0.5f;
            Debug.Log("[BossPatternController] DoPattern left = " + leftPattern);

            yield return StartCoroutine(DoPattern(leftPattern));
        }
    }

    private IEnumerator DoPattern(bool isLeft)
    {
        yield return StartCoroutine(DoMissileAttack());

        yield return StartCoroutine(DoFarMove());

        if (farCenter != null)
        {
            yield return StartCoroutine(MoveTo(farCenter.position));
        }

        Transform sideFar = isLeft ? farLeft : farRight;
        if (sideFar != null)
        {
            yield return StartCoroutine(MoveTo(sideFar.position));
        }

        // ここで予告を出す。
        // この後の「消える → 出る → 溜め」込みで、スラッシュ開始の約2秒前になる。
        yield return StartCoroutine(ShowSlashNoticeBeforeSlash(isLeft));

        yield return StartCoroutine(Disappear());

        Transform closePos = isLeft ? closeLeft : closeRight;

        if (closePos != null)
        {
            transform.position = closePos.position;
        }

        yield return StartCoroutine(Appear());

        if (normalCloseSprite != null && sr != null)
        {
            sr.sprite = normalCloseSprite;
        }

        yield return StartCoroutine(DoSlash(isLeft));

        yield return StartCoroutine(Disappear());

        if (sideFar != null)
        {
            transform.position = sideFar.position;
        }

        yield return StartCoroutine(Appear());

        if (normalFarSprite != null && sr != null)
        {
            sr.sprite = normalFarSprite;
        }

        yield return StartCoroutine(DoMissileAttack());

        yield return StartCoroutine(DoFarMove());
    }

    private IEnumerator ShowSlashNoticeBeforeSlash(bool fromLeft)
    {
        ShowSlashNotice(fromLeft);

        float timeUntilSlashAfterThisWait =
            Mathf.Max(0f, disappearDuration) +
            Mathf.Max(0f, appearDuration) +
            Mathf.Max(0f, slashReadyDuration);

        float waitBeforeMove =
            Mathf.Max(0f, slashNoticeLeadTime - timeUntilSlashAfterThisWait);

        Debug.Log(
            "[BossPatternController] Slash notice. fromLeft = " +
            fromLeft +
            " / leadTime = " +
            slashNoticeLeadTime +
            " / waitBeforeMove = " +
            waitBeforeMove
        );

        float timer = waitBeforeMove;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator MoveTo(Vector3 targetPos)
    {
        while ((transform.position - targetPos).sqrMagnitude > 0.001f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        yield return new WaitForSeconds(waitTimeAtPoint);
    }

    private IEnumerator DoFarMove()
    {
        if (farLeft == null || farRight == null)
        {
            yield break;
        }

        float t = 0f;
        Vector3 a = farLeft.position;
        Vector3 b = farRight.position;

        while (t < farMoveDuration)
        {
            float pingpong = Mathf.PingPong(t * 0.5f, 1f);
            transform.position = Vector3.Lerp(a, b, pingpong);

            t += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator DoMissileAttack()
    {
        int count = Random.Range(3, 6);

        for (int i = 0; i < count; i++)
        {
            FireMissile();
            yield return new WaitForSeconds(0.4f);
        }

        yield return new WaitForSeconds(0.5f);
    }

    private void FireMissile()
    {
        // 既に BossMissileShooter を別で使っている場合はここは空のままでOK
    }

    private IEnumerator DoSlash(bool fromLeft)
    {
        EnableCloseHitbox(true);

        slashCanBeInterrupted = true;
        slashInterrupted = false;

        if (slashReadySprite != null && sr != null)
        {
            sr.sprite = slashReadySprite;
        }

        Debug.Log("[BossPatternController] Slash ready. fromLeft = " + fromLeft);

        float preTime = Mathf.Max(0f, slashReadyDuration);

        while (preTime > 0f && !slashInterrupted)
        {
            preTime -= Time.deltaTime;
            yield return null;
        }

        slashCanBeInterrupted = false;
        EnableCloseHitbox(false);
        HideSlashNotice();

        if (slashInterrupted)
        {
            yield return StartCoroutine(PlayStaggerAnim());
            yield break;
        }

        yield return StartCoroutine(PlaySlashAnimAndHit(fromLeft));
    }

    private IEnumerator Disappear()
    {
        if (col != null)
        {
            col.enabled = false;
        }

        if (sr != null)
        {
            sr.enabled = false;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, disappearDuration));
    }

    private IEnumerator Appear()
    {
        if (sr != null)
        {
            sr.enabled = true;
        }

        if (col != null)
        {
            col.enabled = true;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, appearDuration));
    }

    private void EnableCloseHitbox(bool on)
    {
        // 今回は直接ダメージ方式なので空でOK
    }

    private IEnumerator PlayStaggerAnim()
    {
        Debug.Log("[BossPatternController] Boss stagger");

        if (staggerSprite != null && sr != null)
        {
            sr.sprite = staggerSprite;
        }

        yield return new WaitForSeconds(0.2f);

        if (normalCloseSprite != null && sr != null)
        {
            sr.sprite = normalCloseSprite;
        }

        HideSlashNotice();
    }

    private IEnumerator PlaySlashAnimAndHit(bool fromLeft)
    {
        if (slashAttackSprite != null && sr != null)
        {
            sr.sprite = slashAttackSprite;
        }

        Debug.Log("[BossPatternController] Slash attack start. fromLeft = " + fromLeft);

        yield return new WaitForSeconds(0.15f);

        ResolvePlayerReferences();

        bool guarded = false;

        if (playerGuard != null)
        {
            if (fromLeft)
            {
                guarded = playerGuard.IsGuardingFromLeftAttack();
            }
            else
            {
                guarded = playerGuard.IsGuardingFromRightAttack();
            }
        }

        if (guarded)
        {
            Debug.Log("[BossPatternController] Slash blocked by guard.");
        }
        else
        {
            if (playerHP != null)
            {
                Debug.Log("[BossPatternController] Slash hit player. damage = " + slashDamageToPlayer);
                playerHP.DamageToPlayer(slashDamageToPlayer);
            }
            else
            {
                Debug.LogWarning("[BossPatternController] Slash hit, but PlayerHP is null.");
            }
        }

        yield return new WaitForSeconds(0.25f);

        if (normalCloseSprite != null && sr != null)
        {
            sr.sprite = normalCloseSprite;
        }

        HideSlashNotice();
    }
}