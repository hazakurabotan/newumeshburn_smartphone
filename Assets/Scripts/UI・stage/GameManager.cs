using System.Collections;
using System.Collections.Generic;
using TMPro;                       // TextMeshPro用
using UnityEngine;
using UnityEngine.SceneManagement;  // シーン切り替え用
using UnityEngine.UI;              // UI制御用

// ------------------------------------------------------
// GameManager
// ゲーム全体の進行・UI・アイテム・演出をまとめて管理する
// シングルトン（ゲーム中で1個だけ存在）パターン
// ------------------------------------------------------
public class GameManager : MonoBehaviour
{
    // === 復活演出・プレイヤー ===
    public GameObject player;                       // プレイヤー本体
    public Sprite normalSprite;                     // 通常時スプライト
    public Sprite revivedSprite;                    // 復活演出用スプライト
    public RuntimeAnimatorController revivedOverrideController; // 復活用アニメコントローラー

    public static bool fromRestart = false;         // リスタートからの遷移かフラグ

    public GameObject videoCanvas;                  // 復活演出Canvas
    public UnityEngine.Video.VideoPlayer videoPlayer; // 復活演出Video
    public float revivalChance = 0.4f;              // 復活確率
    private bool triedRevival = false;              // 復活を既に試したか

    // === シングルトン管理 ===
    public static GameManager Instance;             // 唯一のインスタンス

    // === アイテム関連 ===
    public Sprite[] itemSprites;                    // アイテム画像一覧
    public int equippedItemId = -1;                 // 装備中アイテムID

    // === UI要素 ===
    public GameObject mainImage;                    // 勝利・敗北等の画像
    public Sprite gameOverSpr;
    public Sprite gameClearSpr;
    public GameObject panel;                        // ボタンパネル
    public GameObject restartButton;                // リスタートボタン
    public GameObject nextButton;                   // ネクストボタン
    public Image cutInImage;                    // カットイン画像
    public AudioSource cutInAudioSource;            // カットイン用音
    public AudioClip cutInVoiceClip;
    public GameObject laserPrefab;                  // レーザー演出
    public Sprite railgunCutinSprite;
    public static bool isPaused = false;
    public GameObject pausePanel;
    public TimeController timeCnt;


    // === ゲーム進行・成長関連 ===
    public int killCount = 0;                       // 撃破数
    public int bulletLevel = 1;                     // 弾のレベル
    public GameObject levelUpPanel;                 // レベルアップ演出
    public PlayerShoot playerShoot;                 // 弾発射スクリプト参照

    private bool isReviving = false;                // 復活演出中かどうか

    // === アイテムパネル ===
    public GameObject itemDisplayPanel;
    bool isItemPanelOpen = false;

    // === ステージ管理 ===
    public static int currentStage = 1;

    // === タイマー関連 ===
    public GameObject timeBar;
    public TextMeshProUGUI timeText;
    

    // === スコア管理 ===
    public TextMeshProUGUI scoreText;
    public static int totalScore = 0; // 合計スコア
    public int stageScore = 0;        // ステージごとスコア

    // ------------------------------------------------------
    // Awake: シングルトン初期化、リソース読み込み
    // ------------------------------------------------------
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;                      // 唯一のインスタンスに
            DontDestroyOnLoad(this.gameObject);   // シーン遷移でも消えない

