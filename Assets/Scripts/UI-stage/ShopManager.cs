using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


// ショップ全体の流れを管理するスクリプト
public class ShopManager : MonoBehaviour
{
    public TypeWriterText twText;      // メッセージ表示用（タイプライター演出）
    public ShopSelector selector;      // 商品選択肢UI
    public GameObject confirmPanel;    // ハイ/イイエ確認パネル
    int currentItemIndex;              // 現在選択中の商品番号

    void Start()
    {
        // ショップの一連の流れをコルーチンで開始
        StartCoroutine(ShopFlow());
    }

    // ====== ショップの会話や選択肢の進行管理 ======
    IEnumerator ShopFlow()
    {
        twText.ShowText("ようこそ、いらっしゃい");
        yield return WaitZ(); // Zキー待ち

        twText.ShowText("今回はこれをしいれているよ");
        yield return WaitZ(); // Zキー待ち

        selector.StartSelect(); // 商品選択スタート
        yield return new WaitUntil(() => selector.selecting == false); // 選択が終わるまで待つ

        // ※決定時は ShowConfirm(selectedIndex) を呼ぶ想定
    }

    // ====== 「これを買いますか？」などの確認ウィンドウ表示 ======
    public void ShowConfirm(int idx)
    {
        currentItemIndex = idx;         // 選択された商品番号を記録
        confirmPanel.SetActive(true);   // 確認ウィンドウを表示
        // 「ハイ/イイエ」ボタンにイベントを割り当てるのが理想（UIイベントで設定するのもOK）
    }

    // ====== 「ハイ」ボタン押下時の処理 ======
    public void OnConfirmYes()
    {
        PlayerInventory.obtainedItems.Add(currentItemIndex);

        // ↓以下のように「BaseScene」だけ開くよう制御（安全策）
        if (SceneManager.GetActiveScene().name.Contains("Stage2"))
        {
            if (!GameManager.Instance.itemDisplayPanel.activeSelf)
                GameManager.Instance.itemDisplayPanel.SetActive(true);
        }

        ItemDisplayManager displayManager = FindObjectOfType<ItemDisplayManager>();
        if (displayManager != null)
        {
            displayManager.RefreshDisplay();
        }
        else
        {
            Debug.Log("Shopでは ItemDisplayManager が存在しないため表示スキップ");
        }

        confirmPanel.SetActive(false);

        // 購入完了後、会話を進めてBaseSceneに戻る
        StartCoroutine(EndTalk());
    }

    // ====== 「イイエ」ボタン押下時の処理 ======
    public void OnConfirmNo()
    {
        confirmPanel.SetActive(false);   // 確認ウィンドウを閉じる
        selector.selecting = true;       // 選択状態に戻す
    }

    // ====== 会話を終了して次のシーンへ遷移 ======
    IEnumerator EndTalk()
    {
        yield return WaitTextAndZ("それでいい？じゃあがんばって");
        SceneTransitionInfo.cameFromShop = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Stage2");
    }

    // ====== Zキーが押されるまで待機するコルーチン ======
    IEnumerator WaitZ()
    {
        while (!Input.GetKeyDown(KeyCode.Z))
            yield return null;
    }

    IEnumerator WaitTextAndZ(string text)
    {
        twText.ShowText(text);
        yield return new WaitUntil(() => twText.isCompleted); // 全文出るまで待つ
        yield return WaitZ(); // Zを押すまで待つ
    }

}
