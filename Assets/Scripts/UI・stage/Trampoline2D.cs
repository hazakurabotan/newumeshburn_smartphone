using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline2D : MonoBehaviour
{
    [Header("Bounce")]
    public float bounceVelocity = 20f;     // 真上に高く
    public float tickCooldown = 0.15f;     // 多段防止

    [Header("Targets")]
    public string playerTag = "Player";

    [Header("Glow (Red)")]
    public SpriteRenderer trampolineRenderer; // 光らせたい見た目（親のSpriteRendererを入れる）
    public Color glowColor = new Color(1f, 0.2f, 0.2f, 1f);
    public float glowTime = 0.08f;

    readonly Dictionary<int, float> nextOk = new();
    Color defaultColor;

    void Awake()
    {
        if (trampolineRenderer != null) defaultColor = trampolineRenderer.color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        Bounce(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        // “入りっぱなし”でも連続発火しないように（段差でEnterが取りにくい時の保険）
        if (!other.CompareTag(playerTag)) return;
        Bounce(other);
    }

    void Bounce(Collider2D other)
    {
        int id = other.GetInstanceID();
        float now = Time.time;
        if (nextOk.TryGetValue(id, out var t) && now < t) return;
        nextOk[id] = now + tickCooldown;

        var rb = other.attachedRigidbody;
        if (!rb) return;

        // 真上に高く：Xは0、Yだけ上書き
        rb.velocity = new Vector2(0f, bounceVelocity);

        // Mawaru/Playerが速度を上書きするタイプでも効くように保険（任意）
        other.SendMessage("OnTrampolineBounce", bounceVelocity, SendMessageOptions.DontRequireReceiver);

        if (trampolineRenderer) StartCoroutine(GlowOnce());
    }

    IEnumerator GlowOnce()
    {
        trampolineRenderer.color = glowColor;
        yield return new WaitForSeconds(glowTime);
        trampolineRenderer.color = defaultColor;
    }
}