using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemData : MonoBehaviour
{
    public int value = 10; // このアイテムが持っているスコアの値

    void Start()
    {
        // 特に初期化処理はなし（将来的にSE再生など入れてもOK）
    }

    void Update()
    {
        // 毎フレーム処理は今は不要（将来的に回転やエフェクト処理など追加可能）
    }
}
