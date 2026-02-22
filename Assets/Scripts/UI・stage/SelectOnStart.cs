using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectOnStart : MonoBehaviour
{
    public Button first;
    void OnEnable()
    {
        if (first != null)
            EventSystem.current.SetSelectedGameObject(first.gameObject);
    }
}
