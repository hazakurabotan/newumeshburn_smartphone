using UnityEngine;
using UnityEngine.UI;

public class EnemyHpBarController : MonoBehaviour
{
    [SerializeField] Image fillImage; // 中身のImage（枠じゃないほう）

    void Reset() => AutoAssign();
    void Awake()
    {
        AutoAssign();
        ForceImageAsFilled();
        SetHp(1, 1); // 100%表示で初期化
    }

    void AutoAssign()
    {
        if (!fillImage)
        {
            // 子階層から最初に見つかった Image を仮で使用
            fillImage = GetComponentInChildren<Image>(true);
        }
    }

    void ForceImageAsFilled()
    {
        if (!fillImage) return;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0; // Left
        fillImage.fillAmount = 1f;
    }

    public void SetHp(int currentHp, int maxHp)
    {
        if (!fillImage || maxHp <= 0) return;
        float ratio = Mathf.Clamp01((float)currentHp / maxHp);
        fillImage.fillAmount = ratio;
    }
}
