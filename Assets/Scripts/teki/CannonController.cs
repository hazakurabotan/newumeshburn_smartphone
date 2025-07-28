using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 砲台（キャノン）がプレイヤーに向かって弾を撃つスクリプト
public class CannonController : MonoBehaviour
{
    public GameObject objPrefab;     // 発射する弾のプレハブ（Inspectorで指定）
    public float delayTime = 3.0f;   // 弾を撃つまでの待機時間（秒単位）
    public float fireSpeed = 4.0f;   // 弾の発射速度（大きいほど速い）
    public float length = 0.0f;      // プレイヤーを感知する距離（範囲）

    GameObject player;               // プレイヤー本体（動的に取得）
    Transform gateTransform;         // 砲台の発射口の位置（"gate"という子オブジェクトを作成）
    float passedTimes = 0;           // 経過時間をカウントする変数

    // --- プレイヤーが砲台の感知範囲内にいるか判定する関数 ---
    bool CheckLength(Vector2 targetPos)
    {
        // 砲台の位置とプレイヤー位置の距離を計算
        float d = Vector2.Distance(transform.position, targetPos);

        // length以上離れていればfalse、範囲内ならtrue
        return (length >= d);
    }

    void Start()
    {
        // 発射口オブジェクト（"gate"という名前の子オブジェクト）を取得
        gateTransform = transform.Find("gate");

        // プレイヤー本体をタグで探して取得
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        // 毎フレーム経過時間を加算
        passedTimes += Time.deltaTime;

        // プレイヤーが感知範囲内にいる場合だけ発射判定
        if (CheckLength(player.transform.position))
        {
            // delayTime（待機時間）を越えたら弾を撃つ
            if (passedTimes > delayTime)
            {
                passedTimes = 0; // 経過時間をリセット

                // 弾を発射する位置（gateの位置）を取得
                Vector2 pos = gateTransform.position;

                // 弾のプレハブを生成（その場に新しい弾オブジェクトを作る）
                GameObject obj = Instantiate(objPrefab, pos, Quaternion.identity);

                // 発射方向を計算（Z軸の回転角度からX・Y方向ベクトルを作る）
                float angleZ = transform.localEulerAngles.z;
                float x = Mathf.Cos(angleZ * Mathf.Deg2Rad);
                float y = Mathf.Sin(angleZ * Mathf.Deg2Rad);
                Vector2 dir = new Vector2(x, y).normalized; // 単位ベクトルにして方向だけ取得

                // 弾に物理的な力（AddForce）を加えて飛ばす
                Rigidbody2D rbody = obj.GetComponent<Rigidbody2D>();
                if (rbody != null)
                {
                    // dir（方向）× fireSpeed（速度）で発射
                    rbody.AddForce(dir * fireSpeed, ForceMode2D.Impulse);
                }
            }
        }
    }

    // --- Unityエディタ上で感知範囲を可視化するための関数（選択中だけ）---
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; // 赤い線で
        Gizmos.DrawWireSphere(transform.position, length); // 砲台中心に半径lengthの円を描画
    }
}
