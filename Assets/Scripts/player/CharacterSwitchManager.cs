using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class CharacterSwitchManager : MonoBehaviour
{
    [Header("キャラ参照 (自動検出可)")]
    public MawaruController mawaru;
    public PlayerController player;

    [Header("入力 (PlayerInput)")]
    public PlayerInput mawaruInput;
    public PlayerInput playerInput;

    [Header("カメラ追従")]
    public FollowTarget2D simpleCam;   // 使っていれば
    public Camera mainCam;             // 使っていなければ null でOK

    public static CharacterSwitchManager Instance;


    [Header("HPバー")]
    [SerializeField] HpBarController hpBar;  // ← インスペクタで Canvas/HpPanel のやつを割り当て


    [Header("開始時にMawaruを操作")]
    public bool startAsMawaru = true;

    [Header("その場スイッチの挙動")]
    public bool snapAtSwitch = true;         // 入れ替え時に位置をコピーする
    public bool copyVelocity = true;         // 速度も引き継ぐ
    public float zKeepTo = 0f;               // 受け手のZを固定したい場合（2Dなら0が多い）


    public ActiveHpBinder hpBinder;
    private bool isMawaruActive;

    public static Transform ActiveTarget { get; private set; }
    public static bool IsMawaruActive => Instance && Instance.isMawaruActive;

    void Awake()
    {
        Instance = this;

        // 自動検出
        if (!mawaru) mawaru = FindObjectOfType<MawaruController>(true);
        if (!player) player = FindObjectOfType<PlayerController>(true);
        if (!mawaruInput && mawaru) mawaruInput = mawaru.GetComponent<PlayerInput>();
        if (!playerInput && player) playerInput = player.GetComponent<PlayerInput>();
        if (!mainCam) mainCam = Camera.main;
    }

    void Start()
    {
        ApplyControl(startAsMawaru);
    }

    // === PlayerInput(Invoke Unity Events) から結ぶコールバック ===
    public void OnSwitch(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        RequestSwitch(); // 共通エントリ
    }

    // どちらのコントローラから呼ばれてもOK
    public static void RequestSwitch()
    {
        if (Instance != null) Instance.SwitchHere();
        else Debug.LogWarning("[CharacterSwitchManager] Instance is null");
    }

    /// <summary>
    /// 現在位置で入れ替え。snapAtSwitch=false の場合は単なる切替。
    /// </summary>
    private void SwitchHere()
    {
        if (snapAtSwitch && mawaru && player)
        {
            Transform from = isMawaruActive ? mawaru.transform : player.transform;
            Transform to = isMawaruActive ? player.transform : mawaru.transform;

            SetCollidersEnabled(from, false); // ← 修正：操作してた側を無効化！

            Vector3 p = from.position;
            to.position = new Vector3(p.x, p.y, zKeepTo);

            if (copyVelocity)
            {
                var rbFrom = from.GetComponent<Rigidbody2D>();
                var rbTo = to.GetComponent<Rigidbody2D>();
                if (rbFrom && rbTo)
                {
                    rbTo.velocity = rbFrom.velocity;
                    rbTo.angularVelocity = 0f;
                }
            }

            // === 向きをコピー（ここが修正点） ===
            bool fromFacingLeft;
            if (isMawaruActive)
            {
                // mawaru → player
                fromFacingLeft = mawaru.GetComponentInChildren<SpriteRenderer>(true).flipX;
                player.transform.localScale = new Vector3(fromFacingLeft ? -1f : 1f, 1f, 1f);
            }
            else
            {
                // player → mawaru
                fromFacingLeft = player.transform.localScale.x < 0f;
                mawaru.GetComponentInChildren<SpriteRenderer>(true).flipX = fromFacingLeft;
            }
            // ================================

            SetCollidersEnabled(to, true);
        }

        ToggleControl();
    }

    public void ToggleControl()
    {
        ApplyControl(!isMawaruActive);
    }

    private void ApplyControl(bool controlMawaru)
    {
        // 切り替え前に購読解除（多重登録防止）
        if (mawaru) mawaru.OnHpChanged -= UpdateHpBar;
        if (player) player.OnHpChanged -= UpdateHpBar;

        isMawaruActive = controlMawaru;

        // 入力の有効/無効
        if (mawaruInput) mawaruInput.enabled = controlMawaru;
        if (playerInput) playerInput.enabled = !controlMawaru;

        // 表示切替（操作されていない側は非表示）
        if (mawaru) foreach (var sr in mawaru.GetComponentsInChildren<SpriteRenderer>(true))
                sr.enabled = controlMawaru;
        if (player) foreach (var sr in player.GetComponentsInChildren<SpriteRenderer>(true))
                sr.enabled = !controlMawaru;

        // カメラ追従のターゲット更新
        Transform target = controlMawaru ? mawaru?.transform : player?.transform;

        if (simpleCam)
        {
            simpleCam.target = target; // FollowTarget2D を使ってる場合
        }
        else if (mainCam)
        {
            // Bounds版（いまMainCameraについてるのはこっち）
            var followBounds = mainCam.GetComponent<FollowTarget2D_Bounds>();
            if (followBounds) followBounds.target = target;

            // 旧simple版も一応両対応
            var follow = mainCam.GetComponent<FollowTarget2D>();
            if (follow) follow.target = target;
        }

        Debug.Log($"🎮 [CharacterSwitchManager] 切替: {(controlMawaru ? "Mawaru" : "Player")}");

        if (hpBar)
        {
            if (controlMawaru && mawaru)
            {
                mawaru.OnHpChanged += UpdateHpBar;
                hpBar.SetHp(mawaru.currentHP, mawaru.maxHP);   // 現在値を即表示
            }
            else if (player)
            {
                player.OnHpChanged += UpdateHpBar;
                hpBar.SetHp(player.currentHP, player.maxHP);   // 現在値を即表示
            }
        }

        // ← これを追加：UIを操作キャラにバインド
        if (hpBinder) hpBinder.Bind(controlMawaru);


        Debug.Log($"🎮 [CharacterSwitchManager] 切替: {(controlMawaru ? "Mawaru" : "Player")}");

        // ←これを最後に入れる
        ActiveTarget = controlMawaru ? mawaru.transform : player.transform;


    }


    // --- Helpers ---
    void SetCollidersEnabled(Component root, bool enabled)
    {
        if (!root) return;
        foreach (var c in root.GetComponentsInChildren<Collider2D>(true))
            c.enabled = enabled;
    }

    private void UpdateHpBar(int cur, int max)   // ← 追加
    {
        if (hpBar) hpBar.SetHp(cur, max);
    }



}
