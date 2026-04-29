using System.Collections;
using UnityEngine;

public class StageResultStageMarker : MonoBehaviour
{
    [Header("Stage Info")]
    [SerializeField] private string stageDisplayName = "ステージ1";

    [Header("Behavior")]
    [SerializeField] private bool beginOnStart = true;

    private IEnumerator Start()
    {
        if (!beginOnStart)
            yield break;

        StageResultSession.EnsureInstance().BeginStage(stageDisplayName);

        yield return null;
        yield return null;

        StageResultAcquiredItemsTracker tracker = FindObjectOfType<StageResultAcquiredItemsTracker>(true);
        if (tracker != null)
        {
            tracker.ResetBaselinesNow();
        }
    }
}