using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class OpeningLine
{
    [TextArea(2, 4)] public string text;      // 会話本文
    public Sprite background;                // opening06〜08
    public Sprite dialogPanel;               // 統合画像（名前・顔・枠）

    [Header("任意のボイス")]
    public AudioClip voice;                  // 付けたい台詞だけここに入れる
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public bool autoAdvanceOnVoiceEnd = false; // クリップ終了で自動送りしたい時だけON
}

public class OpeningManagerSimple : MonoBehaviour
{
    [Header("UI参照")]
    public Image bgImage;
    public Image dialogPanelImage;
    public TMP_Text bodyText;

    [Header("ボイス再生")]
    public AudioSource voiceSource;          // 2D、PlayOnAwake=OFF、Loop=OFF

    [Header("会話データ")]
    public List<OpeningLine> lines = new List<OpeningLine>();

    [Header("遷移設定")]
    public string nextSceneName = "CharacterSelect";

    int index = -1;
    bool waitingAutoAdvance = false;

    void Start()
    {
        // 安全策：インスペクタ未割り当てなら自動で作成
        if (!voiceSource)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f; // 2D
        }
        Next(); // 1行目表示
    }

    void Update()
    {
        // Southボタン or Space で送り
        bool nextPressed =
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

        if (nextPressed) Next();
    }

    void Next()
    {
        // まだ同じ行のボイスが鳴っていたら止めてから次へ
        if (voiceSource && voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }

        // 自動送り監視中なら解除
        waitingAutoAdvance = false;

        index++;
        if (index >= lines.Count)
        {
            // 遷移前にボイス停止
            if (voiceSource) voiceSource.Stop();
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        var L = lines[index];

        if (bgImage) bgImage.sprite = L.background;
        if (dialogPanelImage) dialogPanelImage.sprite = L.dialogPanel;
        if (bodyText) bodyText.text = L.text;

        // 必要な行だけボイス再生
        if (voiceSource && L.voice)
        {
            voiceSource.clip = L.voice;
            voiceSource.volume = L.voiceVolume;
            voiceSource.Play();

            if (L.autoAdvanceOnVoiceEnd && !waitingAutoAdvance)
            {
                waitingAutoAdvance = true;
                StartCoroutine(AutoAdvanceWhenVoiceEnds());
            }
        }
    }

    System.Collections.IEnumerator AutoAdvanceWhenVoiceEnds()
    {
        // クリップの再生終了まで待って次へ
        while (waitingAutoAdvance && voiceSource && voiceSource.isPlaying)
            yield return null;

        if (waitingAutoAdvance) // 途中でNextが押されてfalseになっていたら何もしない
            Next();
    }
}
