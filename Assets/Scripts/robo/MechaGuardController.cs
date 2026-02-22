using UnityEngine;

public class MechaGuardController : MonoBehaviour
{
    [Header("左ガード: hidarite2 オブジェクト")]
    public GameObject leftGuardObj;

    [Header("右ガード: migite2 オブジェクト")]
    public GameObject rightGuardObj;

    // RoboBattleController から呼ばれる
    public void SetLeft(bool isOn)
    {
        if (leftGuardObj != null)
            leftGuardObj.SetActive(isOn);
    }

    // RoboBattleController から呼ばれる
    public void SetRight(bool isOn)
    {
        if (rightGuardObj != null)
            rightGuardObj.SetActive(isOn);
    }
}
