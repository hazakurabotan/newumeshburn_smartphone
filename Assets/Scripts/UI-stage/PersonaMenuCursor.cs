using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PersonaMenuCursor : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform cursor;                 // 選択カーソル
    public List<RectTransform> menuItems = new();// メニュー項目
    public PersonaMenuDetail detailPanelScript;  // 上部の詳細
    public MenuManager menuManager;              // ルートに付けた MenuManager

    [Header("UI Actions (Input System)")]
    public InputActionReference navigate; // UI/Navigate (Vector2)
    public InputActionReference submit;   // UI/Submit   (Button South)
    public InputActionReference cancel;   // UI/Cancel   (Button East)
    public InputActionReference menu;     // UI/Menu     (Start)  ※任意

    [Header("Tuning")]
    public float moveSpeed = 12f;       // カーソル追従の速さ
    public float repeatDelay = 0.2f;    // 長押し時のリピート間隔
    public float dead = 0.5f;           // 入力デッドゾーン

    int currentIndex = 0;
    float repeatTimer = 0f;
    Vector2 lastNav = Vector2.zero;

    void OnEnable()
    {
        if (submit) submit.action.performed += OnSubmit;
        if (cancel) cancel.action.performed += OnCancel;
        if (menu) menu.action.performed += OnCancel;

        navigate?.action.Enable();
        submit?.action.Enable();
        cancel?.action.Enable();
        menu?.action.Enable();

        // 初期位置
        if (cursor && menuItems.Count > 0)
            cursor.anchoredPosition = menuItems[currentIndex].anchoredPosition;
    }

    void OnDisable()
    {
        if (submit) submit.action.performed -= OnSubmit;
        if (cancel) cancel.action.performed -= OnCancel;
        if (menu) menu.action.performed -= OnCancel;
    }

    void Update()
    {
        if (menuItems.Count == 0 || !cursor) return;

        // --- キーボードの保険（必要なければ削除OK）
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.rightArrowKey.wasPressedThisFrame) Step(1);
            if (kb.leftArrowKey.wasPressedThisFrame) Step(-1);
            if (kb.zKey.wasPressedThisFrame) OnSubmit(default);
            if (kb.escapeKey.wasPressedThisFrame) OnCancel(default);
        }

        // --- ゲームパッド Navigate（1ステップ + リピート）
        var nav = navigate ? navigate.action.ReadValue<Vector2>() : Vector2.zero;

        bool stepped = false;
        if (lastNav.x <= dead && nav.x > dead) { Step(1); stepped = true; repeatTimer = repeatDelay; }
        if (lastNav.x >= -dead && nav.x < -dead) { Step(-1); stepped = true; repeatTimer = repeatDelay; }

        if (!stepped && Mathf.Abs(nav.x) > dead)
        {
            repeatTimer -= Time.unscaledDeltaTime; // ポーズ中でも動く
            if (repeatTimer <= 0f)
            {
                Step(nav.x > 0 ? 1 : -1);
                repeatTimer = repeatDelay;
            }
        }
        else if (Mathf.Abs(nav.x) <= dead)
        {
            repeatTimer = 0f;
        }
        lastNav = nav;

        // カーソルをスムーズ追従（ポーズ中でも動く）
        var target = menuItems[currentIndex].anchoredPosition;
        float t = 1f - Mathf.Exp(-moveSpeed * Time.unscaledDeltaTime);
        cursor.anchoredPosition = Vector2.Lerp(cursor.anchoredPosition, target, t);
    }

    void Step(int dir)
    {
        if (menuItems.Count == 0) return;
        currentIndex = (currentIndex + dir + menuItems.Count) % menuItems.Count;
    }

    void OnSubmit(InputAction.CallbackContext _)
    {
        if (detailPanelScript) detailPanelScript.ShowDetail(currentIndex);
    }

    void OnCancel(InputAction.CallbackContext _)
    {
        // 詳細を開いていたら先に閉じる
        if (detailPanelScript != null && detailPanelScript.detailPanel.activeSelf)
        {
            detailPanelScript.HideDetail();
            return;
        }
        // そうでなければメニュー自体を閉じる
        if (menuManager) menuManager.ToggleMenu();
    }
}
