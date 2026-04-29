using TMPro;
using UnityEngine;

public class TutorialPopupTrigger2D : MonoBehaviour
{
    [Header("表示対象")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text messageText;

    [Header("表示メッセージ")]
    [TextArea(2, 5)]
    [SerializeField] private string message = "このボタンでこの動作ができるよ！";

    [Header("反応する相手")]
    [SerializeField] private string targetTag = "Player";

    [Header("オプション")]
    [SerializeField] private bool hideOnExit = true;
    [SerializeField] private bool showOnlyOnce = false;

    private bool hasShownOnce = false;
    private int insideCount = 0;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        ApplyMessage();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsTarget(other))
            return;

        insideCount++;

        if (showOnlyOnce && hasShownOnce)
            return;

        ShowPopup();
        hasShownOnce = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsTarget(other))
            return;

        insideCount = Mathf.Max(0, insideCount - 1);

        if (!hideOnExit)
            return;

        if (insideCount == 0)
        {
            HidePopup();
        }
    }

    private bool IsTarget(Collider2D other)
    {
        return other.CompareTag(targetTag);
    }

    private void ShowPopup()
    {
        ApplyMessage();

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
    }

    private void HidePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    private void ApplyMessage()
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    public void SetMessage(string newMessage)
    {
        message = newMessage;
        ApplyMessage();
    }

    public void ForceHide()
    {
        insideCount = 0;
        HidePopup();
    }
}