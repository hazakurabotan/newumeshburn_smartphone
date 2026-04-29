using UnityEngine;
using UnityEngine.InputSystem;

public class StageCharacterEnforcer : MonoBehaviour
{
    [Header("ステージ内に置いてあるキャラ（Hierarchyから入れる）")]
    public GameObject meguruObj;
    public GameObject nazoroidObj;

    [Header("タグ設定（操作キャラだけ Player にする）")]
    public string playerTag = "Player";
    public string inactiveTag = "Untagged";

    void Start()
    {
        // どっちが選ばれたか
        var selected = SelectedCharacter.Id;

        bool useMeguru = (selected == CharacterSelectManager.CharacterId.Meguru);
        bool useNazoroid = (selected == CharacterSelectManager.CharacterId.Nazoroid);

        Apply(meguruObj, useMeguru);
        Apply(nazoroidObj, useNazoroid);
    }

    void Apply(GameObject obj, bool active)
    {
        if (!obj) return;

        obj.SetActive(active);

        // カメラやGameManagerが Tag "Player" を探す設計に合わせる
        obj.tag = active ? playerTag : inactiveTag;

        // 念のため：PlayerInputも切る（active=falseでも残る事故対策）
        var pi = obj.GetComponentInChildren<PlayerInput>(true);
        if (pi) pi.enabled = active;
    }
}