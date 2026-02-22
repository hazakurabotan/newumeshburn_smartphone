using System.Collections;
using UnityEngine;

public class BreakableBlockExplode : MonoBehaviour
{
    [Header("HP (Hits)")]
    public int hitsToBreak = 5;
    public string bulletTag = "PlayerBullet"; // プレイヤー弾のTag

    [Header("Render")]
    public SpriteRenderer sr;        // block1x2のSpriteRenderer
    public Sprite explosionSprite;   // bakuha.png
    public Collider2D col;           // BoxCollider2D など

    [Header("Blink Before Explode")]
    public Color blinkColor = new Color(1f, 0.2f, 0.2f, 1f); // 赤
    public float blinkDuration = 0.5f;   // 赤点滅している合計時間
    public float blinkInterval = 0.08f;  // 点滅間隔（好みで）

    [Header("Explosion Show")]
    public float explosionShowTime = 0.2f; // bakuhaを見せる時間（0でもOK）

    int hitCount = 0;
    bool breaking = false;
    Color defaultColor;

    void Awake()
    {
        if (!sr) sr = GetComponent<SpriteRenderer>();
        if (!col) col = GetComponent<Collider2D>();
        if (sr) defaultColor = sr.color;
    }

    // 弾がTriggerの場合
    void OnTriggerEnter2D(Collider2D other)
    {
        if (breaking) return;
        if (!other.CompareTag(bulletTag)) return;

        OnHitByBullet(other.gameObject);
    }

    // 弾がCollisionの場合
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (breaking) return;
        if (!collision.collider.CompareTag(bulletTag)) return;

        OnHitByBullet(collision.collider.gameObject);
    }

    void OnHitByBullet(GameObject bullet)
    {
        hitCount++;

        // 弾を消したいなら（不要なら削除してOK）
        Destroy(bullet);

        if (hitCount >= hitsToBreak)
        {
            StartCoroutine(BreakRoutine());
        }
    }

    IEnumerator BreakRoutine()
    {
        breaking = true;

        // ブロックとしての当たり判定を止める（すり抜け防止ならOFFにしない/好きに選んでOK）
        if (col) col.enabled = false;

        // 赤点滅
        float t = 0f;
        bool on = false;
        while (t < blinkDuration)
        {
            on = !on;
            if (sr) sr.color = on ? blinkColor : defaultColor;
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }

        // 0.5秒後にbakuhaに切り替え、消える
        // （点滅が0.5秒なので「点滅後すぐbakuha」にしてる）
        if (sr)
        {
            sr.color = Color.white;
            if (explosionSprite) sr.sprite = explosionSprite;
        }

        // bakuhaを一瞬見せてから消す（0にしたら即消える）
        if (explosionShowTime > 0f) yield return new WaitForSeconds(explosionShowTime);

        Destroy(gameObject);
    }
}