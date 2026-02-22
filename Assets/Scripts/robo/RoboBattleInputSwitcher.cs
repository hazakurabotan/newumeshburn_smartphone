using UnityEngine;
using UnityEngine.InputSystem;

public class RoboBattleInputSwitcher : MonoBehaviour
{
    void Start()
    {
        var pi = FindObjectOfType<PlayerInput>();
        if (pi != null)
        {
            pi.SwitchCurrentActionMap("RoboBattle");
        }
    }
}
