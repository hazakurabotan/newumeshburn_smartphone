using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class MenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject detailPanel;

    [Header("Persona-style Intro")]
    public GameObject introCanvas;
    public RawImage introImage;
    public VideoPlayer introPlayer;
    public bool skippable = true;
    public float minIntroTime = 0.25f;

    [Header("Input (for skip)")]
    public InputActionReference submit;
    public InputActionReference cancel;
    public InputActionReference menu;

    [SerializeField] bool coverScreen = true; // true=画面を覆う(一部トリミング), false=黒帯で全表示

    PlayerInput _pi;
    bool _isOpen;
    bool _isIntroPlaying;
    float _introTimer;

    void Awake()
    {
        _pi = FindObjectOfType<PlayerInput>(true);
        if (introCanvas) introCanvas.SetActive(false);
        if (menuPanel) menuPanel.SetActive(false);
        if (detailPanel) detailPanel.SetActive(false);
    }

    public void ToggleMenu()
    {
        if (_isOpen)
        {
            CloseMenu();
        }
        else
        {
            StartCoroutine(OpenMenuWithIntro());
        }
    }

    public void OpenMenuBySystem()
    {
        if (!_isOpen)
        {
            StartCoroutine(OpenMenuWithIntro());
        }
    }

    IEnumerator OpenMenuWithIntro()
    {
        _isOpen = true;

        // 入力/時間停止
        Time.timeScale = 0f;
        if (_pi != null)
        {
            _pi.actions.FindActionMap("Player")?.Disable();
            _pi.actions.FindActionMap("UI")?.Enable();
        }
        if (menuPanel) menuPanel.SetActive(false);
        if (detailPanel) detailPanel.SetActive(false);

        // --- Intro 再生 ---
        if (introCanvas && introPlayer && introImage)
        {
            introCanvas.SetActive(true);
            yield return new WaitForEndOfFrame();   // ← これを追加
            _isIntroPlaying = true;
            _introTimer = 0f;

            // 再生準備が終わったらサイズを合わせる
            introPlayer.prepareCompleted += _ => FitIntroToScreen();
            introPlayer.Prepare();
            while (!introPlayer.isPrepared) yield return null;

            // 念のためもう一度フィット
            FitIntroToScreen();

            // スキップ
            System.Action<InputAction.CallbackContext> skip = ctx =>
            {
                if (skippable && _isIntroPlaying && _introTimer >= minIntroTime)
                {
                    introPlayer.Stop();
                    _isIntroPlaying = false;
                }
            };
            if (submit) submit.action.performed += skip;
            if (cancel) cancel.action.performed += skip;
            if (menu) menu.action.performed += skip;

            introPlayer.Play();
            while (_isIntroPlaying && introPlayer.isPlaying)
            {
                _introTimer += Time.unscaledDeltaTime;
                // 画面回転/解像度変化にも追随
                FitIntroToScreen();
                yield return null;
            }

            if (submit) submit.action.performed -= skip;
            if (cancel) cancel.action.performed -= skip;
            if (menu) menu.action.performed -= skip;

            introCanvas.SetActive(false);
        }

        // --- メニュー本体表示 ---
        if (menuPanel) menuPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        if (detailPanel && detailPanel.activeSelf) detailPanel.SetActive(false);
        if (menuPanel) menuPanel.SetActive(false);
        if (introCanvas) introCanvas.SetActive(false);

        Time.timeScale = 1f;
        if (_pi != null)
        {
            _pi.actions.FindActionMap("Player")?.Enable();
            _pi.actions.FindActionMap("UI")?.Enable();
        }
        _isOpen = false;
    }

    // 画面全体に動画をフィット（カバー or フィット）
    void FitIntroToScreen()
    {
        if (!introImage) return;

        var imgRT = introImage.rectTransform;
        var root = introImage.canvas ? introImage.canvas.rootCanvas : null;

        // ① Canvasの実ピクセルサイズを取る（最終手段としてScreenサイズ）
        var pr = root ? root.pixelRect : new Rect(0, 0, Screen.width, Screen.height);
        float pw = pr.width;
        float ph = pr.height;
        if (pw <= 1 || ph <= 1) { pw = Screen.width; ph = Screen.height; }

        // ② 動画（or RenderTexture）のアスペクト
        float vw = 16f, vh = 9f;
        if (introPlayer && introPlayer.texture)
        { vw = introPlayer.texture.width; vh = introPlayer.texture.height; }
        else if (introImage.texture)       // RenderTexture等
        { vw = introImage.texture.width; vh = introImage.texture.height; }

        float vAspect = vw / vh;
        float pAspect = pw / ph;

        // ③ 画面を覆う(cover) / 全表示(fit) を計算
        Vector2 size;
        if (coverScreen)
            size = (pAspect > vAspect) ? new Vector2(pw, pw / vAspect) : new Vector2(ph * vAspect, ph);
        else
            size = (pAspect > vAspect) ? new Vector2(ph * vAspect, ph) : new Vector2(pw, pw / vAspect);

        // ④ 中央に固定してサイズ適用
        imgRT.anchorMin = imgRT.anchorMax = new Vector2(0.5f, 0.5f);
        imgRT.pivot = new Vector2(0.5f, 0.5f);
        imgRT.sizeDelta = size;
        imgRT.anchoredPosition = Vector2.zero;
    }

    // prepareCompleted のラムダをメソッドに
    void OnIntroPrepared(VideoPlayer vp)
    {
        FitIntroToScreen();
    }

    // スキップ用ラムダもメソッドに
    void OnSkip(InputAction.CallbackContext ctx)
    {
        if (skippable && _isIntroPlaying && _introTimer >= minIntroTime)
        {
            introPlayer.Stop();
            _isIntroPlaying = false;
        }
    }




}
