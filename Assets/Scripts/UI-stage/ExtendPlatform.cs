using UnityEngine;

// ------------------------------------------------------------
// ExtendPlatform
// プレイヤーが近づくと自動で横に伸びる足場を実現するスクリプト
// ------------------------------------------------------------
public class ExtendPlatform : MonoBehaviour
{
    // 近くに来たら反応する対象（プレイヤー。Inspectorでセット）
    public Transform player;

    // どのくらい近づいたら伸びる判定になるか（距離）
    public float triggerDistance = 2.5f;

    // どのくらい横に伸びるか（距離）
    public float extendDistance = 2.0f;

    // 足場がどれくらいの速さで伸びるか
    public float extendSpeed = 3.0f;

    // 足場の初期位置（伸び始める前の位置）
    private Vector3 originalPos;

    // もう足場を伸ばしたかどうか
    private bool isExtended = false;

    // ゲーム開始時に一度だけ実行
    void Start()
    {
        // 今の位置を保存（これを基準に伸ばす）
        originalPos = transform.position;
    }

    // 毎フレーム呼ばれる
    void Update()
    {
        // プレイヤーが指定されていなければ何もしない
        if (player == null) return;

        // プレイヤーとの距離を計算
        float dist = Vector2.Distance(player.position, transform.position);

        // まだ伸びてなくて、プレイヤーが近づいたら
        if (!isExtended && dist < triggerDistance)
        {
            isExtended = true; // 伸び開始
        }

        // 足場を伸ばすアニメーション
        if (isExtended)
        {
            // 目標位置（originalPosから右へextendDistanceぶん移動）
            Vector3 target = originalPos + new Vector3(extendDistance, 0, 0);
            // 現在位置から目標位置へ、毎フレーム少しずつ近づける（スムーズに伸ばす）
            transform.position = Vector3.MoveTowards(transform.position, target, extendSpeed * Time.deltaTime);
        }
    }
}
