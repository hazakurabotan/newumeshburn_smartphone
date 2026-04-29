using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// ====================
// 1行ぶんの会話データ
// ====================
[System.Serializable]
public class DialogLine
{
    public string speakerName;
    public Sprite speakerIcon;
    [TextArea(2, 5)]
    public string text;
    public AudioClip voice; // 🎤 ← 追加：再生したい音声（任意）
}

// ====================
// 会話ウィンドウ管理
// ====================
public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance;

    private PlayerInput playerInput;
    private InputAction nextAction;
    public InputActionReference advanceAction;

    public GameObject dialogPanel;
    public TextMeshProUGUI dialogText;
    public TextMeshProUGUI nameText;
    public Image iconImage;
    public AudioSource voiceSource; // 🎤 ← 追加：ボイス再生用

    public TimeController timeController;
    public GameObject BossPanel;
    public TextMeshProUGUI bossHPText;

    public DialogLine[] dialogLines;
    int currentSentence = 0;
    bool isTalking = false;

    void Awake()
    {
        Instance = this;
        playerInput = FindObjectOfType<PlayerInput>();
    }

    void Start()
    {
        dialogPanel.SetActive(false);
        if (BossPanel == null) BossPanel = GameObject.Find("BossPanel");
        if (timeController == null) timeController = FindObjectOfType<TimeController>();
    }

    void Update()
    {
        if (isTalking && Input.GetKeyDown(KeyCode.Z))
        {
            NextSentence();
        }
    }

    // ===== 通常の会話開始 =====
    public void StartDialog(DialogLine[] lines)
    {
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            var pc = player.GetComponent<PlayerController>();
            if (rb != null) rb.velocity = Vector2.zero;
            if (pc != null) pc.enabled = false;
        }

        if (timeController != null)
            timeController.enabled = false;

        dialogLines = lines;
        currentSentence = 0;
        dialogPanel.SetActive(true);
        isTalking = true;
        ShowSentence();
    }

    // ===== セリフ表示 =====
    void ShowSentence()
    {
        if (currentSentence < dialogLines.Length)
        {
            var line = dialogLines[currentSentence];
            dialogText.text = line.text;
            nameText.text = line.speakerName;
            iconImage.sprite = line.speakerIcon;
            iconImage.enabled = (line.speakerIcon != null);

            // 🎤 ボイスが設定されてたら再生
            if (voiceSource && line.voice)
                voiceSource.PlayOneShot(line.voice);
        }
        else
        {
            EndDialog();
        }
    }

    // ===== 次へ =====
    void NextSentence()
    {
        currentSentence++;
        ShowSentence();
    }

    // ===== 会話終了 =====
    void EndDialog()
    {
        dialogPanel.SetActive(false);
        isTalking = false;

        if (timeController != null)
            timeController.enabled = true;

        if (BossPanel != null)
            BossPanel.SetActive(true);

        var boss = FindObjectOfType<BossSimpleJump>();
        if (boss != null)
            boss.isActive = true;

        var player = FindObjectOfType<PlayerController>();
        if (player != null)
            player.enabled = true;
    }

    // ===== オープニングなどで使う音声付きシーケンス再生 =====
    public void PlaySequence(DialogLine[] lines, Action onFinished = null)
    {
        StartCoroutine(CoPlaySequence(lines, onFinished));
    }

    IEnumerator CoPlaySequence(DialogLine[] lines, Action onFinished)
    {
        dialogPanel.SetActive(true);

        foreach (var line in lines)
        {
            dialogText.text = line.text;
            nameText.text = line.speakerName;
            iconImage.sprite = line.speakerIcon;
            iconImage.enabled = (line.speakerIcon != null);

            if (voiceSource && line.voice)
                voiceSource.PlayOneShot(line.voice);

            // SouthボタンまたはZキーで進む
            yield return new WaitUntil(() =>
                Input.GetKeyDown(KeyCode.Z) ||
                Input.GetKeyDown(KeyCode.Space)
            );
        }

        dialogPanel.SetActive(false);
        onFinished?.Invoke();
    }
}