            // Resourcesフォルダからアイテムスプライトを読み込み
            List<Sprite> loaded = new List<Sprite>();
            for (int i = 0; i < 10; i++)
            {
                Sprite s = Resources.Load<Sprite>("ItemSprites/" + i);
                if (s != null) loaded.Add(s);
            }
            itemSprites = loaded.ToArray();
        }
        else if (Instance != this)
        {
            Destroy(this.gameObject); // 2個目は消す
        }
    }

    // ------------------------------------------------------
    // Start: 各種UIと状態の初期化
    // ------------------------------------------------------
    void Start()
    {
        // Inspectorで設定済みならそれを使う
        // 未設定時のみ自動取得する
        if (timeCnt == null)
            timeCnt = FindObjectOfType<TimeController>();

        ResetAllUI();

        StartCoroutine(InitAfterFrame());

        if (videoCanvas != null) videoCanvas.SetActive(false);
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.frame = 0;
        }
        if (fromRestart)
        {
            triedRevival = true;
            fromRestart = false;
        }
        if (SceneManager.GetActiveScene().name == "Stage1") currentStage = 1;
        InactiveImage();
        panel.SetActive(false);

        // ↓ここ消してOK！　【絶対不要】
        // timeCnt = GetComponent<TimeController>();

        if (timeCnt != null && timeCnt.gameTime == 0.0f) timeBar.SetActive(false);

        if (itemDisplayPanel != null) itemDisplayPanel.SetActive(false);
        UpdateScore();

        if (restartButton != null) restartButton.SetActive(false);
        if (nextButton != null) nextButton.SetActive(false);

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }


    // ------------------------------------------------------
    // フレーム跨ぎの初期化処理（HPバー再取得・復活演出など）
    // ------------------------------------------------------
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

                    // HPバー参照がnullなら取得し直し
                    if (pc.hpBar == null)
                        pc.hpBar = FindObjectOfType<HpBarController>();

                    yield return null; // UIがリンク安定するまで1フレーム待つ
                    pc.UpdateHpUI();

                    Debug.Log($"[RESTART] Player HP: {pc.currentHP} / Max: {pc.maxHP}, hpBar:{(pc.hpBar == null ? "NULL" : "OK")}");
                }
            }

            // 復活演出キャンバス非表示
            if (videoCanvas != null)
                videoCanvas.SetActive(false);

            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.frame = 0;
            }

            PlayerController.gameState = "playing";
        }
    }

    // ------------------------------------------------------
    // Update: ゲーム進行・UI制御・スコア・演出など
    // ------------------------------------------------------
    void Update()
    {
        if (timeCnt == null) Debug.LogError("timeCntがnullです！");
        else Debug.Log("timeCnt=" + timeCnt.displayTime);


        // タイマーUI更新
        if (timeCnt != null && timeText != null)
        {
            timeText.text = Mathf.CeilToInt(timeCnt.displayTime).ToString("D3");
        }

        // ゲームクリア時のUI
        if (PlayerController.gameState == "gameclear")
        {
            mainImage.SetActive(false);
            panel.SetActive(true);
            if (restartButton != null) restartButton.SetActive(false);
            if (nextButton != null) nextButton.SetActive(true);

            // タイムボーナス＆スコア加算
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
        // ゲームオーバー＆復活判定
        // プレイヤーがやられた時に、復活演出を行うかどうかを決定する部分
        else if (PlayerController.gameState == "gameover" && !triedRevival && !isReviving)
        {
            // もう一度復活判定を行わないようにフラグを立てる
            triedRevival = true;

            // 復活確率（revivalChance, 例:0.4）より小さいランダム値なら…
            if (Random.value < revivalChance)
            {
                // 復活演出コルーチンを実行（PlayRevivalSequenceが走る）
                StartCoroutine(PlayRevivalSequence());
            }
            else
            {
                // 復活失敗の場合はゲームオーバー用のUIを表示

                mainImage.SetActive(false);       // メイン画像を非表示
                panel.SetActive(true);            // ボタンパネルを表示

                // リスタートボタンはON、ネクストボタンはOFF
                if (restartButton != null) restartButton.SetActive(true);
                if (nextButton != null) nextButton.SetActive(false);

                // タイマーも止める（時間切れ扱い）
                if (timeCnt != null) timeCnt.isTimeOver = true;

                // ゲーム状態を"gameend"に切り替え
                PlayerController.gameState = "gameend";
            }
        }

        // 2回目以降のゲームオーバーUI
        else if (PlayerController.gameState == "gameover")
        {
            if (isReviving) return;

            mainImage.SetActive(false);
            panel.SetActive(true);
            if (restartButton != null) restartButton.SetActive(true);
            if (nextButton != null) nextButton.SetActive(false);
            if (timeCnt != null) timeCnt.isTimeOver = true;

            PlayerController.gameState = "gameend";
        }
        // プレイ中
        else if (PlayerController.gameState == "playing")
        {
            // プレイヤーのスコア加算
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerController playerCnt = player.GetComponent<PlayerController>();
                if (playerCnt.score != 0)
                {
                    stageScore += playerCnt.score;
                    playerCnt.score = 0;
                    UpdateScore();
                }
            }
        }

        // アイテムパネル開閉（左Shiftで開く、Xで閉じる）
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (itemDisplayPanel != null)
            {
                itemDisplayPanel.SetActive(true);
                isItemPanelOpen = true;
            }
        }
        else if (isItemPanelOpen && Input.GetKeyDown(KeyCode.X))
        {
            if (itemDisplayPanel != null)
            {
                itemDisplayPanel.SetActive(false);
                isItemPanelOpen = false;
            }
        }

        // カットイン演出（Sキー）
        if (Input.GetKeyDown(KeyCode.S))
        {
            ShowCutIn(); // ←こっちに差し替える！
            if (cutInAudioSource != null && cutInVoiceClip != null)
            {
                cutInAudioSource.PlayOneShot(cutInVoiceClip);
            }
        }

        // レベルアップパネルが出ていればZキーで閉じる
        if (levelUpPanel != null && levelUpPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                CloseLevelUpPanel();
            }
        }

        // EnterキーでポーズON/OFF
        if (Input.GetKeyDown(KeyCode.Return))
        {
            TogglePause();
        }

    }

    // --- 敵撃破時のカウント＆レベルアップ処理 ---
    // 敵を倒したときに呼び出す関数
    public void AddKill()
    {
        // 撃破数を1つ増やす
        killCount++;

        // もし10体撃破かつ弾レベルが1のとき
        if (killCount == 10 && bulletLevel == 1)
        {
            // 弾レベルを2に上げる（例：連射数アップなど）
            bulletLevel = 2;

            // プレイヤーのPlayerShootスクリプトを取得
            var playerShoot = player.GetComponent<PlayerShoot>();
            if (playerShoot != null)
            {
                // 弾の最大同時発射数を4発に変更
                playerShoot.maxShots = 4;
            }

            // レベルアップ演出用パネル（UI）を表示
            if (levelUpPanel != null)
                levelUpPanel.SetActive(true);

            // ★ここでゲームを一時停止（演出中だけ進行ストップ）
            Time.timeScale = 0f;
        }
    }


    // --- カットインを隠してレーザー演出 ---
    void HideCutIn()
    {
        if (cutInImage != null)
            cutInImage.gameObject.SetActive(false);
        FireLaser();
    }

    // --- レーザーをプレイヤーの位置から発射 ---
    // プレイヤーの位置＆向きに合わせてレーザーオブジェクトを出現させる関数
    void FireLaser()
    {
        // シーン上の"Player"タグ付きオブジェクトを探す
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return; // 見つからなければ何もしない

        // プレイヤーの現在位置
        Vector3 pos = player.transform.position;
        // プレイヤーの向き（ローカルスケールXが＋で右、－で左向き）
        float dir = player.transform.localScale.x;
        // レーザーの長さ
        float length = 1.3f;

        // プレイヤーの少し前方（5fぶん前）にレーザーを出現させる
        GameObject laserObj = Instantiate(
            laserPrefab,                     // プレハブ
            pos + new Vector3(5f * dir, 0, 0), // 発射位置（向きに応じて左右）
            Quaternion.identity              // 回転はデフォルト
        );

        // レーザーのスケール（長さ）を調整
        Vector3 scale = laserObj.transform.localScale;
        scale.x = length; // x軸（横方向）のサイズを指定長さに
        if (dir < 0) scale.x *= -1; // 左向きなら反転

        laserObj.transform.localScale = scale;

        // レーザーの中央がちょうど"前"に来るように、さらに位置を微調整
        laserObj.transform.position += new Vector3((length / 2f) * dir, 0, 0);

        // レーザーオブジェクトを0.6秒後に自動削除（演出終了）
        Destroy(laserObj, 0.6f);
    }



    // --- mainImage（勝利・敗北画像）を非表示にするだけ ---
    void InactiveImage()
    {
        mainImage.SetActive(false);
    }

    // --- リスタートボタンで再スタート ---
    public void OnRestartButton()
    {
        ResetAllUI();
        fromRestart = true;
        triedRevival = true;

        if (panel != null) panel.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);
        if (nextButton != null) nextButton.SetActive(false);
        if (mainImage != null) mainImage.SetActive(false);

        if (timeCnt != null)
        {
            timeCnt.ResetTimer();
        }
        // シーンを再読み込み
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // --- ネクストボタンで次ステージへ ---
    public void OnNextButton()
    {
        ResetAllUI();

        if (panel != null) panel.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);
        if (nextButton != null) nextButton.SetActive(false);
        if (mainImage != null) mainImage.SetActive(false);

        if (timeCnt != null)
        {
            timeCnt.ResetTimer();
        }

        currentStage++;
        if (currentStage == 2)
            SceneManager.LoadScene("BaseScene2");
        else if (currentStage == 3)
            SceneManager.LoadScene("ResultScene");
    }

    // --- スコアUIを更新 ---
    void UpdateScore()
    {
        int score = stageScore + totalScore;
        if (scoreText != null) scoreText.text = score.ToString();
    }

    // --- 現在装備中のアイテム画像（Sprite）を返す ---
    // 今プレイヤーが装備しているアイテムのスプライト画像を返す関数
    public Sprite GetEquippedSprite()
    {
        // アイテム画像配列が存在していて、装備IDも配列の範囲内なら
        if (itemSprites != null && equippedItemId >= 0 && equippedItemId < itemSprites.Length)
            // 装備中IDに対応するスプライトを返す
            return itemSprites[equippedItemId];

        // 条件を満たさなければ（装備なしや不正ID）はnullを返す
        return null;
    }


    // --- アイテムパネルが開いているか判定 ---
    public bool IsItemPanelOpen()
    {
        return itemDisplayPanel != null && itemDisplayPanel.activeSelf;
    }

    // --- シーン切り替え時のイベント登録 ---
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // --- シーン切り替え時のUI・状態リセット ---
    // 新しいシーンが読み込まれたときに呼ばれる
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetAllUI(); // まず全UIを初期化＆非表示

        // 復活判定フラグもリセット
        triedRevival = false;
        isReviving = false;

        cutInAudioSource = GameObject.Find("うめしばオーディオソース")?.GetComponent<AudioSource>();

        // --- UI要素をすべて非表示に ---
        if (itemDisplayPanel != null) itemDisplayPanel.SetActive(false); // アイテムパネル
        isItemPanelOpen = false;
        if (panel != null) panel.SetActive(false);            // ボタンパネル
        if (restartButton != null) restartButton.SetActive(false); // リスタートボタン
        if (nextButton != null) nextButton.SetActive(false);      // ネクストボタン
        if (mainImage != null) mainImage.SetActive(false);        // メイン画像

        // --- タイマーのリセット ---
        if (timeCnt == null) timeCnt = GetComponent<TimeController>(); // 取得し直し
        if (timeCnt != null)
        {
            timeCnt.ResetTimer();         // タイマー値初期化
            timeCnt.isTimeOver = false;   // タイマーを再開状態に
                                          // ショップシーンだけタイマー停止（ショップでは時間経過しない仕様）
            if (scene.name.Contains("Shop"))
                timeCnt.enabled = false;
            else
                timeCnt.enabled = true;
        }

        // --- リスタートから来た場合、プレイヤー復活＆UI再リンク ---
        if (fromRestart)
        {
            triedRevival = true; // 復活判定済みにする
            fromRestart = false; // フラグリセット

            // プレイヤー再取得
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj;
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    // HPバー再リンク（Scene遷移で一度切れることがあるため）
                    pc.hpBar = FindObjectOfType<HpBarController>();

                    // プレイヤー体力全回復＆UIも更新
                    pc.Heal(pc.maxHP);
                    pc.UpdateHpUI();

                    Debug.Log($"[Restart後] currentHP={pc.currentHP}, hpBar={(pc.hpBar == null ? "NULL" : "OK")}");
                }
            }

            // --- 復活演出CanvasやVideoPlayerも再取得・初期化 ---
            if (videoCanvas == null)
                videoCanvas = GameObject.Find("videoCanvas");
            videoCanvas?.SetActive(false); // ついてたら必ず非表示

            if (videoPlayer == null)
                videoPlayer = FindObjectOfType<UnityEngine.Video.VideoPlayer>();
            videoPlayer?.Stop();
            videoPlayer.frame = 0; // 再生位置を頭に戻す

            // ゲーム状態を必ず"playing"に戻す（復活完了！）
            PlayerController.gameState = "playing";
        }
    }


    // --- プレイヤー復活演出（動画再生など含む） ---
    // 復活用の動画を流して演出し、プレイヤーを復活させるコルーチン
    IEnumerator PlayRevivalSequence()
    {
        isReviving = true; // 演出中フラグON（同時多重実行防止）

        // プレイヤーや動画関連のオブジェクトを再取得
        player = GameObject.FindGameObjectWithTag("Player");
        if (videoCanvas == null)
            videoCanvas = GameObject.Find("VideoCanvas");
        if (videoPlayer == null)
            videoPlayer = FindObjectOfType<UnityEngine.Video.VideoPlayer>();

        // プレイヤーが見つからなければエラーで中断
        if (player == null)
        {
            Debug.LogError("復活演出時にplayerが見つからない！");
            yield break;
        }

        // --- 物理挙動とコライダーを一時停止、プレイヤー非表示 ---
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Collider2D col = player.GetComponent<Collider2D>();
        if (rb != null) rb.simulated = false;  // 物理停止
        if (col != null) col.enabled = false;  // 当たり判定OFF
        player.SetActive(false);               // 完全に非表示

        // --- 復活動画キャンバスを表示 ---
        if (videoCanvas != null) videoCanvas.SetActive(true);

        // --- 復活動画を再生 ---
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
                videoPlayer.Prepare();                 // 準備（非同期）
                while (!videoPlayer.isPrepared) yield return null; // 準備できるまで待つ
                videoPlayer.Play();                    // 再生開始
                while (videoPlayer.isPlaying) yield return null;   // 終わるまで待つ
            }
        }
        else
        {
            Debug.LogError("[復活] VideoPlayerが見つかりません！");
        }

        // --- 動画が終わったらキャンバスを非表示に ---
        if (videoCanvas != null) videoCanvas.SetActive(false);

        // --- プレイヤーを所定の位置に戻す（例：ステージ左端など）---
        player.transform.position = new Vector3(-8.97f, 0.0f, 0f);

        yield return new WaitForFixedUpdate(); // 物理処理安定用

        // --- プレイヤーの物理・コライダー・表示を復活 ---
        if (rb != null)
        {
            rb.velocity = Vector2.zero;   // 移動をリセット
            rb.simulated = true;          // 物理再開
        }
        if (col != null) col.enabled = true; // 当たり判定ON

        player.SetActive(true);
        yield return null; // 1フレーム待機（UI安定化）

        // --- スプライトを「復活時バージョン」に切り替え ---
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        if (sr != null && revivedSprite != null)
            sr.sprite = revivedSprite;

        // --- タイマー再開＆リスタートボタン非表示 ---
        if (timeCnt != null) timeCnt.isTimeOver = false;
        PlayerController.gameState = "playing";
        if (restartButton != null) restartButton.SetActive(false);

        // --- HP全回復・UI更新 ---
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.Heal(pc.maxHP);
            pc.UpdateHpUI();
        }

        // --- 復活アニメーションコントローラに切り替え ---
        var animator = player.GetComponent<Animator>();
        if (animator == null)
            Debug.LogWarning("[復活] animatorがnullです！");
        if (revivedOverrideController == null)
            Debug.LogWarning("[復活] revivedOverrideControllerがnullです！");

        if (animator != null && revivedOverrideController != null)
        {
            Debug.Log("[復活] 切り替え前 Controller名: " + animator.runtimeAnimatorController?.name);
            animator.runtimeAnimatorController = revivedOverrideController;
            Debug.Log("[復活] 切り替え後 Controller名: " + animator.runtimeAnimatorController?.name);
            yield return null; // コントローラ切り替え後は1フレーム待つ
            Debug.Log("[復活] 1フレーム後 Controller名: " + animator.runtimeAnimatorController?.name);
            animator.Play("RePlayerMove"); // 復活用アニメ再生
        }
        else
        {
            Debug.LogWarning("[復活] animator or revivedOverrideController がnullなので切り替えスキップ！");
        }

        isReviving = false; // 演出中フラグOFF
    }

    void ShowCutIn()
    {
        Debug.Log("ShowCutIn呼ばれた cutInImage=" + (cutInImage != null) + " railgunCutinSprite=" + (railgunCutinSprite != null));
        if (cutInImage != null && railgunCutinSprite != null)
        {
            cutInImage.sprite = railgunCutinSprite;
            cutInImage.gameObject.SetActive(true);
            Debug.Log("カットイン表示！！ Sprite名:" + railgunCutinSprite.name);
            Invoke(nameof(HideCutIn), 1.0f);
        }
        else
        {
            Debug.LogWarning("cutInImageまたはrailgunCutinSpriteが設定されてません！");
        }
    }

    // --- すべてのUIを非表示＆初期化 ---
    // ゲームリスタートやシーン切り替え時にUIを一度リセットするための関数
    void ResetAllUI()
    {
        // 復活演出用Canvasを非表示
        if (videoCanvas != null) videoCanvas.SetActive(false);

        // 動画プレイヤーがあれば停止＆再生位置リセット
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.frame = 0;
        }

        // メイン画像・パネル・ボタン類も全部非表示
        if (mainImage != null) mainImage.SetActive(false);       // 勝敗画像など
        if (panel != null) panel.SetActive(false);               // ボタンパネル
        if (restartButton != null) restartButton.SetActive(false); // リスタートボタン
        if (nextButton != null) nextButton.SetActive(false);     // ネクストボタン
                                                                 // 他にも非表示にしたいUIがあればここに追加
    }

    // --- レベルアップ演出を閉じる＆ゲーム再開 ---
    // レベルアップ演出中だけ一時停止していたゲーム進行を再開する関数
    public void CloseLevelUpPanel()
    {
        // レベルアップ用パネル（UI）を非表示
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        // 一時停止していたゲームを再開
        Time.timeScale = 1f;
    }


    void TogglePause()
    {
        bool willPause = Time.timeScale > 0f;
        Time.timeScale = willPause ? 0f : 1f;
        if (pausePanel != null)
            pausePanel.SetActive(willPause);
    }

}