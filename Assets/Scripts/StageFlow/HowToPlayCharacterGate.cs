using UnityEngine;
using UnityEngine.InputSystem;

public class HowToPlayCharacterGate : MonoBehaviour
{
    public string playerPrefsKey = "SelectedChara"; // 0=Player 1=Mawaru

    [Header("Scene refs")]
    public GameObject playerObj;
    public GameObject mawaruObj;
    public Transform spawnPoint;

    void Start()
    {
        int sel = PlayerPrefs.GetInt(playerPrefsKey, 0);
        bool useMawaru = (sel == 1);

        SetActiveCharacter(useMawaru);
    }

    void SetActiveCharacter(bool useMawaru)
    {
        if (!playerObj || !mawaruObj) return;

        // 位置を揃える（SpawnPointがあるならそこへ）
        if (spawnPoint)
        {
            if (useMawaru) mawaruObj.transform.position = spawnPoint.position;
            else playerObj.transform.position = spawnPoint.position;
        }

        ApplyActive(playerObj, active: !useMawaru);
        ApplyActive(mawaruObj, active: useMawaru);
    }

    void ApplyActive(GameObject obj, bool active)
    {
        obj.SetActive(active);

        // PlayerInputが残ってると「両方動く」事故が起きやすいので、
        // 非アクティブ側はそもそもSetActive(false)で完全停止にする方針
        // （必要ならここでRigidbody2D速度初期化などを追加してもOK）
        if (active)
        {
            var rb = obj.GetComponent<Rigidbody2D>();
            if (rb) rb.velocity = Vector2.zero;
        }
    }
}