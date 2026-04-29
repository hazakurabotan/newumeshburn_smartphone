using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button meguruButton;
    public Button nazoroidButton;

    [Header("Voices")]
    public AudioClip meguruVoice;   // 水樹 澪
    public AudioClip nazoroidVoice; // ティニー

    [Header("Fallback Next Scene (Stage未選択の保険)")]
    public string fallbackNextSceneName = "Stage01";

    AudioSource audioSrc;
    bool locked;

    void Awake()
    {
        audioSrc = GetComponent<AudioSource>();

        // 念のため null チェック（割り当て忘れ事故防止）
        if (meguruButton != null)
            meguruButton.onClick.AddListener(() => Choose(CharacterId.Meguru));

        if (nazoroidButton != null)
            nazoroidButton.onClick.AddListener(() => Choose(CharacterId.Nazoroid));
    }

    public enum CharacterId { Meguru, Nazoroid }

    public void Choose(CharacterId id)
    {
        if (locked) return;
        locked = true;

        // 選択キャラ保持（既存の仕組みをそのまま使う）
        SelectedCharacter.Id = id;

        AudioClip clip = (id == CharacterId.Meguru) ? meguruVoice : nazoroidVoice;
        float wait = 0.1f;

        if (clip != null && audioSrc != null)
        {
            audioSrc.Stop();
            audioSrc.PlayOneShot(clip);
            wait = clip.length;
        }

        // 入力ロック（連打防止）
        if (meguruButton != null) meguruButton.interactable = false;
        if (nazoroidButton != null) nazoroidButton.interactable = false;

        StartCoroutine(LoadAfter(wait));
    }

    System.Collections.IEnumerator LoadAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        // ★ここが重要：StageSelectで選ばれたステージがあればそれへ
        string stage = SelectedStage.SceneName;
        if (string.IsNullOrEmpty(stage)) stage = fallbackNextSceneName;

        SceneManager.LoadScene(stage);
    }
}

// 既存の「選択キャラ保持」入れ物
public static class SelectedCharacter
{
    public static CharacterSelectManager.CharacterId Id =
        CharacterSelectManager.CharacterId.Meguru;
}