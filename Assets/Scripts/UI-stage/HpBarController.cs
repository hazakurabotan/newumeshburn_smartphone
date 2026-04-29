using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------
// HpBarController
// プレイヤーのHPバー（画像差し替え式）を管理するスクリプト
// -----------------------------------------------
public class HpBarController : MonoBehaviour
{
    public Image barImage;      // HPバー表示用のImageコンポーネント
    private Sprite[] hpSprites; // HP値ごとのバー画像配列（0～15まで16枚）

    void Awake()
    {
        // --- HPバー画像（Resources/HpBars/HP0.png ～ HP15.png）を一括ロード ---
        hpSprites = new Sprite[16]; // 配列を16個分用意（0～15HP用）
        for (int i = 0; i <= 15; i++)
        {
            // 各HP値に対応する画像ファイルを読み込む
            hpSprites[i] = Resources.Load<Sprite>($"HpBars/HP{i}");
        }
    }

    /// <summary>
    /// HPバーを現在値にあわせて更新（0～15）
    /// </summary>
    /// <param name="currentHp">現在のHP</param>
    /// <param name="maxHp">最大HP（デフォルト15）</param>
    public void SetHp(int currentHp, int maxHp = 15)
    {
        // 0〜15 段階に正規化
        int idx = Mathf.RoundToInt(Mathf.Clamp01((float)currentHp / Mathf.Max(1, maxHp)) * 15f);
        idx = Mathf.Clamp(idx, 0, 15);

        if (hpSprites != null && idx < hpSprites.Length && hpSprites[idx] != null)
            barImage.sprite = hpSprites[idx];
    }


}
