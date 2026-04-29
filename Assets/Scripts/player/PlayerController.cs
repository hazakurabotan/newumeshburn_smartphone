using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    // === 基本 ===
    Rigidbody2D rbody;
    public PlayerShoot playerShootScript;
    public PlayerBombThrow playerBombThrowScript;
    public Joystick joystick;
    float axisH = 0f, axisV = 0f;

    public float speed = 3.0f;
    public float jump = 9.0f;
    public LayerMask groundLayer;
    bool goJump = false;

    // === ダッシュ ===
    public float dashSpeed = 6.0f;
    public float dashDoubleTapTime = 0.3f;
    float lastLeftTapTime = -1f;
    float lastRightTapTime = -1f;
    bool isDashing = false;
    float dashReleaseTimer = 0f;
    public float dashKeepTime = 0.3f;
    float prevAxisH = 0f;

    // === Railgun / Cut-in（Player管理） ===
    [Header("Railgun / Cut-in (Player)")]
    public UnityEngine.UI.Image cutInImage;
    public Sprite railgunCutinSprite;
    public AudioSource railgunAudioSource;
    public AudioClip railgunVoiceClip;
    public GameObject laserPrefab;

    // === 壁ジャンプ ===
    public float wallJumpPowerX = 7.0f;
    public float wallJumpPowerY = 12.0f;
    float wallJumpLockTimer = 0f;
    public float wallJumpLockTime = 0.2f;
    public Transform wallCheckLeft, wallCheckRight;
    public LayerMask wallLayer;
    public float wallCheckRadius = 0.2f;
    bool isTouchingWallLeft = false, isTouchingWallRight = false;
    bool isWallSliding = false;
    public float wallSlideSpeed = 2f;

    // === ビジュアル／無敵 ===
    SpriteRenderer sr;
    float invincibleTimer = 0f;
    public float invincibleTime = 3f;
    public GameObject afterImagePrefab;
    public float afterImageInterval = 0.05f;
    float afterImageTimer = 0f;

    // === UI/HP/サウンド ===
    public AudioSource audioSource;
    public AudioClip deathClip;
    public HpBarController hpBar;
    public int maxHP = 3;
    public int currentHP;
    public TextMeshProUGUI hpText;
    bool isDead = false;

    // === アニメ ===
    Animator animator;
    public string stopAnime = "PlayerStop";
    public string moveAnime = "PlayerMove";
    public string jumpAnime = "PlayerJump";
    public string goalAnime = "PlayerGoal";
    public string deadAnime = "PlayerOver";
    string oldAnime = "";
    public RuntimeAnimatorController PlayerAnime;

    // ★追加：ワンショットアニメ（撃つ・被弾）
    [Header("One-shot Animations")]
    public string shotAnime = "PlayerShot";
    public string damageAnime = "PlayerDamage";

    [Tooltip("Clipが見つからない時の保険（秒）")]
    public float shotLockFallback = 0.12f;
    public float damageLockFallback = 0.25f;

    float actionLockTimer = 0f;
    float shotLockTime = 0.12f;
    float damageLockTime = 0.25f;

    // === アイテム/ポーズ ===
    public GameObject itemPanelObj;
    public GameObject pausePanelObj;
    bool isItemPanelOpen = false;

    // === スコア/攻撃力 ===
    public int score = 0;
    public int bulletDamage = 1;

    // === ハシゴ ===
    bool onLadder = false;
    public float climbSpeed = 3f;

    // === ノックバック ===
    [Header("Knockback")]
    public Vector2 knockbackForce = new Vector2(7f, 4f);
    public float knockbackDuration = 0.2f;

    Vector2 knockbackVelocity = Vector2.zero;
    float knockbackTimer = 0f;

    // === ゲーム状態 ===
    public static string gameState = "playing";
    Vector3 respawnPosition;

    // === 追加: SE用 ===
    [Header("Shoot SE")]
    public AudioSource seSource;
    public AudioClip shootSE;

    public event System.Action<int, int> OnHpChanged;

    private PlayerInput _playerInput;
    private InputAction _railgunAction;
    private InputAction _summonAction;
    private InputAction _ropeAction;

    float normalGravity = 1f;

    [Header("Shoot Voices (Random)")]
    public AudioClip[] shootVoices;
    [Range(0f, 1f)] public float voiceVolume = 1f;
    public float voiceMinInterval = 0.05f;

    float lastVoiceTime = -999f;
    int lastVoiceIndex = -1;

    void PlayRandomShootVoice()
    {
        if (seSource == null) return;
        if (shootVoices == null || shootVoices.Length == 0) return;
        if (Time.time - lastVoiceTime < voiceMinInterval) return;

        int idx = UnityEngine.Random.Range(0, shootVoices.Length);

        if (shootVoices.Length >= 2 && idx == lastVoiceIndex)
            idx = (idx + 1) % shootVoices.Length;

        var clip = shootVoices[idx];
        if (clip == null) return;

        seSource.PlayOneShot(clip, voiceVolume);

        lastVoiceIndex = idx;
        lastVoiceTime = Time.time;
    }

    [Header("Damage Voice")]
    public AudioClip damageVoice;
    [Range(0f, 1f)] public float damageVoiceVolume = 1f;
    public float damageVoiceMinInterval = 0.2f;

    float lastDamageVoiceTime = -999f;

    void PlayDamageVoice()
    {
        if (seSource == null) return;
        if (damageVoice == null) return;
        if (Time.time - lastDamageVoiceTime < damageVoiceMinInterval) return;

        seSource.PlayOneShot(damageVoice, damageVoiceVolume);
        lastDamageVoiceTime = Time.time;
    }

    void Awake()
    {
        if (playerShootScript == null) playerShootScript = GetComponent<PlayerShoot>();
        if (playerBombThrowScript == null) playerBombThrowScript = GetComponent<PlayerBombThrow>();
        _playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        hpBar = FindObjectOfType<HpBarController>();
        currentHP = maxHP;
        hpBar?.SetHp(currentHP);
        UpdateHpUI();

        rbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        normalGravity = rbody.gravityScale;

        respawnPosition = transform.position;

        oldAnime = stopAnime;
        gameState = "playing";

        if (animator && animator.runtimeAnimatorController == null && PlayerAnime != null)
            animator.runtimeAnimatorController = PlayerAnime;

        if (seSource == null)
        {
            seSource = GetComponent<AudioSource>();
            if (seSource == null) seSource = gameObject.AddComponent<AudioSource>();
            seSource.playOnAwake = false;
            seSource.spatialBlend = 0f;
        }

        if (railgunAudioSource == null)
        {
            railgunAudioSource = GetComponent<AudioSource>();
            if (railgunAudioSource == null) railgunAudioSource = gameObject.AddComponent<AudioSource>();
            railgunAudioSource.playOnAwake = false;
            railgunAudioSource.spatialBlend = 0f;
        }

        shotLockTime = FindClipLength(shotAnime, shotLockFallback);
        damageLockTime = FindClipLength(damageAnime, damageLockFallback);
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (GameManager.Instance != null && GameManager.Instance.IsItemPanelOpen()) return;
        if (gameState != "playing") return;

        if (wallJumpLockTimer > 0f) wallJumpLockTimer -= Time.deltaTime;
        if (actionLockTimer > 0f) actionLockTimer -= Time.deltaTime;

        if (joystick != null && joystick.isActiveAndEnabled)
        {
            axisH = Mathf.Abs(joystick.Horizontal) > 0.1f ? joystick.Horizontal : 0f;
            axisV = Mathf.Abs(joystick.Vertical) > 0.1f ? joystick.Vertical : 0f;
        }

        if (isDashing && afterImagePrefab != null)
        {
            afterImageTimer -= Time.deltaTime;
            if (afterImageTimer <= 0f)
            {
                var obj = Instantiate(afterImagePrefab, transform.position, transform.rotation);
                var srr = obj.GetComponent<SpriteRenderer>();
                var mySr = GetComponent<SpriteRenderer>();
                if (srr && mySr)
                {
                    srr.sprite = mySr.sprite;
                    srr.flipX = mySr.flipX;
                    srr.color = new Color(1f, 1f, 1f, 0.5f);
                }
                afterImageTimer = afterImageInterval;
            }
        }
        else afterImageTimer = 0f;

        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        Vector2 origin = new Vector2(transform.position.x, col.bounds.min.y - 0.05f);
        bool onGround = Physics2D.OverlapCircle(origin, 0.1f, groundLayer);

        if (wallCheckLeft) isTouchingWallLeft = Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);
        if (wallCheckRight) isTouchingWallRight = Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);

        isWallSliding = false;
        if ((isTouchingWallLeft || isTouchingWallRight) && !onGround && axisH != 0 && wallJumpLockTimer <= 0f)
        {
            isWallSliding = true;
            rbody.velocity = new Vector2(rbody.velocity.x, -wallSlideSpeed);
        }

        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
        {
            DoJumpInput();
        }

        if (axisH > 0) transform.localScale = new Vector2(1, 1);
        else if (axisH < 0) transform.localScale = new Vector2(-1, 1);

        if (onLadder && transform.position.y >= 5.5f)
        {
            SceneManager.LoadScene("Stage2");
        }

        if (invincibleTimer > 0f) invincibleTimer -= Time.deltaTime;

        float now = Time.time;
        if (axisH > 0.5f && prevAxisH <= 0.5f)
        {
            if (now - lastRightTapTime < dashDoubleTapTime) { isDashing = true; dashReleaseTimer = dashKeepTime; }
            lastRightTapTime = now; lastLeftTapTime = -100f;
        }
        else if (axisH < -0.5f && prevAxisH >= -0.5f)
        {
            if (now - lastLeftTapTime < dashDoubleTapTime) { isDashing = true; dashReleaseTimer = dashKeepTime; }
            lastLeftTapTime = now; lastRightTapTime = -100f;
        }
        if (isDashing)
        {
            dashReleaseTimer -= Time.deltaTime;
            if (dashReleaseTimer <= 0f) isDashing = false;
        }
        if (Mathf.Abs(axisH) < 0.1f)
        {
            isDashing = false; dashReleaseTimer = 0f;
        }
        prevAxisH = axisH;
    }

    void LateUpdate()
    {
        var shoot = GetComponent<PlayerShoot>();
        if (invincibleTimer > 0f)
        {
            float blink = Mathf.Repeat(invincibleTimer * 10f, 1f);
            sr.color = (blink < 0.5f) ? Color.yellow : new Color(1, 1, 0, 0);
        }
        else if (shoot != null && shoot.isCharging)
        {
            float blink = Mathf.PingPong(Time.time * 3f, 1f);
            if (shoot.chargeTime >= shoot.requiredCharge)
                sr.color = (blink < 0.5f) ? Color.red : new Color(1f, 1f, 1f, 0.4f);
            else
                sr.color = (blink < 0.5f) ? Color.blue : new Color(1f, 1f, 1f, 0.4f);
        }
        else sr.color = Color.white;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (gameState != "playing") return;

        if (knockbackTimer > 0)
        {
            rbody.velocity = knockbackVelocity;
            knockbackTimer -= Time.fixedDeltaTime;
            return;
        }

        float inputX = axisH;

        if (wallJumpLockTimer > 0) { wallJumpLockTimer -= Time.fixedDeltaTime; inputX = 0; }
        else wallJumpLockTimer = 0;

        if (isWallSliding)
        {
            rbody.velocity = new Vector2(rbody.velocity.x, Mathf.Max(rbody.velocity.y, -wallSlideSpeed));
        }
        else
        {
            rbody.velocity = new Vector2(inputX * (isDashing ? dashSpeed : speed), rbody.velocity.y);
        }

        if (onLadder)
        {
            rbody.velocity = new Vector2(0, axisV * climbSpeed);
            rbody.gravityScale = 0f;
            return;
        }
        else
        {
            rbody.gravityScale = normalGravity;
        }

        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        Vector2 origin = new Vector2(transform.position.x, col.bounds.min.y - 0.05f);
        bool onGround = Physics2D.OverlapCircle(origin, 0.1f, groundLayer);

        if (onGround && goJump)
        {
            rbody.AddForce(Vector2.up * jump, ForceMode2D.Impulse);
            goJump = false;
        }

        if (actionLockTimer > 0f) return;

        string next = onGround ? (axisH == 0 ? stopAnime : moveAnime) : jumpAnime;
        if (next != oldAnime)
        {
            oldAnime = next;
            if (animator && animator.HasState(0, Animator.StringToHash(next)))
                animator.Play(next);
        }
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused)
        {
            axisH = 0; axisV = 0;
            return;
        }
        Vector2 move = ctx.ReadValue<Vector2>();
        axisH = move.x; axisV = move.y;
    }

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        DoJumpInput();
    }

    private void DoJumpInput()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        var col = GetComponent<CapsuleCollider2D>();
        Vector2 origin = new Vector2(transform.position.x, col.bounds.min.y - 0.05f);
        bool onGround = Physics2D.OverlapCircle(origin, 0.1f, groundLayer);

        if (!onGround && (isWallSliding || isTouchingWallLeft || isTouchingWallRight))
        {
            float dir;
            if (isTouchingWallRight && !isTouchingWallLeft) dir = -1f;
            else if (isTouchingWallLeft && !isTouchingWallRight) dir = 1f;
            else dir = (axisH >= 0) ? 1f : -1f;

            rbody.velocity = new Vector2(dir * wallJumpPowerX, wallJumpPowerY);
            wallJumpLockTimer = wallJumpLockTime;
            isWallSliding = false;

            transform.localScale = new Vector2(dir, 1f);
            transform.position += new Vector3(dir * 0.2f, 0f, 0f);
            return;
        }

        if (onGround) Jump();
    }

    public void OnShoot(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        if (playerShootScript == null) playerShootScript = GetComponent<PlayerShoot>();

        if (ctx.started)
        {
            playerShootScript.OnShootButtonDown();
            PlayRandomShootVoice();

            if (shootSE && seSource) seSource.PlayOneShot(shootSE);
        }

        if (ctx.canceled)
        {
            playerShootScript.OnShootButtonUp();
        }
    }

    public void OnPunch(InputAction.CallbackContext ctx) { }

    public void OnSummon(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (!ctx.started && !ctx.performed) return;

        if (playerBombThrowScript == null) playerBombThrowScript = GetComponent<PlayerBombThrow>();
        if (playerBombThrowScript == null) return;

        playerBombThrowScript.OnBombButtonDown();
    }

    public void OnRope(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (!ctx.started && !ctx.performed) return;

        if (playerBombThrowScript == null) playerBombThrowScript = GetComponent<PlayerBombThrow>();
        if (playerBombThrowScript == null) return;

        playerBombThrowScript.OnBombButtonDown();
    }

    public void SetHanging(bool hanging) { }
    public void OnRopeReturned() { }

    public void OnItemPanel(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (!ctx.performed) return;

        isItemPanelOpen = !isItemPanelOpen;
        if (itemPanelObj) itemPanelObj.SetActive(isItemPanelOpen);
        GameManager.Instance?.SetItemPanelOpen(isItemPanelOpen);
    }

    public void OnPause(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        GameManager.Instance?.TogglePausePublic();
    }

    public void OnSwitch(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        CharacterSwitchManager.RequestSwitch();
    }

    public void Jump() { goJump = true; }

    public void Goal()
    {
        if (animator) animator.Play(goalAnime);
        gameState = "gameclear";
    }

    public void GameOver()
    {
        if (animator) animator.Play(deadAnime);
        gameState = "gameover";
        GameStop();

        var cap = GetComponent<CapsuleCollider2D>();
        if (cap) cap.enabled = false;

        rbody.AddForce(Vector2.up * 5, ForceMode2D.Impulse);

        if (audioSource != null && deathClip != null)
            audioSource.PlayOneShot(deathClip);
    }

    void GameStop() { rbody.velocity = Vector2.zero; }

    public void TakeDamage(int damage)
    {
        float fallbackAttackerX = transform.position.x + ((transform.localScale.x >= 0) ? 1f : -1f);
        TakeDamage(damage, fallbackAttackerX);
    }

    public void TakeDamage(int damage, float attackerX)
    {
        if (gameState != "playing") return;
        if (invincibleTimer > 0f) return;
        if (isDead) return;

        ApplyKnockbackFromX(attackerX);

        currentHP = Mathf.Clamp(currentHP - damage, 0, maxHP);

        hpBar?.SetHp(currentHP);
        OnHpChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
        {
            isDead = true;
            GameOver();
            StartCoroutine(LoadResultAfterDelay(2.0f));
        }
        else
        {
            invincibleTimer = invincibleTime;
        }
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
        if (hpBar == null) hpBar = FindObjectOfType<HpBarController>();
        hpBar?.SetHp(currentHP);
    }

    public void UpdateHpUI()
    {
        if (hpBar == null)
        {
            hpBar = FindObjectOfType<HpBarController>();
            if (hpBar == null) { Debug.LogWarning("HpBarControllerが見つかりませんでした"); return; }
        }
        hpBar.SetHp(currentHP, maxHP);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Ladder")) onLadder = true;
        else if (col.CompareTag("Goal")) Goal();
        else if (col.CompareTag("Dead")) GameOver();
        else if (col.CompareTag("ScoreItem"))
        {
            var item = col.GetComponent<ItemData>();
            if (item != null) { score += item.value; Destroy(col.gameObject); }
        }
        else if (col.CompareTag("Enemy"))
        {
            knockbackTimer = knockbackDuration;
            TakeDamage(1, col.bounds.center.x);
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Ladder")) onLadder = false;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.CompareTag("Enemy"))
        {
            TakeDamage(1, col.collider.bounds.center.x);
        }
    }

    void ShowCutIn()
    {
        if (cutInImage != null && railgunCutinSprite != null)
        {
            cutInImage.sprite = railgunCutinSprite;
            cutInImage.gameObject.SetActive(true);
            Invoke(nameof(HideCutIn), 1.0f);
        }
        else
        {
            FireLaser();
        }

        if (railgunAudioSource != null && railgunVoiceClip != null)
            railgunAudioSource.PlayOneShot(railgunVoiceClip);
    }

    void HideCutIn()
    {
        if (cutInImage != null) cutInImage.gameObject.SetActive(false);
        FireLaser();
    }

    void FireLaser()
    {
        if (laserPrefab == null) return;

        Vector3 pos = transform.position;
        float dir = (transform.localScale.x >= 0) ? 1f : -1f;
        float length = 1.3f;

        GameObject laserObj = Instantiate(laserPrefab, pos + new Vector3(5f * dir, 0, 0), Quaternion.identity);

        Vector3 scale = laserObj.transform.localScale;
        scale.x = Mathf.Abs(length) * dir;
        laserObj.transform.localScale = scale;

        laserObj.transform.position += new Vector3((length / 2f) * dir, 0, 0);

        Destroy(laserObj, 0.6f);
    }

    public void OnRailgun(InputAction.CallbackContext ctx)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;
        if (!ctx.performed) return;
        ShowCutIn();
    }

    void OnEnable()
    {
        if (_playerInput != null && _playerInput.actions != null)
        {
            _railgunAction = _playerInput.actions.FindAction("Railgun");
            if (_railgunAction != null)
            {
                _railgunAction.performed -= OnRailgun;
                _railgunAction.performed += OnRailgun;
            }

            _summonAction = _playerInput.actions.FindAction("Summon");
            if (_summonAction != null)
            {
                _summonAction.started -= OnSummon;
                _summonAction.performed -= OnSummon;
                _summonAction.started += OnSummon;
                _summonAction.performed += OnSummon;
            }

            _ropeAction = _playerInput.actions.FindAction("Rope");
            if (_ropeAction != null)
            {
                _ropeAction.started -= OnRope;
                _ropeAction.performed -= OnRope;
                _ropeAction.started += OnRope;
                _ropeAction.performed += OnRope;
            }
            else
            {
                Debug.LogWarning("PlayerControls に Rope アクションが見つかりません。");
            }
        }
    }

    void OnDisable()
    {
        if (_railgunAction != null)
        {
            _railgunAction.performed -= OnRailgun;
        }

        if (_summonAction != null)
        {
            _summonAction.started -= OnSummon;
            _summonAction.performed -= OnSummon;
        }

        if (_ropeAction != null)
        {
            _ropeAction.started -= OnRope;
            _ropeAction.performed -= OnRope;
        }
    }

    public void RespawnAtStartPosition()
    {
        transform.position = respawnPosition;

        rbody.velocity = Vector2.zero;
        gameState = "playing";
        isDead = false;

        currentHP = maxHP;
        hpBar?.SetHp(currentHP);
        OnHpChanged?.Invoke(currentHP, maxHP);

        var col = GetComponent<CapsuleCollider2D>();
        if (col) col.enabled = true;

        invincibleTimer = invincibleTime;
    }

    IEnumerator LoadResultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("Resuit");
    }

    void PlayOneShotAnim(string stateName, float lockTime)
    {
        if (!animator) return;
        if (string.IsNullOrEmpty(stateName)) return;

        int hash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, hash)) return;

        animator.Play(stateName, 0, 0f);
        actionLockTimer = Mathf.Max(0.01f, lockTime);
    }

    float FindClipLength(string clipName, float fallback)
    {
        if (!animator || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(clipName))
            return fallback;

        var clips = animator.runtimeAnimatorController.animationClips;
        foreach (var c in clips)
        {
            if (c != null && c.name == clipName)
                return Mathf.Max(0.01f, c.length);
        }
        return fallback;
    }

    void ApplyKnockbackFromX(float attackerX)
    {
        float dir = (transform.position.x < attackerX) ? -1f : 1f;
        knockbackVelocity = new Vector2(dir * Mathf.Abs(knockbackForce.x), Mathf.Abs(knockbackForce.y));
        knockbackTimer = knockbackDuration;
    }
}