using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class DialogSequenceManager : MonoBehaviour
{
    public static DialogSequenceManager Instance;

    [Header("UI refs")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI bodyText;
    public UnityEngine.UI.Image portraitImage;
    public AudioSource voiceSource; // 任意。無ければ音は鳴らさない

    void Awake() => Instance = this;

    // South（決定）で進める簡易版
    public void PlaySequence(DialogueLine[] lines, Action onFinished)
    {
        StartCoroutine(Co(lines, onFinished));
    }

    IEnumerator Co(DialogueLine[] lines, Action onFinished)
    {
        if (lines == null || lines.Length == 0)
        {
            onFinished?.Invoke();
            yield break;
        }

        foreach (var line in lines)
        {
            // UI反映（null安全に）
            if (nameText) nameText.text = line?.speakerName ?? "";
            if (bodyText) bodyText.text = line?.text ?? "";
            if (portraitImage) portraitImage.sprite = line?.portrait;

            // ボイス（あれば）
            if (voiceSource && line?.voice)
            {
                voiceSource.Stop();
                voiceSource.PlayOneShot(line.voice);
            }

            // Southボタン待ち（新InputSystem。なければSpaceにフォールバック）
            yield return new WaitUntil(() =>
                Gamepad.current != null
                    ? (Gamepad.current.buttonSouth.wasPressedThisFrame)
                    : Input.GetKeyDown(KeyCode.Space)
            );
        }

        onFinished?.Invoke();
    }
}
