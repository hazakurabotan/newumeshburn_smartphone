using UnityEngine;

// ----------------------------------------------
// TrapPlatform
// プレイヤーが近づくと一定時間トゲ（spikeObject）が飛び出すギミック
// ----------------------------------------------
public class TrapPlatform : MonoBehaviour
{
    public GameObject spikeObject;      // 表示・消去するトゲ（子オブジェクト等で割当）
    public float spikeShowTime = 1.0f;  // トゲが出現している時間
    public float activateDistance = 1.5f; // 反応する距離（近づいたら発動）

    private bool isActive = false;      // トゲが今出ているか
    private float timer = 0f;           // 出現からの経過秒数
    private Transform player;           // プレイヤーのTransform参照

    void Start()
    {
        if (spikeObject != null)
            spikeObject.SetActive(false); // ゲーム開始時は非表示にする

        // プレイヤーのTransform取得
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return; // プレイヤーいなければ何もしない

        float dist = Vector2.Distance(player.position, transform.position); // 距離計算

        // プレイヤーが指定距離内に入ったらトゲを出現
        if (!isActive && dist <= activateDistance)
        {
            ActivateSpike();
        }

        // トゲが出ている間はタイマー計測、指定時間経過で収納
        if (isActive)
        {
            timer += Time.deltaTime;
            if (timer >= spikeShowTime)
            {
                DeactivateSpike();
            }
        }
    }

    // --- トゲを出現させる ---
    void ActivateSpike()
    {
        if (spikeObject != null)
            spikeObject.SetActive(true); // 表示ON
        isActive = true;
        timer = 0f;                     // タイマーリセット
    }

    // --- トゲを収納する ---
    void DeactivateSpike()
    {
        if (spikeObject != null)
            spikeObject.SetActive(false); // 表示OFF
        isActive = false;
        timer = 0f;
    }
}
