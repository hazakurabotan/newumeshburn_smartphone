using UnityEngine;

public sealed class StageCarryOverStageMarker : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] private bool resetAllCarryOverDataOnEnter = false;

    private void Start()
    {
        StageCarryOverRuntime runtime = StageCarryOverRuntime.Instance;

        if (resetAllCarryOverDataOnEnter)
        {
            runtime.ResetAllData();
        }

        runtime.BeginStageTracking();
    }
}