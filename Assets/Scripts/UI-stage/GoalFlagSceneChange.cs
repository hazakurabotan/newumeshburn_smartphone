using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalFlagSceneChange : MonoBehaviour
{
    public string nextSceneName = "Scenes/Stage2"; // Inspector‚Å‚àOK

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
