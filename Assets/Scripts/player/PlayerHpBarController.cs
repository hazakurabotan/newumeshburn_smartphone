using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------------
// PlayerHpBarController
// プレイヤー用の「ハート型」や「アイコン型」HPバーを制御するクラス
// （HP0～HP3まで対応）
// -----------------------------------------------------
public class PlayerHpBarController : MonoBehaviour
{
    public Image hpImage;            // 実際に表示するHPバーのImage
    public Sprite[] hpSprites;       // HPごとの画像（インスペクターで並べてセット）
    public int maxHp = 3;            // 最大HP（画像配列と揃えておく）

    // --- HPの値にあわせてバー画像を切り替える ---
    public void SetHp(int hp)
    {
        // 値が範囲外にならないよう0～maxHpに制限
        int clamped = Mathf.Clamp(hp, 0, maxHp);

        // 配列と画像がちゃんと用意されているかチェック
        if (hpSprites != null && hpSprites.Length > clamped)
        {
            // HP値に対応した画像へ差し替え
            hpImage.sprite = hpSprites[clamped];
        }
    }
}
