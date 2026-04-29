using UnityEngine;

// ロックマン風カメラ：プレイヤーが画面端を越えたら1画面分カメラが移動するスクリプト
public class MegaManCamera : MonoBehaviour
{
    public Transform player;           // 追従するプレイヤー（Inspectorで指定 or 自動取得）
    public float screenWidth = 16f;    // 1画面分の横幅（Unityユニットで指定、例:16）
    public float screenHeight = 9f;    // 1画面分の高さ
    public float yOffset = 0f;         // 画面全体を上下にオフセットしたい場合に調整

    private Vector2 currentScreenOrigin; // 現在の画面の左下座標（区画の原点）

    void Start()
    {
        // プレイヤーの参照が無ければタグで自動取得
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        // ゲーム開始時に現在の画面区画を計算
        UpdateScreenOrigin();
    }

    void Update()
    {
        Vector2 playerPos = player.position;

        // -------- 横方向の区画移動チェック --------
        // プレイヤーが左端を出た
        if (playerPos.x < currentScreenOrigin.x)
        {
            currentScreenOrigin.x -= screenWidth; // 1画面左へ
            MoveCamera();
        }
        // プレイヤーが右端を出た
        else if (playerPos.x > currentScreenOrigin.x + screenWidth)
        {
            currentScreenOrigin.x += screenWidth; // 1画面右へ
            MoveCamera();
        }
        //
        // if (playerPos.y > currentScreenOrigin.y + screenHeight) {
        //     currentScreenOrigin.y += screenHeight;
        //     MoveCamera();
        // }


        // -------- 縦方向の区画移動（必要なら追加）--------
        // プレイヤーが下端を出た
        // if (playerPos.y < currentScreenOrigin.y) {
        //     currentScreenOrigin.y -= screenHeight;
        //     MoveCamera();
        // }
        // プレイヤーが上端を出た
        // if (playerPos.y > currentScreenOrigin.y + screenHeight) {
        //     currentScreenOrigin.y += screenHeight;
        //     MoveCamera();
        // }
    }

    // カメラ位置を現在の画面区画の中央に移動
    void MoveCamera()
    {
        float camX = currentScreenOrigin.x + screenWidth / 2f;
        float camY = currentScreenOrigin.y + screenHeight / 2f + yOffset; // y方向オフセットも加算
        transform.position = new Vector3(camX, camY, transform.position.z);
    }

    // プレイヤーが今いる画面区画を計算し、区画原点を更新
    void UpdateScreenOrigin()
    {
        // 例：player.x=17, screenWidth=16なら→区画原点=16
        float originX = Mathf.Floor(player.position.x / screenWidth) * screenWidth;
        float originY = Mathf.Floor(player.position.y / screenHeight) * screenHeight;
        currentScreenOrigin = new Vector2(originX, originY);

        MoveCamera(); // 初期位置にカメラ移動
    }
}
