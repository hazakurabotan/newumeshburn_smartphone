using UnityEngine;

public class MechaGuardController : MonoBehaviour
{
    [Header("左ガード: hidarite2 オブジェクト")]
    public GameObject leftGuardObj;

    [Header("右ガード: migite2 オブジェクト")]
    public GameObject rightGuardObj;

    public bool IsLeftGuarding
    {
        get
        {
            return leftGuardObj != null && leftGuardObj.activeInHierarchy;
        }
    }

    public bool IsRightGuarding
    {
        get
        {
            return rightGuardObj != null && rightGuardObj.activeInHierarchy;
        }
    }

    public bool IsAnyGuarding
    {
        get
        {
            return IsLeftGuarding || IsRightGuarding;
        }
    }

    public void SetLeft(bool isOn)
    {
        if (leftGuardObj != null)
        {
            leftGuardObj.SetActive(isOn);
        }
    }

    public void SetRight(bool isOn)
    {
        if (rightGuardObj != null)
        {
            rightGuardObj.SetActive(isOn);
        }
    }

    public bool IsGuardingFromLeftAttack()
    {
        return IsLeftGuarding;
    }

    public bool IsGuardingFromRightAttack()
    {
        return IsRightGuarding;
    }
}