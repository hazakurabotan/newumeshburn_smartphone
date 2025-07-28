using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// プレイヤーが触れるとON/OFFが切り替わるスイッチのスクリプト
// ON/OFFで見た目を切り替えたり、動く床(MovingBloc)を動かしたり止めたりします
public class SwitchAction : MonoBehaviour
{
    public GameObject targetMoveBlock;    // 動かしたい床（MovingBlocが付いてるオブジェクトを指定）
    public Sprite imageOn;                // ON時に表示する画像
    public Sprite imageOff;               // OFF時に表示する画像
    public bool on = false;               // スイッチの状態（ON=true, OFF=false）

    // ========== 最初に一度だけ呼ばれる ==========
    void Start()
    {
        // スイッチの状態で画像を切り替え
        if (on)
        {
            GetComponent<SpriteRenderer>().sprite = imageOn;
        }
        else
        {
            GetComponent<SpriteRenderer>().sprite = imageOff;
        }
    }

    // ========== 毎フレーム（今回は何もしていない） ==========
    void Update()
    {
        // 空欄でもOK（拡張したい場合に使う）
    }

    // ========== スイッチにプレイヤーが触れた時に呼ばれる ==========
    void OnTriggerEnter2D(Collider2D col)
    {
        // 触れた相手が「Player」タグなら
        if (col.gameObject.tag == "Player")
        {
            if (on)
            {
                // 既にONならOFFに切り替える
                on = false;
                GetComponent<SpriteRenderer>().sprite = imageOff;

                // 床(MovingBloc)を止める
                MovingBloc movingBloc = targetMoveBlock.GetComponent<MovingBloc>();
                movingBloc.Stop();
            }
            else
            {
                // OFFならONに切り替える
                on = true;
                GetComponent<SpriteRenderer>().sprite = imageOn;

                // 床(MovingBloc)を動かす
                MovingBloc movingBloc = targetMoveBlock.GetComponent<MovingBloc>();
                movingBloc.Move();
            }
        }
    }
}
