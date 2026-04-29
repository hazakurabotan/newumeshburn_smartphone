using UnityEngine;

public class StageClearToResult : MonoBehaviour
{
    [Header("Result Panel")]
    [SerializeField] private StageResultPanelController resultPanelController;

    [Header("Trigger")]
    [SerializeField] private bool useTriggerEnter2D = true;
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private string[] acceptedTags = { "Player" };

    private bool alreadyTriggered = false;

    private void Awake()
    {
        if (resultPanelController == null)
        {
            resultPanelController = FindObjectOfType<StageResultPanelController>(true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTriggerEnter2D)
            return;

        if (!IsAccepted(other))
            return;

        TriggerResult();
    }

    public void TriggerResult()
    {
        if (triggerOnlyOnce && alreadyTriggered)
            return;

        alreadyTriggered = true;

        if (resultPanelController == null)
        {
            resultPanelController = FindObjectOfType<StageResultPanelController>(true);
        }

        if (resultPanelController == null)
        {
            Debug.LogError("[StageClearToResult] StageResultPanelController Ç™å©Ç¬Ç©ÇËÇ‹ÇπÇÒÅB");
            return;
        }

        resultPanelController.ShowCurrentResult();
    }

    private bool IsAccepted(Collider2D other)
    {
        if (other == null)
            return false;

        if (acceptedTags == null || acceptedTags.Length == 0)
            return true;

        for (int i = 0; i < acceptedTags.Length; i++)
        {
            string tagName = acceptedTags[i];

            if (string.IsNullOrWhiteSpace(tagName))
                continue;

            if (other.CompareTag(tagName))
                return true;

            if (other.transform.root != null && other.transform.root.CompareTag(tagName))
                return true;
        }

        return false;
    }
}