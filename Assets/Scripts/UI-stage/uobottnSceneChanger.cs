using UnityEngine;
using UnityEngine.SceneManagement;

public class uobottnSceneChanger : MonoBehaviour
{
    public string nextSceneName = "Stage3";

    private bool isPlayerInTrigger = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
        }
    }

    void Update()
    {
        if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.UpArrow))
        {
            Debug.Log("è„ÉLÅ[Ç≈LoadSceneé¿çs: " + nextSceneName);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}