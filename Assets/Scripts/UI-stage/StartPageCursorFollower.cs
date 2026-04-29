using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class StartPageCursorFollower : MonoBehaviour
{
    [Header("このページのルート")]
    public RectTransform pageRoot;

    [Header("サイズの足し引き")]
    public Vector2 sizePadding = new Vector2(0f, 0f);

    [Header("位置の微調整")]
    public Vector2 positionOffset = Vector2.zero;

    private RectTransform _frame;
    private Graphic _graphic;
    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _frame = GetComponent<RectTransform>();
        _graphic = GetComponent<Graphic>();
        _canvasGroup = GetComponent<CanvasGroup>();

        // カーソル枠は前面に置いておく
        transform.SetAsLastSibling();

        HideVisualOnly();
    }

    private void LateUpdate()
    {
        if (_frame == null)
        {
            return;
        }

        if (EventSystem.current == null)
        {
            HideVisualOnly();
            return;
        }

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current == null)
        {
            HideVisualOnly();
            return;
        }

        Selectable sel = current.GetComponent<Selectable>();
        if (sel == null)
        {
            sel = current.GetComponentInParent<Selectable>();
        }

        if (sel == null || !sel.IsInteractable())
        {
            HideVisualOnly();
            return;
        }

        if (pageRoot != null && !sel.transform.IsChildOf(pageRoot))
        {
            HideVisualOnly();
            return;
        }

        RectTransform target = sel.transform as RectTransform;
        if (target == null)
        {
            HideVisualOnly();
            return;
        }

        // 違う親の下にいたら、対象と同じ親へ移す
        if (_frame.parent != target.parent)
        {
            _frame.SetParent(target.parent, false);
            _frame.SetAsLastSibling();
        }

        ShowVisualOnly();

        _frame.anchorMin = target.anchorMin;
        _frame.anchorMax = target.anchorMax;
        _frame.pivot = target.pivot;
        _frame.anchoredPosition = target.anchoredPosition + positionOffset;
        _frame.sizeDelta = target.sizeDelta + sizePadding;
    }

    private void ShowVisualOnly()
    {
        if (_graphic != null)
        {
            _graphic.enabled = true;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }

    private void HideVisualOnly()
    {
        if (_graphic != null)
        {
            _graphic.enabled = false;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }
}