using UnityEngine;
using System.Collections;

public class MechaFist : MonoBehaviour
{
    // MechaFist
    public int damage = 5;                  // 通常パンチ
    public float punchTime = 0.2f;     // 前に出て戻るまでの時間
    public Vector3 punchOffset;        // どれくらい前に出すか（ローカル座標）

    bool isPunching = false;
    Vector3 defaultLocalPos;
    bool canHit = false;

    void Awake()
    {
        defaultLocalPos = transform.localPosition;
        gameObject.SetActive(false);
    }

    public void Attack()
    {
        if (isPunching) return;
        StartCoroutine(PunchRoutine());
    }

    IEnumerator PunchRoutine()
    {
        isPunching = true;
        gameObject.SetActive(true);
        canHit = true;

        float t = 0f;
        while (t < punchTime)
        {
            float rate = t / punchTime;
            // 行きと帰りをまとめた簡単なカーブ（0→1→0）
            float curve = Mathf.Sin(rate * Mathf.PI);
            transform.localPosition = defaultLocalPos + punchOffset * curve;

            t += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = defaultLocalPos;
        gameObject.SetActive(false);
        isPunching = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!canHit) return;
        if (!other.CompareTag("Boss")) return;

        var bossHp = other.GetComponent<BossHP>();
        if (bossHp != null)
        {
            bossHp.TakeDamage(damage);
        }

        var pattern = other.GetComponent<BossPatternController>();
        if (pattern != null)
        {
            pattern.OnPunchedByPlayer();  // ここが「斬撃キャンセル」用トリガー
        }

        canHit = false;
    }
}
