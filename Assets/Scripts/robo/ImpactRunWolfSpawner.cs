using System.Collections.Generic;
using UnityEngine;

public class ImpactRunWolfSpawner : MonoBehaviour
{
    [Header("References")]
    public ImpactRunWolfEnemy wolfPrefab;
    public Camera targetCamera;
    public Transform enemyParent;

    [Header("Spawn Position")]
    public float spawnY = -1.6f;
    public float randomSpawnY = 0.05f;
    public float spawnOffsetRight = 2.5f;

    [Header("Spawn Timing")]
    [Min(0.1f)] public float minSpawnInterval = 1.2f;
    [Min(0.1f)] public float maxSpawnInterval = 2.2f;
    [Min(1)] public int maxAlive = 3;

    [Header("Scene State")]
    public bool stopWhenRunFinished = true;

    private readonly List<ImpactRunWolfEnemy> alive = new List<ImpactRunWolfEnemy>();
    private float nextSpawnTime;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        ScheduleNextSpawn(true);
    }

    private void Update()
    {
        CleanupNulls();

        if (wolfPrefab == null || targetCamera == null)
            return;

        if (stopWhenRunFinished && ImpactRunGameManager.Instance != null && ImpactRunGameManager.Instance.IsFinished)
            return;

        if (alive.Count >= maxAlive)
            return;

        if (Time.time < nextSpawnTime)
            return;

        SpawnWolf();
        ScheduleNextSpawn(false);
    }

    private void SpawnWolf()
    {
        float halfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        float x = targetCamera.transform.position.x + halfWidth + spawnOffsetRight;
        float y = spawnY + Random.Range(-randomSpawnY, randomSpawnY);

        ImpactRunWolfEnemy enemy = Instantiate(
            wolfPrefab,
            new Vector3(x, y, 0f),
            Quaternion.identity,
            enemyParent
        );

        enemy.targetCamera = targetCamera;
        alive.Add(enemy);
    }

    private void CleanupNulls()
    {
        for (int i = alive.Count - 1; i >= 0; i--)
        {
            if (alive[i] == null)
                alive.RemoveAt(i);
        }
    }

    private void ScheduleNextSpawn(bool first)
    {
        float delay = first
            ? Random.Range(0.25f, 0.6f)
            : Random.Range(minSpawnInterval, maxSpawnInterval);

        nextSpawnTime = Time.time + delay;
    }
}