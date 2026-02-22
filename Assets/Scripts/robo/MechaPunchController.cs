using UnityEngine;

public class MechaPunchController : MonoBehaviour
{
    [Header("左パンチ: hidarite1 オブジェクト")]
    public GameObject leftPunchObj;

    [Header("右パンチ: migite1 オブジェクト")]
    public GameObject rightPunchObj;

    [Header("パンチが表示される時間(秒)")]
    public float punchDuration = 0.2f;

    float leftTimer;
    float rightTimer;

    public int damageToBoss = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ボスに当たった？
        BossHP bossHp = other.GetComponentInParent<BossHP>();
        if (bossHp != null)
        {
            // ダメージ
            bossHp.TakeDamage(damageToBoss);

            // 斬撃キャンセル用の通知も送る
            BossPatternController pattern = other.GetComponentInParent<BossPatternController>();
            if (pattern != null)
            {
                pattern.OnPunchedByPlayer();
            }
        }
    }


    // RoboBattleController から呼ばれる
    public void LeftPunch()
    {
        if (leftPunchObj == null) return;

        leftPunchObj.SetActive(true);
        leftTimer = punchDuration;
    }

    // RoboBattleController から呼ばれる
    public void RightPunch()
    {
        if (rightPunchObj == null) return;

        rightPunchObj.SetActive(true);
        rightTimer = punchDuration;
    }

    void Update()
    {
        if (leftTimer > 0f)
        {
            leftTimer -= Time.deltaTime;
            if (leftTimer <= 0f && leftPunchObj != null)
                leftPunchObj.SetActive(false);
        }

        if (rightTimer > 0f)
        {
            rightTimer -= Time.deltaTime;
            if (rightTimer <= 0f && rightPunchObj != null)
                rightPunchObj.SetActive(false);
        }
    }



}
