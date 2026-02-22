using UnityEngine;
using UnityEngine.SceneManagement;

public class RoboBossHP : MonoBehaviour
{
    public int maxHP = 50;
    int currentHP;

    void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int dmg)
    {
        if (dmg <= 0) return;

        currentHP -= dmg;
        if (currentHP <= 0)
        {
            currentHP = 0;
            StartCoroutine(GoResult());
        }

        // ここで BOSS HPバー更新
    }

    System.Collections.IEnumerator GoResult()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("Result");   // BuildSettingsにある Result へ
    }
}
