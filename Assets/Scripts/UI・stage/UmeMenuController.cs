using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;



public class UmeMenuController : MonoBehaviour
{


    [Header("Panels")]
    public GameObject mainMenu;
    public GameObject weaponScreen, accessoriesScreen, settingsScreen;
    public GameObject skillScreen, missionScreen, saveScreen, titleScreen; // ← 追加

    [Header("First Selects")]
    public GameObject firstSelect;                 // MainMenu の最初に選ばせたいボタン（例: WEAPON）
    public GameObject firstSelectWeapon, firstSelectAccessories, firstSelectSettings;
    public GameObject firstSelectSkill, firstSelectMission, firstSelectSave, firstSelectTitle;

    [Header("Back To Title")]
    public string titleSceneName = "Title";        // タイトルシーン名（プロジェクトに合わせて変更）

    [Header("Input")]
    public InputActionReference uiCancel;          // UI/Cancel

    void OnEnable()
    {
        OpenMain();
        if (uiCancel) { uiCancel.action.performed += OnCancel; uiCancel.action.Enable(); }
    }
    void OnDisable() { if (uiCancel) uiCancel.action.performed -= OnCancel; }

    // ===== メインへ =====
    public void OpenMain() { SetAll(false); SafeOn(mainMenu, true); Focus(firstSelect); }

    // ===== サブスクリーンを開く =====
    public void OpenWeapon() => Open(weaponScreen, firstSelectWeapon);
    public void OpenAccessories() => Open(accessoriesScreen, firstSelectAccessories);
    public void OpenSettings() => Open(settingsScreen, firstSelectSettings);
    public void OpenSkill() => Open(skillScreen, firstSelectSkill);
    public void OpenMission() => Open(missionScreen, firstSelectMission);
    public void OpenSave() => Open(saveScreen, firstSelectSave);

    // ===== タイトルへ戻る（確認画面） =====
    public void OpenTitleScreen() => Open(titleScreen, firstSelectTitle);
    public void ConfirmBackToTitle()
    {
        Time.timeScale = 1f;

        // メニュー開いてても閉じてても安全
        var mm = FindObjectOfType<MenuManager>(true);
        if (mm) mm.CloseMenu();

        if (!string.IsNullOrEmpty(titleSceneName))
            SceneManager.LoadScene(titleSceneName);
    }

    // ===== 共通ユーティリティ =====
    void Open(GameObject screen, GameObject first = null)
    { SetAll(false); SafeOn(screen, true); Focus(first); }

    void SetAll(bool on)
    {
        SafeOn(mainMenu, on);
        SafeOn(weaponScreen, on); SafeOn(accessoriesScreen, on); SafeOn(settingsScreen, on);
        SafeOn(skillScreen, on); SafeOn(missionScreen, on); SafeOn(saveScreen, on); SafeOn(titleScreen, on);
    }

    void SafeOn(GameObject go, bool on) { if (go) go.SetActive(on); }
    void Focus(GameObject go)
    {
        var es = EventSystem.current;
        if (es == null || go == null) return;
        // いったん解除してからセットすると、裏に残った選択を確実に切れる
        es.SetSelectedGameObject(null);
        es.SetSelectedGameObject(go);
    }

    // Cancel: サブ画面中→メイン / メイン中→メニュー閉じる
    void OnCancel(InputAction.CallbackContext _)
    {
        if (mainMenu && !mainMenu.activeInHierarchy) OpenMain();
        else { var mm = FindObjectOfType<MenuManager>(true); if (mm) mm.CloseMenu(); }
    }
}
