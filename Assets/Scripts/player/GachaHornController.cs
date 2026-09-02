using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GachaHornController : MonoBehaviour
{
    public enum GachaState
    {
        Idle,
        Charging,
        ReadyToRotate,
        Rotating,
        RewardDisplaying
    }

    public enum RewardCategory
    {
        MD,
        Heal,
        Sticker
    }

    private enum Direction4
    {
        None,
        Up,
        Right,
        Down,
        Left
    }

    [Serializable]
    public class MdRewardEntry
    {
        public string id;
        public string title;
        [TextArea(2, 5)] public string effectText;
        public Sprite icon;
    }

    [Serializable]
    public class HealRewardEntry
    {
        public string id;
        public string displayName;
        public Sprite icon;
    }

    [Serializable]
    public class StickerRewardEntry
    {
        public string id;
        public string displayName;
        public Sprite icon;
    }

    [Header("Use Conditions")]
    [Tooltip("prayer（ウメロイド）操作中だけ true にしてください。")]
    [SerializeField] private bool isPrayerActiveCharacter = true;

    [Tooltip("ここに入れたシーン名ではガチャホーンを使いません。例: StageSelect")]
    [SerializeField] private string[] blockedSceneNames = { "StageSelect" };

    [Header("Coin Settings")]
    [SerializeField] private int requiredCoins = 10;

    [Header("Rates (%)")]
    [SerializeField, Range(0f, 100f)] private float mdRate = 20f;
    [SerializeField, Range(0f, 100f)] private float healRate = 50f;
    [SerializeField, Range(0f, 100f)] private float stickerRate = 30f;

    [Header("MD Settings")]
    [SerializeField] private int maxMdEnhanceLevel = 3;

    [Header("Reward Tables")]
    [SerializeField] private List<MdRewardEntry> mdRewards = new List<MdRewardEntry>();
    [SerializeField] private List<HealRewardEntry> healRewards = new List<HealRewardEntry>();
    [SerializeField] private List<StickerRewardEntry> stickerRewards = new List<StickerRewardEntry>();

    [Header("Input")]
    [SerializeField] private bool enableKeyboardDebugInput = false;

    [Header("UI - Counters / Guide")]
    [SerializeField] private TextMeshProUGUI ownedCoinsText;
    [SerializeField] private TextMeshProUGUI insertedCoinsText;
    [SerializeField] private TextMeshProUGUI guideText;

    [Header("UI - Head Icon")]
    [SerializeField] private Transform playerHeadAnchor;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform rewardHeadIconRoot;
    [SerializeField] private Image rewardHeadIconImage;
    [SerializeField] private Vector3 rewardHeadWorldOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private float rewardHeadIconDuration = 1.5f;

    [Header("UI - Popup Panel")]
    [SerializeField] private GameObject popupPanelRoot;
    [SerializeField] private Image popupIconImage;
    [SerializeField] private TextMeshProUGUI popupCategoryText;
    [SerializeField] private TextMeshProUGUI popupTitleText;
    [SerializeField] private TextMeshProUGUI popupBodyText;
    [SerializeField] private TextMeshProUGUI popupStatusText;
    [SerializeField] private float popupDisplayDuration = 2.0f;

    [Header("Glow")]
    [SerializeField] private Animator glowAnimator;
    [SerializeField] private string glowTriggerName = "Play";
    [SerializeField] private Graphic[] glowGraphicTargets;
    [SerializeField] private SpriteRenderer[] glowSpriteTargets;
    [SerializeField] private RectTransform glowScaleTarget;
    [SerializeField] private Color glowColor = new Color(1f, 0.95f, 0.45f, 1f);
    [SerializeField] private int glowBlinkCount = 6;
    [SerializeField] private float glowBlinkInterval = 0.08f;
    [SerializeField] private float glowScaleMultiplier = 1.08f;

    [Header("Animators")]
    [SerializeField] private Animator coinInsertAnimator;
    [SerializeField] private string coinInsertTriggerName = "Play";

    [SerializeField] private Animator machineAnimator;
    [SerializeField] private string machineSpinTriggerName = "Spin";

    [SerializeField] private Animator capsuleAnimator;
    [SerializeField] private string capsuleEjectTriggerName = "Eject";
    [SerializeField] private string capsuleOpenTriggerName = "Open";

    [Header("Timing")]
    [SerializeField] private float rotateAnimationDuration = 0.60f;
    [SerializeField] private float capsuleEjectDuration = 0.45f;
    [SerializeField] private float capsuleOpenDuration = 0.40f;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip coinInsertClip;

    private readonly Direction4[] clockwiseSequence = { Direction4.Up, Direction4.Right, Direction4.Down, Direction4.Left };
    private readonly Dictionary<string, int> mdLevels = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> healCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private readonly Dictionary<string, int> stickerCounts = new Dictionary<string, int>(StringComparer.Ordinal);

    private GachaState currentState = GachaState.Idle;
    private int insertedCoins = 0;
    private int rotateStepIndex = 0;
    private bool isSequenceRunning = false;

    private Coroutine glowRoutine;
    private Color[] glowGraphicOriginalColors;
    private Color[] glowSpriteOriginalColors;
    private Vector3 glowOriginalScale = Vector3.one;

    private bool isRewardHeadIconShowing = false;
    private float rewardHeadIconHideTime = 0f;

    private void Awake()
    {
        CacheGlowOriginalValues();
        HidePopupPanel();
        HideRewardHeadIcon();
        RefreshAllUi();
    }

    private void Update()
    {
        UpdateRewardHeadIconPosition();

        if (!CanReceiveInput())
        {
            return;
        }

        if (!isSequenceRunning && (currentState == GachaState.Idle || currentState == GachaState.Charging))
        {
            if (WasInsertCoinPressedThisFrame())
            {
                TryInsertCoin();
            }
        }

        if (!isSequenceRunning && currentState == GachaState.ReadyToRotate)
        {
            Direction4 direction = ReadPressedDirectionThisFrame();
            if (direction != Direction4.None)
            {
                ProcessClockwiseInput(direction);
            }
        }
    }

    private void OnValidate()
    {
        requiredCoins = Mathf.Max(1, requiredCoins);
        maxMdEnhanceLevel = Mathf.Max(1, maxMdEnhanceLevel);
        glowBlinkCount = Mathf.Max(1, glowBlinkCount);
        glowBlinkInterval = Mathf.Max(0.01f, glowBlinkInterval);
        glowScaleMultiplier = Mathf.Max(1f, glowScaleMultiplier);
        rewardHeadIconDuration = Mathf.Max(0.1f, rewardHeadIconDuration);
        popupDisplayDuration = Mathf.Max(0.1f, popupDisplayDuration);
        rotateAnimationDuration = Mathf.Max(0f, rotateAnimationDuration);
        capsuleEjectDuration = Mathf.Max(0f, capsuleEjectDuration);
        capsuleOpenDuration = Mathf.Max(0f, capsuleOpenDuration);
    }

    public void SetPrayerActiveCharacter(bool value)
    {
        isPrayerActiveCharacter = value;
        RefreshGuideText();
    }

    public void ResetCharge()
    {
        insertedCoins = 0;
        rotateStepIndex = 0;
        currentState = GachaState.Idle;
        isSequenceRunning = false;

        StopGlowRoutineIfRunning();
        RestoreGlowTargets();

        HidePopupPanel();
        HideRewardHeadIcon();
        RefreshAllUi();
    }

    public int GetInsertedCoins()
    {
        return insertedCoins;
    }

    public GachaState GetCurrentState()
    {
        return currentState;
    }

    public int GetMdLevel(string mdId)
    {
        if (string.IsNullOrWhiteSpace(mdId))
        {
            return -1;
        }

        return mdLevels.TryGetValue(mdId, out int level) ? level : -1;
    }

    private bool CanReceiveInput()
    {
        if (!isPrayerActiveCharacter)
        {
            return false;
        }

        if (IsBlockedScene(SceneManager.GetActiveScene().name))
        {
            return false;
        }

        return true;
    }

    private bool IsBlockedScene(string sceneName)
    {
        if (blockedSceneNames == null || blockedSceneNames.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < blockedSceneNames.Length; i++)
        {
            if (string.Equals(blockedSceneNames[i], sceneName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool WasInsertCoinPressedThisFrame()
    {
        // スクショどおり Right Trigger を使う
        bool gamepadPressed = Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame;
        bool keyboardPressed = enableKeyboardDebugInput && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        return gamepadPressed || keyboardPressed;
    }

    private Direction4 ReadPressedDirectionThisFrame()
    {
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame) return Direction4.Up;
            if (Gamepad.current.dpad.right.wasPressedThisFrame) return Direction4.Right;
            if (Gamepad.current.dpad.down.wasPressedThisFrame) return Direction4.Down;
            if (Gamepad.current.dpad.left.wasPressedThisFrame) return Direction4.Left;
        }

        if (enableKeyboardDebugInput && Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame) return Direction4.Up;
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame) return Direction4.Right;
            if (Keyboard.current.downArrowKey.wasPressedThisFrame) return Direction4.Down;
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame) return Direction4.Left;
        }

        return Direction4.None;
    }

    private void TryInsertCoin()
    {
        if (insertedCoins >= requiredCoins)
        {
            return;
        }

        if (!TrySpendWalletCoins(1))
        {
            return;
        }

        insertedCoins++;
        rotateStepIndex = 0;
        currentState = insertedCoins >= requiredCoins ? GachaState.ReadyToRotate : GachaState.Charging;

        PlayCoinInsertEffects();
        RefreshAllUi();

        if (insertedCoins >= requiredCoins)
        {
            PlayGlowEffect();
        }
    }

    private void PlayCoinInsertEffects()
    {
        if (sfxSource != null && coinInsertClip != null)
        {
            sfxSource.PlayOneShot(coinInsertClip);
        }

        PlayAnimatorTrigger(coinInsertAnimator, coinInsertTriggerName);
    }

    private void PlayGlowEffect()
    {
        PlayAnimatorTrigger(glowAnimator, glowTriggerName);

        StopGlowRoutineIfRunning();
        glowRoutine = StartCoroutine(GlowRoutine());
    }

    private IEnumerator GlowRoutine()
    {
        for (int i = 0; i < glowBlinkCount; i++)
        {
            SetGlowTargets(true);
            yield return new WaitForSeconds(glowBlinkInterval);
            SetGlowTargets(false);
            yield return new WaitForSeconds(glowBlinkInterval);
        }

        RestoreGlowTargets();
        glowRoutine = null;
    }

    private void StopGlowRoutineIfRunning()
    {
        if (glowRoutine != null)
        {
            StopCoroutine(glowRoutine);
            glowRoutine = null;
        }
    }

    private void CacheGlowOriginalValues()
    {
        if (glowGraphicTargets != null)
        {
            glowGraphicOriginalColors = new Color[glowGraphicTargets.Length];
            for (int i = 0; i < glowGraphicTargets.Length; i++)
            {
                glowGraphicOriginalColors[i] = glowGraphicTargets[i] != null ? glowGraphicTargets[i].color : Color.white;
            }
        }

        if (glowSpriteTargets != null)
        {
            glowSpriteOriginalColors = new Color[glowSpriteTargets.Length];
            for (int i = 0; i < glowSpriteTargets.Length; i++)
            {
                glowSpriteOriginalColors[i] = glowSpriteTargets[i] != null ? glowSpriteTargets[i].color : Color.white;
            }
        }

        if (glowScaleTarget != null)
        {
            glowOriginalScale = glowScaleTarget.localScale;
        }
    }

    private void SetGlowTargets(bool glowOn)
    {
        if (glowGraphicTargets != null)
        {
            for (int i = 0; i < glowGraphicTargets.Length; i++)
            {
                if (glowGraphicTargets[i] == null) continue;
                glowGraphicTargets[i].color = glowOn ? glowColor : glowGraphicOriginalColors[i];
            }
        }

        if (glowSpriteTargets != null)
        {
            for (int i = 0; i < glowSpriteTargets.Length; i++)
            {
                if (glowSpriteTargets[i] == null) continue;
                glowSpriteTargets[i].color = glowOn ? glowColor : glowSpriteOriginalColors[i];
            }
        }

        if (glowScaleTarget != null)
        {
            glowScaleTarget.localScale = glowOn ? glowOriginalScale * glowScaleMultiplier : glowOriginalScale;
        }
    }

    private void RestoreGlowTargets()
    {
        if (glowGraphicTargets != null)
        {
            for (int i = 0; i < glowGraphicTargets.Length; i++)
            {
                if (glowGraphicTargets[i] == null) continue;
                glowGraphicTargets[i].color = glowGraphicOriginalColors != null && i < glowGraphicOriginalColors.Length
                    ? glowGraphicOriginalColors[i]
                    : Color.white;
            }
        }

        if (glowSpriteTargets != null)
        {
            for (int i = 0; i < glowSpriteTargets.Length; i++)
            {
                if (glowSpriteTargets[i] == null) continue;
                glowSpriteTargets[i].color = glowSpriteOriginalColors != null && i < glowSpriteOriginalColors.Length
                    ? glowSpriteOriginalColors[i]
                    : Color.white;
            }
        }

        if (glowScaleTarget != null)
        {
            glowScaleTarget.localScale = glowOriginalScale;
        }
    }

    private void ProcessClockwiseInput(Direction4 direction)
    {
        Direction4 expected = clockwiseSequence[rotateStepIndex];

        if (direction == expected)
        {
            rotateStepIndex++;
            RefreshGuideText();

            if (rotateStepIndex >= clockwiseSequence.Length)
            {
                StartCoroutine(RunGachaSequence());
            }

            return;
        }

        rotateStepIndex = direction == Direction4.Up ? 1 : 0;
        RefreshGuideText();
    }

    private IEnumerator RunGachaSequence()
    {
        isSequenceRunning = true;
        currentState = GachaState.Rotating;
        RefreshGuideText();

        StopGlowRoutineIfRunning();
        RestoreGlowTargets();

        PlayAnimatorTrigger(machineAnimator, machineSpinTriggerName);
        if (rotateAnimationDuration > 0f)
        {
            yield return new WaitForSeconds(rotateAnimationDuration);
        }

        PlayAnimatorTrigger(capsuleAnimator, capsuleEjectTriggerName);
        if (capsuleEjectDuration > 0f)
        {
            yield return new WaitForSeconds(capsuleEjectDuration);
        }

        PlayAnimatorTrigger(capsuleAnimator, capsuleOpenTriggerName);
        if (capsuleOpenDuration > 0f)
        {
            yield return new WaitForSeconds(capsuleOpenDuration);
        }

        RewardResult reward = ResolveReward();

        ShowRewardHeadIcon(reward.icon);
        bool popupShown = ShowPopupForReward(reward);

        float waitTime = rewardHeadIconDuration;
        if (popupShown)
        {
            waitTime = Mathf.Max(waitTime, popupDisplayDuration);
        }

        currentState = GachaState.RewardDisplaying;
        RefreshGuideText();

        yield return new WaitForSeconds(waitTime);

        HidePopupPanel();
        HideRewardHeadIcon();

        insertedCoins = 0;
        rotateStepIndex = 0;
        isSequenceRunning = false;
        currentState = GachaState.Idle;
        RefreshAllUi();
    }

    private RewardResult ResolveReward()
    {
        RewardCategory category = PickRewardCategory();

        switch (category)
        {
            case RewardCategory.MD:
                return ResolveMdReward();
            case RewardCategory.Heal:
                return ResolveHealReward();
            default:
                return ResolveStickerReward();
        }
    }

    private RewardCategory PickRewardCategory()
    {
        float availableMdRate = mdRewards.Count > 0 ? mdRate : 0f;
        float availableHealRate = healRewards.Count > 0 ? healRate : 0f;
        float availableStickerRate = stickerRewards.Count > 0 ? stickerRate : 0f;

        float totalRate = availableMdRate + availableHealRate + availableStickerRate;
        if (totalRate <= 0f)
        {
            throw new InvalidOperationException("GachaHornController: Reward tables are empty.");
        }

        float roll = UnityEngine.Random.Range(0f, totalRate);

        if (roll < availableMdRate)
        {
            return RewardCategory.MD;
        }

        roll -= availableMdRate;
        if (roll < availableHealRate)
        {
            return RewardCategory.Heal;
        }

        return RewardCategory.Sticker;
    }

    private RewardResult ResolveMdReward()
    {
        MdRewardEntry entry = GetRandomEntry(mdRewards);
        if (entry == null)
        {
            return ResolveHealReward();
        }

        RewardResult result = new RewardResult
        {
            category = RewardCategory.MD,
            id = entry.id,
            title = string.IsNullOrWhiteSpace(entry.title) ? entry.id : entry.title,
            body = entry.effectText,
            icon = entry.icon,
            status = "MD獲得"
        };

        if (!mdLevels.TryGetValue(entry.id, out int currentLevel))
        {
            mdLevels[entry.id] = 0;
            result.mdLevelAfter = 0;
            result.wasDuplicateMd = false;
            return result;
        }

        if (currentLevel < maxMdEnhanceLevel)
        {
            currentLevel++;
            mdLevels[entry.id] = currentLevel;
            result.mdLevelAfter = currentLevel;
            result.wasDuplicateMd = true;
            result.status = "MD強化 +" + currentLevel;
            return result;
        }

        AddWalletCoins(1);
        result.mdLevelAfter = currentLevel;
        result.wasDuplicateMd = true;
        result.refundedCoins = 1;
        result.status = "強化上限のためコイン1枚返還";
        return result;
    }

    private RewardResult ResolveHealReward()
    {
        HealRewardEntry entry = GetRandomEntry(healRewards);
        if (entry == null)
        {
            return ResolveStickerReward();
        }

        if (!healCounts.ContainsKey(entry.id))
        {
            healCounts[entry.id] = 0;
        }

        healCounts[entry.id]++;

        return new RewardResult
        {
            category = RewardCategory.Heal,
            id = entry.id,
            title = string.IsNullOrWhiteSpace(entry.displayName) ? entry.id : entry.displayName,
            body = string.Empty,
            icon = entry.icon,
            status = "回復アイテム獲得"
        };
    }

    private RewardResult ResolveStickerReward()
    {
        StickerRewardEntry entry = GetRandomEntry(stickerRewards);
        if (entry == null)
        {
            return ResolveMdReward();
        }

        if (!stickerCounts.ContainsKey(entry.id))
        {
            stickerCounts[entry.id] = 0;
        }

        stickerCounts[entry.id]++;

        return new RewardResult
        {
            category = RewardCategory.Sticker,
            id = entry.id,
            title = string.IsNullOrWhiteSpace(entry.displayName) ? entry.id : entry.displayName,
            body = string.Empty,
            icon = entry.icon,
            status = "ステッカー獲得"
        };
    }

    private T GetRandomEntry<T>(List<T> list) where T : class
    {
        if (list == null || list.Count == 0)
        {
            return null;
        }

        int index = UnityEngine.Random.Range(0, list.Count);
        return list[index];
    }

    private bool ShowPopupForReward(RewardResult reward)
    {
        if (popupPanelRoot == null)
        {
            return false;
        }

        if (reward.category == RewardCategory.Heal)
        {
            HidePopupPanel();
            return false;
        }

        popupPanelRoot.SetActive(true);

        if (popupIconImage != null)
        {
            popupIconImage.sprite = reward.icon;
            popupIconImage.enabled = reward.icon != null;
        }

        if (popupCategoryText != null)
        {
            popupCategoryText.text = reward.category == RewardCategory.MD ? "MD" : "STICKER";
        }

        if (popupTitleText != null)
        {
            popupTitleText.text = reward.title;
        }

        if (popupBodyText != null)
        {
            popupBodyText.text = reward.category == RewardCategory.MD ? reward.body : string.Empty;
        }

        if (popupStatusText != null)
        {
            popupStatusText.text = reward.status;
        }

        return true;
    }

    private void HidePopupPanel()
    {
        if (popupPanelRoot != null)
        {
            popupPanelRoot.SetActive(false);
        }

        if (popupIconImage != null)
        {
            popupIconImage.sprite = null;
            popupIconImage.enabled = false;
        }

        if (popupCategoryText != null)
        {
            popupCategoryText.text = string.Empty;
        }

        if (popupTitleText != null)
        {
            popupTitleText.text = string.Empty;
        }

        if (popupBodyText != null)
        {
            popupBodyText.text = string.Empty;
        }

        if (popupStatusText != null)
        {
            popupStatusText.text = string.Empty;
        }
    }

    private void ShowRewardHeadIcon(Sprite icon)
    {
        if (rewardHeadIconRoot == null || rewardHeadIconImage == null || icon == null)
        {
            return;
        }

        rewardHeadIconImage.sprite = icon;
        rewardHeadIconImage.enabled = true;
        rewardHeadIconRoot.gameObject.SetActive(true);

        isRewardHeadIconShowing = true;
        rewardHeadIconHideTime = Time.time + rewardHeadIconDuration;

        UpdateRewardHeadIconPosition();
    }

    private void HideRewardHeadIcon()
    {
        isRewardHeadIconShowing = false;
        rewardHeadIconHideTime = 0f;

        if (rewardHeadIconRoot != null)
        {
            rewardHeadIconRoot.gameObject.SetActive(false);
        }

        if (rewardHeadIconImage != null)
        {
            rewardHeadIconImage.sprite = null;
            rewardHeadIconImage.enabled = false;
        }
    }

    private void UpdateRewardHeadIconPosition()
    {
        if (!isRewardHeadIconShowing)
        {
            return;
        }

        if (Time.time >= rewardHeadIconHideTime)
        {
            HideRewardHeadIcon();
            return;
        }

        if (playerHeadAnchor == null || rewardHeadIconRoot == null || uiCanvas == null)
        {
            return;
        }

        Camera worldCam = worldCamera != null ? worldCamera : Camera.main;
        if (worldCam == null)
        {
            return;
        }

        Vector3 worldPos = playerHeadAnchor.position + rewardHeadWorldOffset;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(worldCam, worldPos);

        RectTransform parentRect = rewardHeadIconRoot.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        Camera uiCam = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCam, out Vector2 localPoint))
        {
            rewardHeadIconRoot.anchoredPosition = localPoint;
        }
    }

    private void PlayAnimatorTrigger(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
        {
            return;
        }

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    private void RefreshAllUi()
    {
        RefreshOwnedCoinsUi();
        RefreshInsertedCoinsUi();
        RefreshGuideText();
    }

    private void RefreshOwnedCoinsUi()
    {
        if (ownedCoinsText != null)
        {
            ownedCoinsText.text = GetWalletCoins().ToString();
        }
    }

    private void RefreshInsertedCoinsUi()
    {
        if (insertedCoinsText != null)
        {
            insertedCoinsText.text = insertedCoins + " / " + requiredCoins;
        }
    }

    private void RefreshGuideText()
    {
        if (guideText == null)
        {
            return;
        }

        if (!CanReceiveInput())
        {
            guideText.text = string.Empty;
            return;
        }

        switch (currentState)
        {
            case GachaState.Idle:
            case GachaState.Charging:
                guideText.text = "RTでコイン投入";
                break;

            case GachaState.ReadyToRotate:
                guideText.text = BuildRotateGuideText();
                break;

            case GachaState.Rotating:
                guideText.text = "ガチャ作動中";
                break;

            case GachaState.RewardDisplaying:
                guideText.text = "景品獲得";
                break;

            default:
                guideText.text = string.Empty;
                break;
        }
    }

    private string BuildRotateGuideText()
    {
        string[] steps = { "↑", "→", "↓", "←" };
        string text = "時計回し ";

        for (int i = 0; i < steps.Length; i++)
        {
            if (i < rotateStepIndex)
            {
                text += "[" + steps[i] + "]";
            }
            else
            {
                text += steps[i];
            }

            if (i < steps.Length - 1)
            {
                text += " ";
            }
        }

        return text;
    }

    // ---------------------------
    // GameCurrency 連携
    // CoinCounterUI.cs に合わせて同じ財布を見る
    // ---------------------------

    private object GetGameCurrencyInstance()
    {
        try
        {
            GameCurrency.EnsureInstance();
            return GameCurrency.Instance;
        }
        catch
        {
            return null;
        }
    }

    private int GetWalletCoins()
    {
        object instance = GetGameCurrencyInstance();
        if (instance == null)
        {
            return 0;
        }

        Type type = instance.GetType();

        PropertyInfo prop = type.GetProperty("Coins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(int))
        {
            object value = prop.GetValue(instance, null);
            return value is int intValue ? intValue : 0;
        }

        FieldInfo field = type.GetField("Coins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(int))
        {
            object value = field.GetValue(instance);
            return value is int intValue ? intValue : 0;
        }

        MethodInfo getCoinsMethod = type.GetMethod("GetCoins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (getCoinsMethod != null && getCoinsMethod.ReturnType == typeof(int))
        {
            object value = getCoinsMethod.Invoke(instance, null);
            return value is int intValue ? intValue : 0;
        }

        return 0;
    }

    private bool TrySetWalletCoins(int newValue)
    {
        object instance = GetGameCurrencyInstance();
        if (instance == null)
        {
            return false;
        }

        Type type = instance.GetType();

        PropertyInfo prop = type.GetProperty("Coins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.PropertyType == typeof(int) && prop.CanWrite)
        {
            prop.SetValue(instance, newValue, null);
            return true;
        }

        FieldInfo field = type.GetField("Coins", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(int))
        {
            field.SetValue(instance, newValue);
            return true;
        }

        return false;
    }

    private bool TrySpendWalletCoins(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        object instance = GetGameCurrencyInstance();
        if (instance == null)
        {
            return false;
        }

        Type type = instance.GetType();

        string[] boolMethods =
        {
            "TrySpendCoins",
            "SpendCoins",
            "UseCoins",
            "TryUseCoins",
            "RemoveCoins"
        };

        for (int i = 0; i < boolMethods.Length; i++)
        {
            MethodInfo method = type.GetMethod(boolMethods[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) continue;

            ParameterInfo[] ps = method.GetParameters();
            if (ps.Length != 1 || ps[0].ParameterType != typeof(int)) continue;

            object result = method.Invoke(instance, new object[] { amount });

            if (method.ReturnType == typeof(bool))
            {
                RefreshOwnedCoinsUi();
                return result is bool b && b;
            }

            if (method.ReturnType == typeof(void))
            {
                RefreshOwnedCoinsUi();
                return true;
            }

            if (method.ReturnType == typeof(int))
            {
                RefreshOwnedCoinsUi();
                return true;
            }
        }

        int current = GetWalletCoins();
        if (current < amount)
        {
            return false;
        }

        bool success = TrySetWalletCoins(current - amount);
        if (success)
        {
            RefreshOwnedCoinsUi();
        }
        return success;
    }

    private void AddWalletCoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        object instance = GetGameCurrencyInstance();
        if (instance == null)
        {
            return;
        }

        Type type = instance.GetType();

        string[] addMethods =
        {
            "AddCoins",
            "GainCoins",
            "IncreaseCoins"
        };

        for (int i = 0; i < addMethods.Length; i++)
        {
            MethodInfo method = type.GetMethod(addMethods[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) continue;

            ParameterInfo[] ps = method.GetParameters();
            if (ps.Length != 1 || ps[0].ParameterType != typeof(int)) continue;

            method.Invoke(instance, new object[] { amount });
            RefreshOwnedCoinsUi();
            return;
        }

        int current = GetWalletCoins();
        if (TrySetWalletCoins(current + amount))
        {
            RefreshOwnedCoinsUi();
        }
    }

    private sealed class RewardResult
    {
        public RewardCategory category;
        public string id;
        public string title;
        public string body;
        public string status;
        public Sprite icon;
        public bool wasDuplicateMd;
        public int mdLevelAfter;
        public int refundedCoins;
    }
}