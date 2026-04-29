using UnityEngine;

public class StageNode : MonoBehaviour
{
    [Header("この地点で決定したときに入るScene名")]
    public string stageSceneName; // 例: "Stage11" / "HowToPlay" / "Town"

    [Header("キャラ選択を挟む？（true: StageSelect→CharacterSelect→このScene）")]
    public bool useCharacterSelect = true;

    [Header("上下左右の接続（行ける方向だけ入れる）")]
    public StageNode up;
    public StageNode down;
    public StageNode left;
    public StageNode right;

    [Header("未解放なら false（移動/決定を止められる）")]
    public bool unlocked = true;
}