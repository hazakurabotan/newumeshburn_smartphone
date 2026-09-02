using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class OpeningLine
{
    [TextArea(2, 4)] public string text;
    public Sprite background;
    public Sprite dialogPanel;

    [Header("任意のボイス")]
    public AudioClip voice;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public bool autoAdvanceOnVoiceEnd = false;
}

public class OpeningManagerSimple : MonoBehaviour
{
    [Header("UI参照")]
    public Image bgImage;
    public Image dialogPanelImage;
    public TMP_Text bodyText;

    [Header("ボイス再生")]
    public AudioSource voiceSource;

    [Header("会話データ")]
    public List<OpeningLine> lines = new List<OpeningLine>();

    [Header("遷移設定")]
    public string nextSceneName = "CharacterSelect";

    [Header("文字送り")]
    [Tooltip("ONなら本文を1文字ずつ表示します。")]
    public bool useTypewriter = true;

    [Tooltip("1文字を表示する間隔。小さいほど速いです。")]
    public float typewriterInterval = 0.04f;

    [Tooltip("句読点のあとだけ少し長く待ちたい場合の追加待ち時間。")]
    public float punctuationExtraWait = 0.12f;

    [Tooltip("ONなら、ボタンを押した時に表示中の文章を一気に全文表示します。")]
    public bool pressToCompleteCurrentLine = true;

    [Header("操作")]
    [Tooltip("ONなら、ジョイコン/ゲームパッドのSouthボタン短押しで進みます。")]
    public bool enableSouthButtonNext = true;

    [Tooltip("ONなら、ジョイコン/ゲームパッドのSouthボタン長押しでスキップします。")]
    public bool enableSouthButtonHoldSkip = true;

    [Tooltip("Southボタンを何秒押し続けたらスキップするか。")]
    public float southHoldSkipSeconds = 1.0f;

    [Tooltip("ONなら、Spaceキーで進みます。デバッグ用。")]
    public bool enableSpaceKeyNext = true;

    [Header("Debug")]
    public bool debugLog = true;

    int index = -1;
    bool waitingAutoAdvance = false;
    bool sceneLoading = false;

    bool isTyping = false;
    bool requestCompleteText = false;
    Coroutine typingCoroutine;
    Coroutine autoAdvanceCoroutine;

    bool southHolding = false;
    bool southSkipTriggered = false;
    float southHoldTimer = 0f;

    void Start()
    {
        if (!voiceSource)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f;
        }

        Next();
    }

    void Update()
    {
        if (sceneLoading) return;

        HandleSouthButton();
        HandleKeyboard();
    }

    void HandleSouthButton()
    {
        if (Gamepad.current == null) return;

        var south = Gamepad.current.buttonSouth;

        if (south.wasPressedThisFrame)
        {
            southHolding = true;
            southSkipTriggered = false;
            southHoldTimer = 0f;
        }

        if (southHolding && south.isPressed)
        {
            southHoldTimer += Time.unscaledDeltaTime;

            if (enableSouthButtonHoldSkip && !southSkipTriggered && southHoldTimer >= southHoldSkipSeconds)
            {
                southSkipTriggered = true;
                SkipOpening();
                return;
            }
        }

        if (southHolding && south.wasReleasedThisFrame)
        {
            bool wasShortPress = !southSkipTriggered && southHoldTimer < southHoldSkipSeconds;

            southHolding = false;
            southSkipTriggered = false;
            southHoldTimer = 0f;

            if (enableSouthButtonNext && wasShortPress)
            {
                AdvanceOrCompleteText();
            }
        }
    }

    void HandleKeyboard()
    {
        if (!enableSpaceKeyNext) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            AdvanceOrCompleteText();
        }
    }

    void AdvanceOrCompleteText()
    {
        if (sceneLoading) return;

        if (isTyping && pressToCompleteCurrentLine)
        {
            requestCompleteText = true;
            return;
        }

        Next();
    }

    void Next()
    {
        if (sceneLoading) return;

        if (voiceSource && voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }

        waitingAutoAdvance = false;

        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
        requestCompleteText = false;

        index++;

        if (index >= lines.Count)
        {
            LoadNextScene();
            return;
        }

        ShowLine(index);
    }

    void ShowLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= lines.Count)
            return;

        OpeningLine line = lines[lineIndex];

        if (bgImage)
            bgImage.sprite = line.background;

        if (dialogPanelImage)
            dialogPanelImage.sprite = line.dialogPanel;

        if (voiceSource && line.voice)
        {
            voiceSource.clip = line.voice;
            voiceSource.volume = line.voiceVolume;
            voiceSource.Play();
        }

        if (useTypewriter)
        {
            typingCoroutine = StartCoroutine(TypewriterRoutine(line));
        }
        else
        {
            if (bodyText)
                bodyText.text = line.text;

            if (line.autoAdvanceOnVoiceEnd)
            {
                waitingAutoAdvance = true;
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceWhenReady());
            }
        }
    }

    IEnumerator TypewriterRoutine(OpeningLine line)
    {
        isTyping = true;
        requestCompleteText = false;

        string fullText = line.text ?? "";

        if (bodyText)
            bodyText.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (requestCompleteText)
            {
                if (bodyText)
                    bodyText.text = fullText;

                break;
            }

            if (bodyText)
                bodyText.text = fullText.Substring(0, i + 1);

            float wait = typewriterInterval;

            char c = fullText[i];
            if (c == '。' || c == '、' || c == '！' || c == '？' || c == '!' || c == '?' || c == '…')
                wait += punctuationExtraWait;

            yield return new WaitForSecondsRealtime(wait);
        }

        if (bodyText)
            bodyText.text = fullText;

        isTyping = false;
        requestCompleteText = false;
        typingCoroutine = null;

        if (line.autoAdvanceOnVoiceEnd)
        {
            waitingAutoAdvance = true;
            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceWhenReady());
        }
    }

    IEnumerator AutoAdvanceWhenReady()
    {
        while (!sceneLoading && waitingAutoAdvance && isTyping)
            yield return null;

        while (!sceneLoading && waitingAutoAdvance && voiceSource && voiceSource.isPlaying)
            yield return null;

        if (!sceneLoading && waitingAutoAdvance)
        {
            waitingAutoAdvance = false;
            autoAdvanceCoroutine = null;
            Next();
        }
    }

    void SkipOpening()
    {
        if (sceneLoading) return;

        if (debugLog)
            Debug.Log("[OpeningManagerSimple] Southボタン長押しでOpeningをスキップします。");

        LoadNextScene();
    }

    void LoadNextScene()
    {
        if (sceneLoading) return;

        sceneLoading = true;
        waitingAutoAdvance = false;

        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (voiceSource)
            voiceSource.Stop();

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogWarning("[OpeningManagerSimple] nextSceneName が空です。遷移先シーン名を設定してください。");
            sceneLoading = false;
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}