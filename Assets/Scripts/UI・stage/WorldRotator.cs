// WorldRotator.cs (v2)
using System.Collections;
using UnityEngine;

public class WorldRotator : MonoBehaviour
{
    [Header("Rotate this rigidbody (Tilemap/CompositeÇ…ïtÇØÇΩRB)")]
    public Rigidbody2D target;               // Å© Ç±Ç±Ç… Tilemap ÇÃRBÇäÑÇËìñÇƒÇÈ
    public float duration = 0.6f;
    public bool freezePlayerDuringTurn = true;

    void Awake()
    {
        if (!target) target = GetComponent<Rigidbody2D>(); // ï€åØ
    }

    public void RotateBy90CW() { if (target) StartCoroutine(RotateTo(target.rotation - 90f)); }
    public void RotateBy90CCW() { if (target) StartCoroutine(RotateTo(target.rotation + 90f)); }

    IEnumerator RotateTo(float targetAngle)
    {
        PlayerController pc = null; Rigidbody2D prb = null;
        if (freezePlayerDuringTurn)
        {
            pc = FindObjectOfType<PlayerController>();
            if (pc) pc.enabled = false;
            if (pc && (prb = pc.GetComponent<Rigidbody2D>())) { prb.velocity = Vector2.zero; prb.isKinematic = true; }
        }

        float start = target.rotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.fixedDeltaTime / Mathf.Max(0.01f, duration);
            float ang = Mathf.LerpAngle(start, targetAngle, Mathf.SmoothStep(0, 1, t));
            target.MoveRotation(ang);          // Å© ìñÇΩÇËîªíËÇ‡àÍèèÇ…âÒÇÈ
            yield return new WaitForFixedUpdate();
        }
        target.MoveRotation(targetAngle);
        Physics2D.SyncTransforms();

        if (freezePlayerDuringTurn)
        {
            if (prb) prb.isKinematic = false;
            if (pc) pc.enabled = true;
        }
    }
}
