using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ----------------------------------------------
// SaveController
// シーンをまたいでも消えない「データ保持オブジェクト」
// 例：イベント消費状況などをHashSetで管理
// ----------------------------------------------
public class SaveController : MonoBehaviour
{
    // --- シングルトン化（どのシーンでも1つだけ存在する仕組み）---
    public static SaveController instance;

    // --- （例）「タグ＋ID」で一度消費したイベントを記録するセット ---
    public HashSet<(string tag, int arrangeId)>
        consumedEvent = new HashSet<(string tag, int arrangeId)>();

    private void Awake()
    {
        // まだ唯一のインスタンスが無い場合、自分を「唯一の」instanceとして保存
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // シーン切り替えても消えない
        }
        else
        {
            // すでに他に1個ある場合は、自分は破棄（重複防止）
            Destroy(gameObject);
        }
    }

    // --- 例：イベント消費を記録 ---
    void ConsumedEvent(string tag, int arrangeId)
    {
        consumedEvent.Add((tag, arrangeId));
    }

    // --- 例：イベントがすでに消費済みか調べる ---
    bool IsConsumed(string tag, int arrangeId)
    {
        return consumedEvent.Contains((tag, arrangeId));
    }

    // Start, Update は現状使っていない
}
