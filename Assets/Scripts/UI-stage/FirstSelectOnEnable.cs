using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FirstSelectOnEnable : MonoBehaviour
{
    [SerializeField] private Selectable first;

    private void OnEnable()
    {
        // 次のフレームで選択を移す（レイアウト完了待ち）
        StartCoroutine(SelectNextFrame());
    }

    private IEnumerator SelectNextFrame()
    {
        yield return null;
        if (first == null) first = GetComponentInChildren<Selectable>(true);
        if (EventSystem.current != null && first != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }
}
