using UnityEngine;

public class LogWhenDisabled : MonoBehaviour
{
    void OnDisable()
    {
        Debug.LogWarning($"{name} が無効化されました (OnDisable)。", this);
        Debug.Log(System.Environment.StackTrace); // 呼び出し経路ヒント
    }
}
