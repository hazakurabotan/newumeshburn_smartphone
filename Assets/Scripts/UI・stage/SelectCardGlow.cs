using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SelectCardGlow : MonoBehaviour,
    ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("子の Glow(Image) を自動検出します。見つからなければ最初の Image を使います。")]
    public Image glow; // 任意。未設定なら自動で探す。

    [Header("Pulse")]
    public float minAlpha = 0.35f;
    public float maxAlpha = 0.9f;
    public float speed = 3f;

    bool focused;
    Coroutine pulseCo;

    void Awake()
    {
        if (glow == null)
        {
            // 優先：名前GlowのImage → 子階層のImage → 自分の子のImage
            var t = transform.Find("Glow");
            if (t != null) glow = t.GetComponent<Image>();
            if (glow == null) glow = GetComponentInChildren<Image>(true);

            if (glow == null)
            {
                Debug.LogWarning("[SelectCardGlow] 子に Image が見つかりません。Glowなしで動作します。", this);
                return;
            }
        }

        // 初期は非表示にしておく（フォーカスが来たら点灯）
        glow.gameObject.SetActive(false);
        // 念のためクリックを邪魔しない
        glow.raycastTarget = false;
    }

    public void OnSelect(BaseEventData e) => SetFocus(true);
    public void OnDeselect(BaseEventData e) => SetFocus(false);
    public void OnPointerEnter(PointerEventData e) => SetFocus(true);
    public void OnPointerExit(PointerEventData e) => SetFocus(false);

    void SetFocus(bool on)
    {
        if (glow == null) return;
        if (focused == on) return;

        focused = on;
        glow.gameObject.SetActive(on);

        if (on)
        {
            if (pulseCo != null) StopCoroutine(pulseCo);
            pulseCo = StartCoroutine(Pulse());
        }
        else
        {
            if (pulseCo != null) StopCoroutine(pulseCo);
            pulseCo = null;
        }
    }

    System.Collections.IEnumerator Pulse()
    {
        var baseColor = glow.color;
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * speed;
            float a = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(t) + 1f) * 0.5f);
            glow.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
            yield return null;
        }
    }
}
