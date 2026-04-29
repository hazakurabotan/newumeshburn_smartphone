using UnityEngine;
using TMPro;
using System.Collections;

// 文字を1文字ずつ表示するタイプライター風のテキスト表示スクリプト
public class TypeWriterText : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public float typeSpeed = 0.03f;

    private string fullText;
    private bool isTyping = false;
    private bool skip = false;

    public bool isCompleted { get; private set; } // ←全文表示済みかどうか

    public void ShowText(string text)
    {
        StopAllCoroutines();
        fullText = text;
        StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        isTyping = true;
        isCompleted = false;   // 追加: 開始時はまだ未完
        messageText.text = "";
        foreach (char c in fullText)
        {
            messageText.text += c;
            if (skip) break;
            yield return new WaitForSeconds(typeSpeed);
        }
        messageText.text = fullText;
        isTyping = false;
        isCompleted = true;    // 追加: 全文表示したらtrue
        skip = false;
    }

    void Update()
    {
        if (isTyping && Input.GetKeyDown(KeyCode.Z))
        {
            skip = true;
        }
    }
}