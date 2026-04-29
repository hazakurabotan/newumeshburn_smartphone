using UnityEngine;

// プレイヤーの出現位置を管理するスクリプト
// 通常はdefaultSpawn、ショップから戻ったときはshopSpawnPointにワープ
public class PlayerSpawnManager : MonoBehaviour
{
    public Transform defaultSpawn;      // 通常の出現位置（Inspectorで設定）
    public Transform shopSpawnPoint;    // ショップから戻ったときの出現位置

    void Start()
    {
        // 「Player」タグのついたオブジェクトを探す
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // ショップから戻ってきたフラグがONで、shopSpawnPointが指定されていればそちらにワープ
            if (SceneTransitionInfo.cameFromShop && shopSpawnPoint != null)
            {
                player.transform.position = shopSpawnPoint.position;
                SceneTransitionInfo.cameFromShop = false; // 一度使ったらリセット（戻った後は通常に戻す）
            }
            // それ以外（最初の開始など）はdefaultSpawnに配置
            else if (defaultSpawn != null)
            {
                player.transform.position = defaultSpawn.position;
            }
        }
    }
}
