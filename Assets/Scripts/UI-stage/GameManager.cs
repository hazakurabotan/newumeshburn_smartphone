using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // === 復活演出・プレイヤー ===
    public GameObject player;
    public Sprite normalSprite;
    public Sprite revivedSprite;
    public RuntimeAnimatorController revivedOverrideController;
    // ★追加：復活後に戻すための Player の初期位置
    Vector3 playerSpawnPosition;
    bool hasPlayerSpawnPosition = false;

    public Transform playerSpawnPoint;


    [Header("復活アニメ")]
    [Tooltip("復活直後に再生するステート名（revivedOverrideController内のステート名）")]
    public string revivedStateName = "RePlayerMove";   // ★追加：Inspectorで変更可

    public static bool fromRestart = false;

    public GameObject videoCanvas;
    public UnityEngine.Video.VideoPlayer videoPlayer;
    public float revivalChance = 0.4f;
    private bool triedRevival = false;

    // === シングルトン ===
    public static GameManager Instance;

    // === アイテム関連 ===
    public Sprite[] itemSprites;
    public int equippedItemId = -1;

    // === UI要素（シーン側のオブジェクト） ===
    public GameObject mainImage;
    public Sprite gameOverSpr;
    public Sprite gameClearSpr;
    public GameObject panel;
    public GameObject restartButton;
    public GameObject nextButton;
    public static bool isPaused = false;
    public GameObject pausePanel;
    public TimeController timeCnt;

    bool itemPanelOpen = false;
    public bool IsPaused => Time.timeScale == 0f;

    // === 進行・成長 ===
    public int killCount = 0;
    public int bulletLevel = 1;
    public PlayerShoot playerShoot;
    private bool isReviving = false;

    // === アイテムパネル ===
    public GameObject itemDisplayPanel;
    bool isItemPanelOpen = false;

    public GameObject levelUpPanel;

    // === ステージ管理 ===
    public static int currentStage = 1;

    // === タイマー ===
    public GameObject timeBar;
    public TextMeshProUGUI timeText;

    // === Input System ===
    private PlayerInput playerInput;
    private InputAction _pauseAction; // Pauseアクション（UI→なければPlayer）
    private InputAction _menuAction;  // Menuアクション（UI→なければPlayer）
    private InputAction _uiSubmit;   // ← 追加: Aボタン(Submit)

    // === スコア ===
    public TextMeshProUGUI scoreText;
    public static int totalScore = 0;
    public int stageScore = 0;

    [Header("Audio")]
    public AudioSource bgmSource;

    //==================== ライフサイクル ====================

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            var loaded = new List<Sprite>();
            for (int i = 0; i < 10; i++)
            {
                Sprite s = Resources.Load<Sprite>("ItemSprites/" + i);
                if (s != null) loaded.Add(s);
            }
            itemSprites = loaded.ToArray();
        }
        else if (Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        playerInput = FindObjectOfType<PlayerInput>();
        SetupInputActions();   // ★入力を初期化
    }

    void Start()
    {
        if (timeCnt == null) timeCnt = FindObjectOfType<TimeController>();

        // シーン内参照を取得
        RelinkSceneObjects();

        // ★追加：Player の初期位置を記録
        if (playerSpawnPoint != null)
        {
            // シーンに置いた SpawnPoint を最優先
            playerSpawnPosition = playerSpawnPoint.position;
            hasPlayerSpawnPosition = true;
        }
        else if (player != null)
        {
            // SpawnPoint が未設定なら、今の Player の位置を初期位置として使う
            playerSpawnPosition = player.transform.position;
            hasPlayerSpawnPosition = true;
        }

        PlayerController.gameState = "playing";


        ResetAllUI();
        StartCoroutine(InitAfterFrame());

        if (videoCanvas != null) videoCanvas.SetActive(false);
        if (videoPlayer != null) { videoPlayer.Stop(); videoPlayer.frame = 0; }

        if (fromRestart) { triedRevival = true; fromRestart = false; }

        if (SceneManager.GetActiveScene().name == "Stage1") currentStage = 1;

        SafeSetActive(mainImage, false);
        SafeSetActive(panel, false);

        if (timeCnt != null && timeCnt.gameTime == 0.0f) SafeSetActive(timeBar, false);

        SafeSetActive(itemDisplayPanel, false);
        UpdateScore();

        SafeSetActive(restartButton, false);
        SafeSetActive(nextButton, false);
        SafeSetActive(levelUpPanel, false);
    }

    IEnumerator InitAfterFrame()
    {
        yield return null;

        if (fromRestart)
        {
            triedRevival = true;
            fromRestart = false;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj;
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.Heal(pc.maxHP);
                    if (pc.hpBar == null) pc.hpBar = FindObjectOfType<HpBarController>();
                    yield return null;
                    pc.UpdateHpUI();
                    Debug.Log($"[RESTART] Player HP: {pc.currentHP} / Max: {pc.maxHP}, hpBar:{(pc.hpBar == null ? "NULL" : "OK")}");
                }
            }

            SafeSetActive(videoCanvas, false);
            if (videoPlayer != null) { videoPlayer.Stop(); videoPlayer.frame = 0; }

            PlayerController.gameState = "playing";
        }
    }

    void Update()
    {
        if (!IsGameplayScene()) return;


        // 参照が切れていたら取り直す（リスタート直後対策）
        if (!panel || !restartButton || !nextButton || !mainImage || !timeBar || !timeText || !scoreText || !itemDisplayPanel || !levelUpPanel || !pausePanel)
        {
            RelinkSceneObjects();
        }

        // タイマーUI
        if (timeCnt != null && timeText != null)
        {
            timeText.text = Mathf.CeilToInt(timeCnt.displayTime).ToString("D3");
        }

        // ==== ゲーム状態 ====
        if (PlayerController.gameState == "gameclear")
        {
            SafeSetActive(mainImage, false);
            SafeSetActive(panel, true);
            SafeSetActive(restartButton, false);
            SafeSetActive(nextButton, true);

            if (timeCnt != null)
            {
                timeCnt.isTimeOver = true;
                int time = (int)timeCnt.displayTime;
                totalScore += time * 10;
            }
            totalScore += stageScore;
            stageScore = 0;
            UpdateScore();

            PlayerController.gameState = "gameend";
        }
        else if (PlayerController.gameState == "gameover" && !triedRevival && !isReviving)
        {
            triedRevival = true;

            if (Random.value < revivalChance)
            {
                Debug.Log("[GameManager] 復活イベント開始");
                StartCoroutine(PlayRevivalSequence());
            }
            else
            {
                // ★復活しなかった → そのままリザルトへ
                if (timeCnt != null) timeCnt.isTimeOver = true;
                SceneManager.LoadScene("Resuit");   // ← Build Settings にあるシーン名
                PlayerController.gameState = "gameend";
            }
        }
        else if (PlayerController.gameState == "gameover")
        {
            // ★ここに来るのは、すでに revival 済み or 無しのとき
            if (isReviving) return;

            if (timeCnt != null) timeCnt.isTimeOver = true;
            SceneManager.LoadScene("Resuit");
            PlayerController.gameState = "gameend";
        }

        // キーボード Enter でもポーズ
        if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            TogglePausePublic();
        }

        // アイテムパネルをXで閉じる（任意）
        if (isItemPanelOpen && Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            SafeSetActive(itemDisplayPanel, false);
            isItemPanelOpen = false;
        }

        // レベルアップパネル中のクローズ
        if (levelUpPanel != null && levelUpPanel.activeSelf && Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
        {
            CloseLevelUpPanel();
        }
    }

    //==================== 進行系 ====================

    void InactiveImage() => SafeSetActive(mainImage, false);

    public void OnRestartButton()
    {
        ResetAllUI();
        fromRestart = false;
        triedRevival = false;

        totalScore = 0;
        stageScore = 0;
        equippedItemId = -1;
        currentStage = 1;

        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    public void OnNextButton()
    {
        ResetAllUI();

        SafeSetActive(panel, false);
        SafeSetActive(restartButton, false);
        SafeSetActive(nextButton, false);
        SafeSetActive(mainImage, false);

        if (timeCnt != null) timeCnt.ResetTimer();

        currentStage++;
        if (currentStage == 2) SceneManager.LoadScene("BaseScene2");
        else if (currentStage == 3) SceneManager.LoadScene("ResultScene");
    }

    void UpdateScore()
    {
        int score = stageScore + totalScore;
        if (scoreText != null) scoreText.text = score.ToString();
    }

    public Sprite GetEquippedSprite()
    {
        if (itemSprites != null && equippedItemId >= 0 && equippedItemId < itemSprites.Length)
            return itemSprites[equippedItemId];
        return null;
    }

    public bool IsItemPanelOpen() => itemDisplayPanel != null && itemDisplayPanel.activeSelf;

    //==================== シーン切替 ====================

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RelinkSceneObjects();

        if (!IsGameplayScene()) return;


        // 再リンク
        RelinkSceneObjects();


        // ★追加：そのシーンの Player 初期位置を記録
        if (playerSpawnPoint != null)
        {
            playerSpawnPosition = playerSpawnPoint.position;
            hasPlayerSpawnPosition = true;
        }
        else if (player != null)
        {
            playerSpawnPosition = player.transform.position;
            hasPlayerSpawnPosition = true;
        }

        PlayerController.gameState = "playing";

        ResetAllUI();

        triedRevival = false;
        isReviving = false;

        // タイマー
        if (timeCnt == null) timeCnt = FindObjectOfType<TimeController>();
        if (timeCnt != null)
        {
            timeCnt.ResetTimer();
            timeCnt.isTimeOver = false;
            timeCnt.enabled = !scene.name.Contains("Shop");
        }

        // リスタートから来た場合の復帰
        if (fromRestart)
        {
            triedRevival = true;
            fromRestart = false;

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj;
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.hpBar = FindObjectOfType<HpBarController>();
                    pc.Heal(pc.maxHP);
                    pc.UpdateHpUI();
                    Debug.Log($"[Restart後] currentHP={pc.currentHP}, hpBar={(pc.hpBar == null ? "NULL" : "OK")}");
                }
            }

            if (videoCanvas == null) videoCanvas = GameObject.Find("videoCanvas");
            SafeSetActive(videoCanvas, false);

            if (videoPlayer == null) videoPlayer = FindObjectOfType<UnityEngine.Video.VideoPlayer>();
            if (videoPlayer != null) { videoPlayer.Stop(); videoPlayer.frame = 0; }

            PlayerController.gameState = "playing";
        }

        // 入力を取り直す（シーン跨ぎ対策）
        SetupInputActions();
    }

    IEnumerator PlayRevivalSequence()
    {
        isReviving = true;

        player = GameObject.FindGameObjectWithTag("Player");
        if (videoCanvas == null) videoCanvas = GameObject.Find("VideoCanvas");
        if (videoPlayer == null) videoPlayer = FindObjectOfType<UnityEngine.Video.VideoPlayer>();

        if (player == null)
        {
            Debug.LogError("復活演出時にplayerが見つからない！");
            isReviving = false; // ★安全
            yield break;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        var col = player.GetComponent<Collider2D>();
        if (rb != null) rb.simulated = false;
        if (col != null) col.enabled = false;
        player.SetActive(false);

        SafeSetActive(videoCanvas, true);

        if (videoPlayer != null)
        {
            if (videoPlayer.clip == null)
            {
                Debug.LogError("[復活] VideoPlayerのVideoClipが設定されていません！");
            }
            else
            {
                videoPlayer.Stop();
                videoPlayer.frame = 0;
                videoPlayer.Prepare();
                while (!videoPlayer.isPrepared) yield return null;
                videoPlayer.Play();
                while (videoPlayer.isPlaying) yield return null;
            }
        }
        else
        {
            Debug.LogError("[復活] VideoPlayerが見つかりません！");
        }

        SafeSetActive(videoCanvas, false);

        // ====== ここから：ステージをリロードして初期位置から復活 ======

        triedRevival = true;          // もう一度は復活しない
        fromRestart = true;          // Start / OnSceneLoaded 側のHP全回復処理を使うなら

        // ゲーム状態を playing に戻しておく（staticなのでそのまま持ち越される）
        PlayerController.gameState = "playing";

        // 今いるステージ名を取得してロードし直す
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log("[復活] シーンをリロードして初期位置から再スタート: " + sceneName);
        SceneManager.LoadScene(sceneName);

        isReviving = false;

        yield return new WaitForFixedUpdate();

        if (rb != null) { rb.velocity = Vector2.zero; rb.simulated = true; }
        if (col != null) col.enabled = true;

        player.SetActive(true);
        yield return null;

        var sr = player.GetComponent<SpriteRenderer>();
        if (sr != null && revivedSprite != null) sr.sprite = revivedSprite;

        if (timeCnt != null) timeCnt.isTimeOver = false;
        PlayerController.gameState = "playing";
        SafeSetActive(restartButton, false);

        var pc2 = player.GetComponent<PlayerController>();
        if (pc2 != null) { pc2.Heal(pc2.maxHP); pc2.UpdateHpUI(); }

        // ===== ここから：Animatorの堅牢化（★修正箇所） =====
        var animator = player.GetComponent<Animator>();
        if (animator == null) Debug.LogWarning("[復活] animatorがnullです！");
        if (revivedOverrideController == null) Debug.LogWarning("[復活] revivedOverrideControllerがnullです！");

        if (animator != null && revivedOverrideController != null)
        {
            Debug.Log("[復活] 切り替え前 Controller名: " + animator.runtimeAnimatorController?.name);

            // コントローラ差し替え
            animator.runtimeAnimatorController = revivedOverrideController;

            // 差し替え直後の安定化
            animator.Rebind();
            animator.Update(0f);

            Debug.Log("[復活] 切り替え後 Controller名: " + animator.runtimeAnimatorController?.name);
            yield return null; // 1フレーム待機してレイヤー/ブレンドを安定化

            int layer = 0;                           // ★レイヤーを明示
            string state = revivedStateName;         // ★Inspectorから指定
            int hash = Animator.StringToHash(state);

            if (!animator.HasState(layer, hash))
            {
                Debug.LogWarning($"[復活] ステート '{state}' が見つかりません（Controller: {animator.runtimeAnimatorController?.name}）。" +
                                 " Animatorウィンドウでノード名を確認するか、revivedStateName を正しい名前に変更してください。");

                // フォールバック候補（必要なら増やしてください）
                string[] fallbacks = { "Idle", "Idle01", "PlayerIdle" };
                bool found = false;
                foreach (var fb in fallbacks)
                {
                    int fbHash = Animator.StringToHash(fb);
                    if (animator.HasState(layer, fbHash))
                    {
                        state = fb;
                        hash = fbHash;
                        found = true;
                        Debug.Log($"[復活] フォールバックで '{state}' を再生します。");
                        break;
                    }
                }

                if (!found)
                {
                    // どうしても見つからない場合は再バインドのみで終了
                    animator.Rebind();
                    animator.Update(0f);
                    isReviving = false;
                    yield break;
                }
            }

            // 再生（レイヤー/正規化時間を指定）
            animator.Play(hash, layer, 0f);
        }
        else
        {
            Debug.LogWarning("[復活] animator or revivedOverrideController がnullなので切り替えスキップ！");
        }
        // ===== ここまで：Animatorの堅牢化 =====

        isReviving = false;
    }

    //==================== ユーティリティ ====================

    void SafeSetActive(GameObject go, bool active)
    {
        if (go) go.SetActive(active);
    }

    void RelinkSceneObjects()
    {
        if (!panel) panel = GameObject.Find("Panel") ?? GameObject.Find("PausePanel") ?? panel;
        if (!restartButton) restartButton = GameObject.Find("RestartButton");
        if (!nextButton) nextButton = GameObject.Find("NextButton");
        if (!mainImage) mainImage = GameObject.Find("MainImage") ?? GameObject.Find("cutInImage") ?? mainImage;
        if (!pausePanel) pausePanel = GameObject.Find("PausePanel");
        if (!itemDisplayPanel) itemDisplayPanel = GameObject.Find("ItemDisplayPanel") ?? GameObject.Find("itemDisplayPanel");

        if (!timeBar) timeBar = GameObject.Find("TimeBar");
        if (!timeText)
        {
            var go = GameObject.Find("TimeText");
            if (go) timeText = go.GetComponent<TextMeshProUGUI>();
            if (!timeText) timeText = FindObjectOfType<TextMeshProUGUI>(true);
        }

        if (!scoreText)
        {
            var go = GameObject.Find("ScoreText");
            if (go) scoreText = go.GetComponent<TextMeshProUGUI>();
            if (!scoreText) scoreText = FindObjectOfType<TextMeshProUGUI>(true);
        }

        if (!levelUpPanel) levelUpPanel = GameObject.Find("levelUpPanel");

        if (timeCnt == null) timeCnt = FindObjectOfType<TimeController>();
        if (videoCanvas == null) videoCanvas = GameObject.Find("VideoCanvas") ?? GameObject.Find("videoCanvas");
        if (videoPlayer == null) videoPlayer = FindObjectOfType<UnityEngine.Video.VideoPlayer>();

        if (!bgmSource) bgmSource = GameObject.Find("BGMPlayer")?.GetComponent<AudioSource>();
    }

    void ResetAllUI()
    {
        SafeSetActive(videoCanvas, false);
        if (videoPlayer != null) { videoPlayer.Stop(); videoPlayer.frame = 0; }

        SafeSetActive(mainImage, false);
        SafeSetActive(panel, false);
        SafeSetActive(restartButton, false);
        SafeSetActive(nextButton, false);
    }

    public void CloseLevelUpPanel()
    {
        SafeSetActive(levelUpPanel, false);
        Time.timeScale = 1f;
        playerInput?.actions?.FindActionMap("Player")?.Enable();   // ← 追加（任意）
    }

    public void TogglePausePublic()
    {
        bool willPause = Time.timeScale > 0f;

        Time.timeScale = willPause ? 0f : 1f;
        if (pausePanel) pausePanel.SetActive(willPause);

        if (bgmSource != null)
        {
            if (willPause) bgmSource.Pause();
            else bgmSource.UnPause();
        }

        var playerMap = playerInput != null ? playerInput.actions.FindActionMap("Player") : null;
        if (playerMap != null)
        {
            string[] toToggle = { "Move", "Jump", "Shoot", "Rope", "Punch", "Summon", "ItemPanel", "Railgun" };
            foreach (var name in toToggle)
            {
                var act = playerMap.FindAction(name);
                if (act == null) continue;
                if (willPause) act.Disable(); else act.Enable();
            }
        }

        var uiMap = playerInput != null ? playerInput.actions.FindActionMap("UI") : null;
        if (uiMap != null) uiMap.Enable(); // Pause/Menu を常に受付

        Debug.Log(willPause ? "[Pause] Enter" : "[Pause] Exit");
    }

    public void SetItemPanelOpen(bool open) => itemPanelOpen = open;

    void ShowRestartPanel()
    {
        SafeSetActive(panel, true);
        SafeSetActive(restartButton, true);
        if (playerInput != null)
        {
            playerInput.actions.FindActionMap("Player").Disable();
            playerInput.actions.FindActionMap("UI").Enable();
        }
    }

    void HideRestartPanel()
    {
        SafeSetActive(panel, false);
        SafeSetActive(restartButton, false);
        if (playerInput != null)
        {
            playerInput.actions.FindActionMap("UI").Disable();
            playerInput.actions.FindActionMap("Player").Enable();
        }
    }

    //===== 入力の取得/購読を一元管理 =====
    void SetupInputActions()
    {
        if (playerInput == null) playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput == null || playerInput.actions == null) return;

        var uiMap = playerInput.actions.FindActionMap("UI");
        var playerMap = playerInput.actions.FindActionMap("Player");

        // Pause
        if (_pauseAction != null) _pauseAction.performed -= OnUIPause;
        _pauseAction = uiMap?.FindAction("Pause") ?? playerMap?.FindAction("Pause");
        if (_pauseAction != null) { _pauseAction.Enable(); _pauseAction.performed += OnUIPause; }

        // Menu
        if (_menuAction != null) _menuAction.performed -= OnOpenMainMenu;
        _menuAction = uiMap?.FindAction("Menu") ?? playerMap?.FindAction("Menu");
        if (_menuAction != null) { _menuAction.Enable(); _menuAction.performed += OnOpenMainMenu; }

        // Submit (A / South)  ← レベルアップパネルをAで閉じる用
        if (_uiSubmit != null) _uiSubmit.performed -= OnUISubmit;
        _uiSubmit = uiMap?.FindAction("Submit") ?? playerMap?.FindAction("Submit");
        if (_uiSubmit != null) { _uiSubmit.Enable(); _uiSubmit.performed += OnUISubmit; }

        // Build（任意：ビルド/スキルパネル用に追加している場合）
        var build = uiMap?.FindAction("Build") ?? playerMap?.FindAction("Build");
        if (build != null)
        {
            build.Enable();
            build.performed -= OnOpenBuild; // 多重購読防止
            build.performed += OnOpenBuild;
        }

        uiMap?.Enable(); // UI ナビ全体を有効化
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SetupInputActions(); // シーン有効化時にも再セット
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_pauseAction != null) _pauseAction.performed -= OnUIPause;
        if (_menuAction != null) _menuAction.performed -= OnOpenMainMenu;
        if (_uiSubmit != null) _uiSubmit.performed -= OnUISubmit;   // ← 追加
    }

    // 既存の「さいかい(再開)」ボタンのOnClickはこれに紐づけ
    public void OnPleaseButton() => TogglePausePublic();

    private void OnUIPause(InputAction.CallbackContext _)
    {
        TogglePausePublic();
    }

    private void OnOpenMainMenu(InputAction.CallbackContext _)
    {
        FindObjectOfType<MenuManager>(true)?.ToggleMenu();
    }

    private void OnUISubmit(InputAction.CallbackContext _)
    {
        if (levelUpPanel != null && levelUpPanel.activeSelf)
            CloseLevelUpPanel();
    }

    private void OnOpenBuild(InputAction.CallbackContext _)
    {
        var bm = BuildManager.Instance ?? FindObjectOfType<BuildManager>(true);
        bm?.Toggle();
    }

    bool IsGameplayScene()
    {
        string n = SceneManager.GetActiveScene().name;
        // あなたの命名に合わせて調整OK：
        // Stage11 / Stage12 / Stage1… みたいに Stage で始まるならこれでOK
        return n.StartsWith("Stage") || n.StartsWith("BaseScene");
    }

}
