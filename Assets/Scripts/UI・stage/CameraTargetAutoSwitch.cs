using UnityEngine;

public class CameraTargetAutoSwitch : MonoBehaviour
{
    [Header("Refs")]
    public FollowTarget2D_Bounds follow; // Main Camera ‚Ì FollowTarget2D_Bounds

    void Awake()
    {
        if (!follow) follow = GetComponent<FollowTarget2D_Bounds>();
    }

    void LateUpdate()
    {
        if (!follow) return;

        var t = CharacterSwitchManager.ActiveTarget;
        if (t) follow.target = t;
    }
}