using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 弾やシェルの自動削除を管理するスクリプト
public class ShellController : MonoBehaviour
{
    public float deleteTime = 3.0f; // 何秒後に自動で消すか

    // ====== 最初に1回だけ呼ばれる ======
    void Start()
    {
        // deleteTime秒後にこのオブジェクトを自動削除
        Destroy(gameObject, deleteTime);
    }

    // ====== 毎フレーム呼ばれる（今回は使っていません）======
    void Update()
    {
        // 処理なし
    }

    // ====== 何かとぶつかった時（Trigger同士の判定） ======
    void OnTriggerEnter2D(Collider2D collision)
    {
        // 何かに接触したらすぐ消す
        Destroy(gameObject);
    }
}
