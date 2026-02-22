using UnityEngine;

public class BossMissileSpawner : MonoBehaviour
{
    [Header("ミサイル設定")]
    [SerializeField] private GameObject missilePrefab;      // misairu1 のプレハブ
    [SerializeField] private Transform warningLeft;         // MissileWarningLeft
    [SerializeField] private Transform warningRight;        // MissileWarningRight

    [SerializeField] private float firstDelay = 2f;         // 最初に撃つまでの待ち時間
    [SerializeField] private float interval = 3f;           // 連射間隔（秒）

    private float timer;

    private void Start()
    {
        timer = firstDelay;
    }

    private void Update()
    {
        // 参照が設定されていないときは何もしない
        if (missilePrefab == null || warningLeft == null || warningRight == null)
        {
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnMissile();
            timer = interval;
        }
    }

    private void SpawnMissile()
    {
        // 左右の警告マーカーの間からランダムなX座標を選ぶ
        float x = Random.Range(warningLeft.position.x, warningRight.position.x);
        float y = warningLeft.position.y;
        float z = warningLeft.position.z;

        Vector3 spawnPos = new Vector3(x, y, z);

        Instantiate(missilePrefab, spawnPos, Quaternion.identity);
    }
}
