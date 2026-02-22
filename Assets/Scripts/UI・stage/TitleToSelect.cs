using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleToSelect : MonoBehaviour
{
    public void GoToCharacterSelect()
    {
        SceneManager.LoadScene("Opening");
    }
}
