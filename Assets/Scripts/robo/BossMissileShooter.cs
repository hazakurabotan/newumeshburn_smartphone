using System.Collections;
using UnityEngine;

public class BossMissileShooter : MonoBehaviour
{
    [Header("Missile Settings")]
    public GameObject missilePrefab;    // misairu1 プレハブ
    public Transform spawnPoint;        // 発射位置（未指定ならボスの位置）
    public float firstDelay = 2f;       // 最初の発射までの待ち時間
    public float interval = 4f;         // その後の発射間隔

    Coroutine shootRoutine;

    void OnEnable()
    {
        shootRoutine = StartCoroutine(ShootLoop());
    }

    void OnDisable()
    {
        if (shootRoutine != null)
            StopCoroutine(shootRoutine);
    }

    IEnumerator ShootLoop()
    {
        // 最初だけ少し待つ
        yield return new WaitForSeconds(firstDelay);

        while (true)
        {
            FireOnce();
            yield return new WaitForSeconds(interval);
        }
    }

    void FireOnce()
    {
        if (missilePrefab == null)
        {
            Debug.LogWarning("BossMissileShooter: missilePrefab が設定されていません。");
            return;
        }

        Vector3 pos = (spawnPoint != null) ? spawnPoint.position : transform.position;
        Instantiate(missilePrefab, pos, Quaternion.identity);
    }
}
