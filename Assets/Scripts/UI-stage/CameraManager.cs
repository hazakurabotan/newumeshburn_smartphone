using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// カメラの追従や移動制限を管理するクラス
public class CameraManager : MonoBehaviour
{
    // === カメラが移動できる範囲（XとYの最小・最大値） ===
    public float leftLimit = -10f;     // カメラが行ける最も左の位置
    public float rightLimit = 20f;     // カメラが行ける最も右の位置
    public float topLimit = 5f;        // カメラが行ける一番上の位置
    public float bottomLimit = -5f;    // カメラが行ける一番下の位置
    // ※ float値の末尾「f」は省略するとエラーになるので忘れずにつける！

    // サブスクリーン用オブジェクト（例：ミニマップや演出用画面）
    public GameObject subScreen;

    void Update()
    {
        // 毎フレーム、タグ "Player" のオブジェクトを探す
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // プレイヤーが見つかった時だけ追従処理を行う
        if (player != null)
        {
            // プレイヤーの現在位置（x, y）を取得
            float x = player.transform.position.x;
            float y = player.transform.position.y;
            float z = transform.position.z; // カメラのZ軸は基本的に固定（2Dゲームなら特に重要）

            // === カメラの移動範囲を制限する ===
            // Clampで指定範囲内にx, yを収める（それ以上は動かない）
            x = Mathf.Clamp(x, leftLimit, rightLimit);
            y = Mathf.Clamp(y, bottomLimit, topLimit);

            // カメラ本体の位置を更新（プレイヤーの方に追従）
            transform.position = new Vector3(x, y, z);

            // === サブスクリーン（ミニマップ等）もカメラ位置に連動させる例 ===
            if (subScreen != null)
            {
                // メインカメラの現在位置を取得
                Vector3 camPos = Camera.main.transform.position;

                // サブスクリーンの表示オフセット位置（例：右下に出したい時の値）
                float offsetX = 3.5f;
                float offsetY = -2.5f;

                // サブスクリーンの位置を更新（z座標は変更しない）
                subScreen.transform.position = new Vector3(
                    camPos.x + offsetX,
                    camPos.y + offsetY,
                    subScreen.transform.position.z
                );
            }
        }
        else
        {
            // プレイヤーが見つからない場合の警告メッセージ
            Debug.LogWarning("Player が見つかりませんでした。タグ 'Player' を確認してください。");
        }
    }
}
