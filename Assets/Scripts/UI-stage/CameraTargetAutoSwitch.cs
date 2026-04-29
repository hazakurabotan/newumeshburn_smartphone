using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CameraTargetAutoSwitch : MonoBehaviour
{
    [Header("Refs")]
    public FollowTarget2D_Bounds follow;

    [Header("Fallback search")]
    public float retryInterval = 0.2f;
    float nextRetryTime = 0f;

    void Awake()
    {
        if (!follow) follow = GetComponent<FollowTarget2D_Bounds>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        nextRetryTime = 0f;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        // シーン切替直後は生成順がずれるので少し猶予
        nextRetryTime = 0f;
    }

    // ★Updateで先にtargetを決める（FollowTarget2D_BoundsのLateUpdateより前）
    void Update()
    {
        if (!follow) return;

        var t = ResolveTarget();

        if (t != null)
        {
            // ★毎フレーム「正しいターゲット」に上書き（mawaru13固定を潰す）
            if (follow.target != t) follow.target = t;
            return;
        }

        // 見つからない場合はたまに再トライ（無駄なFind連打を避ける）
        if (Time.unscaledTime >= nextRetryTime)
        {
            nextRetryTime = Time.unscaledTime + retryInterval;
        }
    }

    Transform ResolveTarget()
    {
        // 最優先：Tag "Player"（あなたのNazoroid/操作キャラがこれになってる想定）
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null && go.activeInHierarchy) return go.transform;

        // 次点：有効なPlayerInput（ただし Canvas みたいなのを避けたいのでフィルタ）
        foreach (var pi in Object.FindObjectsOfType<PlayerInput>(true))
        {
            if (pi == null || !pi.enabled || !pi.gameObject.activeInHierarchy) continue;

            // PlayerタグならOK
            if (pi.CompareTag("Player")) return pi.transform;

            // “キャラっぽい”物だけを拾う（Canvasの誤検出を回避）
            if (pi.GetComponent<Rigidbody2D>() != null) return pi.transform;
        }

        return null;
    }
}