using UnityEngine;

// ボス戦イベント用トリガースクリプト
// プレイヤーがこのトリガーに触れると、イベント会話を開始してボス行動を一時停止させます
public class BossTrigger : MonoBehaviour
{
    public BossSimpleJump boss;               // 対象となるボスのスクリプト（Inspectorでアサイン）
    public DialogManager dialogManager;       // 会話管理スクリプト（Inspectorでアサイン）
    public DialogLine[] dialogLines;          // イベントで再生するセリフ一覧（Inspectorで登録）



    bool hasTriggered = false;                // 既に発動済みかどうかのフラグ（2重発動防止）

    // 他のコライダーがこのオブジェクトに入ったときに呼ばれる
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            // ★ここでHierarchy中のDialogManager全部出す
            Debug.Log("【BossTrigger】トリガー発動時に存在するDialogManager↓");
            foreach (var dm in FindObjectsOfType<DialogManager>())
            {
                Debug.Log("DialogManager: " + dm.gameObject.name);
            }

            var allDialogManagers = FindObjectsOfType<DialogManager>();
            Debug.Log($"【DialogManagerチェック】FindObjectsOfType<DialogManager>().Length = {allDialogManagers.Length}");
            foreach (var dm in allDialogManagers)
            {
                Debug.Log($"DialogManager名: {dm.name} / 親: {dm.transform.parent?.name}");
            }


            boss.isActive = false;
            FindObjectOfType<PlayerController>().enabled = false;

            // ここで毎回DialogManagerを取得しなおす
            DialogManager manager = FindObjectOfType<DialogManager>();
            if (manager != null)
            {
                manager.StartDialog(dialogLines);
            }
            else
            {
                Debug.LogWarning("DialogManagerが見つかりませんでした！");
            }

            Destroy(gameObject);
        }
    }
}
