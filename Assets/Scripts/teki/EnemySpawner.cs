using UnityEngine;

// ---------------------------------------------------------
// EnemySpawner
// 敵キャラクターを出現させ、やられたら一定時間後に再出現させるスクリプト
// ---------------------------------------------------------
public class EnemySpawner : MonoBehaviour
{
    // 出現させる敵キャラクター（プレハブ）の参照
    public GameObject enemyPrefab;

    // 敵が出現する場所（TransformをInspectorで指定）
    public Transform spawnPoint;

    // 敵が倒されてから再び出現するまでの待ち時間（秒）
    public float respawnDelay = 2.0f;

    // 現在出現している敵の参照（消えたかどうかをチェックする用）
    private GameObject currentEnemy;

    // 今リスポーン待ちかどうか（重複リスポーンを防ぐためのフラグ）
    private bool respawning = false;

    // ゲーム開始時に一度だけ呼ばれる
    void Start()
    {
        SpawnEnemy(); // 最初の敵を出現させる
    }

    // 毎フレーム呼ばれる
    void Update()
    {
        // currentEnemyがnull（敵がいない）で、かつリスポーン中でなければ…
        if (currentEnemy == null && !respawning)
        {
            // 待ち時間の後で敵を出す処理を予約（コルーチン）
            StartCoroutine(RespawnAfterDelay());
        }
    }

    // 敵を出現させる関数
    void SpawnEnemy()
    {
        if (spawnPoint != null)
        {
            // enemyPrefabをspawnPointの位置に生成し、参照を保存
            currentEnemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            // スポーン場所が指定されていないとき警告を出す
           // Debug.LogWarning("スポーンポイントが設定されていません！");
        }
    }

    // 一定時間待ってから敵を出現させるコルーチン
    System.Collections.IEnumerator RespawnAfterDelay()
    {
        respawning = true; // リスポーン中フラグを立てる
        yield return new WaitForSeconds(respawnDelay); // 指定秒数待つ
        SpawnEnemy(); // 敵を出現させる
        respawning = false; // フラグを戻す
    }
}
