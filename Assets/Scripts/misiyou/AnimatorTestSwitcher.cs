using UnityEngine;

// -----------------------------------------------
// AnimatorTestSwitcher
// Rキーを押すと、AnimatorOverrideController（アニメーション上書き用）を切り替えて
// 指定したアニメーションを頭から再生するテスト用スクリプトです
// -----------------------------------------------
public class AnimatorTestSwitcher : MonoBehaviour
{
    // Animator型の変数。キャラやオブジェクトのAnimator（アニメーター）を格納する
    // publicにしない＝Inspector上には出ないが、スクリプト内で使える
    private Animator animator;

    // Inspectorで設定できるAnimatorOverrideController
    // 切り替えたい「上書き用アニメーションコントローラ」を指定する
    //Inspectorでアニメーター上書き用を割り当て可能
    public AnimatorOverrideController revivedOverrideController;

    // ゲーム開始時（Awake）はじめに一度だけ実行される関数
    void Awake()
    {
        // このオブジェクトについているAnimatorコンポーネントを取得
        animator = GetComponent<Animator>();
    }

    // 毎フレーム呼ばれる関数
    void Update()
    {
        // Rキーを押した瞬間に処理を行う（毎フレーム判定される）
        if (Input.GetKeyDown(KeyCode.R))
        {
            // 一度アニメーションコントローラをnull（何もなし）にしてから…
            // 一度「なし」にしてからセットし直すことで、しっかり切り替わる
            animator.runtimeAnimatorController = null;
            // 新しいAnimatorOverrideControllerをセット
            //レイヤー0で、"RePlayerStop"というアニメーションを頭から再生
            animator.runtimeAnimatorController = revivedOverrideController;

            // "RePlayerStop"という名前のアニメーションを、レイヤー0で先頭から再生
            animator.Play("RePlayerStop", 0, 0);

            // デバッグ用ログ（UnityのConsoleに表示）
            Debug.Log("切り替えテスト発動！");
        }
    }
}
