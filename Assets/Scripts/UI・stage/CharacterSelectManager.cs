using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Next Scene")]
    public string nextSceneName = "Stage01";

    AudioSource audioSrc;
    bool locked;

    void Awake()
    {
        audioSrc = GetComponent<AudioSource>();
        // クリックでもSubmitでも同じ処理に寄せる
        meguruButton.onClick.AddListener(() => Choose(CharacterId.Meguru));
        nazoroidButton.onClick.AddListener(() => Choose(CharacterId.Nazoroid));
    }

    public enum CharacterId { Meguru, Nazoroid }

    public void Choose(CharacterId id)
    {
        if (locked) return;
        locked = true;

        // ゲーム側で参照したい場合は保持
        SelectedCharacter.Id = id; // staticクラスは下に定義

        AudioClip clip = (id == CharacterId.Meguru) ? meguruVoice : nazoroidVoice;
        float wait = 0.1f;

        if (clip != null && audioSrc != null)
        {
            audioSrc.Stop();
            audioSrc.PlayOneShot(clip);
            wait = clip.length;
        }

        // 入力をロック（連打防止）
        meguruButton.interactable = false;
        nazoroidButton.interactable = false;

        StartCoroutine(LoadAfter(wait));
    }

    System.Collections.IEnumerator LoadAfter(float delay)
    {
        // ボイスを聞かせたいので、タイムスケールは触らない
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(nextSceneName);
    }
}

// 簡易に選択キャラを保持するだけの入れ物
public static class SelectedCharacter
{
    public static CharacterSelectManager.CharacterId Id =
        CharacterSelectManager.CharacterId.Meguru;
}
