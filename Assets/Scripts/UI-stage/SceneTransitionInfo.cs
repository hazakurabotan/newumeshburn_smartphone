using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// シーン間で共通の一時情報をやりとりするためのクラス
// （例：どのシーンから来たか、など）
public static class SceneTransitionInfo
{
    // ショップから来たかどうかを記録するフラグ
    // 例：ショップで購入後にtrueにしておくと、戻った先で特別な演出などに使える
    public static bool cameFromShop = false;
}
