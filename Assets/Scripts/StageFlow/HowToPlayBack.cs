using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class HowToPlayBack : MonoBehaviour
{
    public string backSceneName = "StageSelect";

    void Update()
    {
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            SceneManager.LoadScene(backSceneName);

        if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame)
            SceneManager.LoadScene(backSceneName);
    }
}