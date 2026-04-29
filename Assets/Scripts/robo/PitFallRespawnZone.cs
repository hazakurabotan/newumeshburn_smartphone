using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PitFallRespawnZone : MonoBehaviour
{
    [Header("Damage")]
    [Min(1)] public int fallDamage = 10;

    [Header("Respawn")]
    [Tooltip("落ちたX位置からどれだけ左へ戻すか")]
    [Min(0f)] public float respawnBackOffsetX = 1.2f;

    [Tooltip("リスポーン後のY位置")]
    public float respawnY = -1.2f;

    [Tooltip("指定した場合はこちらを優先して復活地点に使う")]
    public Transform respawnPoint;

    [Header("Pusher Wall")]
    public ImpactRunPusherWall pusherWall;
    [Min(0f)] public float pusherPauseSeconds = 0.35f;
    [Min(0f)] public float pusherExtraBack = 0.5f;


    [Header("Timing")]
    [Min(0f)] public float respawnDelay = 0.05f;

    private readonly HashSet<ImpactRunnerController> processing = new HashSet<ImpactRunnerController>();

    private void Reset()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void Awake()
    {
        if (pusherWall == null)
            pusherWall = FindObjectOfType<ImpactRunPusherWall>();

        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ImpactRunnerController runner = other.GetComponentInParent<ImpactRunnerController>();
        if (runner == null)
            return;

        if (processing.Contains(runner))
            return;

        StartCoroutine(HandleFallRoutine(runner));
    }

    private IEnumerator HandleFallRoutine(ImpactRunnerController runner)
    {
        processing.Add(runner);

        Vector3 fallPos = runner.transform.position;

        if (pusherWall != null)
        {
            pusherWall.PauseWall(respawnDelay + pusherPauseSeconds + 0.05f);
        }

        if (runner.CurrentHP <= fallDamage)
        {
            runner.TakeDamage(fallDamage, fallPos + Vector3.down);
            yield return new WaitForSeconds(0.1f);
            processing.Remove(runner);
            yield break;
        }

        runner.TakeDamage(fallDamage, fallPos + Vector3.down);

        if (respawnDelay > 0f)
            yield return new WaitForSeconds(respawnDelay);
        else
            yield return null;

        Vector3 respawnPos;
        if (respawnPoint != null)
        {
            respawnPos = respawnPoint.position;
        }
        else
        {
            respawnPos = new Vector3(
                fallPos.x - respawnBackOffsetX,
                respawnY,
                runner.transform.position.z
            );
        }

        Rigidbody2D rb = runner.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = respawnPos;
        }
        else
        {
            runner.transform.position = respawnPos;
        }

        if (pusherWall != null)
        {
            pusherWall.PauseAndRewindBehindX(respawnPos.x, pusherPauseSeconds, pusherExtraBack);
        }

        yield return new WaitForSeconds(0.1f);
        processing.Remove(runner);
    }
}