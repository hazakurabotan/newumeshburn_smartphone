using UnityEngine;
using UnityEngine.UI;

// ボスのHPバーを管理するクラス
// HPの値に応じて画像を切り替える仕組みです
public class BossHpBarController : MonoBehaviour
{
    // HPバー用のImageコンポーネント（Inspectorで割り当て）
    public Image hpImage;

    // HP段階ごとに用意したスプライト画像の配列（0=HP0, 1=HP1 ... の順で格納）
    public Sprite[] hpSprites;

    // ボスの最大HP
    public int maxHp = 15;

    // 現在のHP（初期値は最大値）
    public int currentHp = 15;

    // HPを設定し、HPバー画像も更新する関数
    // 例: SetHp(7) ならHPバーを「7」の画像に切り替える
    public void SetHp(int hp)
    {
        // hpの値が0未満や最大値超えにならないよう調整（Clamp＝はみ出たら端で固定）
        currentHp = Mathf.Clamp(hp, 0, maxHp);

        // hpSprites配列が正しくセットされていて、currentHp番目の画像が存在する場合のみ
        if (hpSprites != null && hpSprites.Length > currentHp)
        {
            // HPバー用Imageに対応したスプライト画像を割り当てる
            hpImage.sprite = hpSprites[currentHp];
        }
        // （補足）
        // もしhpSpritesがセットされていない、または配列が足りない場合は何も起きません
        // InspectorでhpSprites配列に「HP0〜HP10」まで順番にスプライトを入れておくのがコツです
    }
}
