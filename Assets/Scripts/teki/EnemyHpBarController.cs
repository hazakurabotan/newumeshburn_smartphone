using UnityEngine;
using UnityEngine.UI;

public class EnemyHpBarController : MonoBehaviour
{
    public Image fillImage; // HPバー用Image（Type: Filled推奨）

    // HPを更新
    public void SetHp(int currentHp, int maxHp)
    {
        float ratio = Mathf.Clamp01((float)currentHp / maxHp);
        fillImage.fillAmount = ratio;
    }
}
