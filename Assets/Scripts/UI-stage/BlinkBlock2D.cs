using System.Collections;
using UnityEngine;

public class BlinkBlock2D : MonoBehaviour
{
    [Header("Timing")]
    public float onTime = 1f;   // ï\é¶ÇµÇƒÇÈéûä‘
    public float offTime = 1f;  // è¡Ç¶ÇƒÇÈéûä‘

    [Header("Components")]
    public SpriteRenderer sr;
    public Collider2D col; // BoxCollider2DÇ»Ç«

    void Awake()
    {
        if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
        if (!col) col = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            SetActiveBlock(true);
            yield return new WaitForSeconds(onTime);

            SetActiveBlock(false);
            yield return new WaitForSeconds(offTime);
        }
    }

    void SetActiveBlock(bool active)
    {
        if (sr) sr.enabled = active;
        if (col) col.enabled = active;
    }
}