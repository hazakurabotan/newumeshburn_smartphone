using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MixJuicePageController : MonoBehaviour
{
    [System.Serializable]
    public class CanPourAnimationSet
    {
        public Sprite[] leftFrames;
        public Sprite[] rightFrames;
    }

    [System.Serializable]
    public class MixRecipe
    {
        [Header("Combination (base definition index order)")]
        public int canA = -1;
        public int canB = -1;

        [Header("Created Juice")]
        public string resultJuiceId;
        public string resultDrinkName;
        public Sprite resultDrinkCardSprite;
        [TextArea(2, 4)] public string resultMessage;

        public bool Matches(int a, int b)
        {
            return (canA == a && canB == b) || (canA == b && canB == a);
        }
    }

    [Header("References")]
    public JuiceInventory juiceInventory;

    [Header("States")]
    public GameObject selectState;
    public GameObject pourState;
    public GameObject rotateState;
    public GameObject resultState;

    [Header("Return")]
    public GameObject startPage;

    [Header("Select")]
    public RectTransform selectStateRoot;
    public Button[] canButtons;
    public RectTransform canCursor;
    public Vector2 cursorOffset = new Vector2(0f, 55f);

    [Header("Pour Images")]
    public Image pageImagePour;
    public Image leftCanImage;
    public Image rightCanImage;
    public Image leftLiquidImage;
    public Image rightLiquidImage;

    [Header("Pour Rects")]
    public RectTransform leftCanRect;
    public RectTransform rightCanRect;
    public RectTransform leftLiquidRect;
    public RectTransform rightLiquidRect;

    [Header("Rotate Images")]
    public Image rotateLeftLiquidImage;
    public Image rotateRightLiquidImage;

    [Header("Rotate Rects")]
    public RectTransform rotateLeftLiquidRect;
    public RectTransform rotateRightLiquidRect;

    [Header("Result UI")]
    public TMP_Text resultText;
    public TMP_Text skillText;
    public Image resultDrinkCard;
    public Button okButton;

    [Header("Can Sprites (base 5 definition index order)")]
    public Sprite[] canSprites;

    [Header("Pour Page Animation (base 5 definition index order)")]
    public CanPourAnimationSet[] pourAnimations = new CanPourAnimationSet[5];
    public Sprite pourPageIdleSprite;
    public float pageFrameDuration = 0.07f;

    [Header("Pour Positions")]
    public Vector2 leftCanStartPos = new Vector2(-120f, -145f);
    public Vector2 leftCanPourPos = new Vector2(-70f, 120f);
    public Vector2 rightCanStartPos = new Vector2(120f, -145f);
    public Vector2 rightCanPourPos = new Vector2(70f, 120f);

    [Header("Liquid Heights")]
    public float liquidStartHeight = 0f;
    public float liquidFilledHeight = 120f;

    [Header("Timing")]
    public float moveDuration = 0.25f;
    public float pourDuration = 0.35f;
    public float waitAfterEachSide = 0.10f;
    public float waitBeforeRotate = 0.15f;

    [Header("Rotate Input")]
    public float rotateInputDeadZone = 0.55f;
    public float rotateCompleteDegrees = 720f;
    public float rotateMinDeltaPerFrame = 1.5f;
    public bool resetProgressWhenReverse = true;

    [Header("Recipes")]
    public MixRecipe[] recipes;

    [Header("Fallback Result")]
    public string fallbackResultJuiceId = "mix_red_red";
    public string fallbackDrinkName = "ミックス缶ジュース";
    [TextArea(2, 4)] public string fallbackResultMessage = "ミックス缶ジュースを手持ちに入れた。";
    public Sprite fallbackResultDrinkCardSprite;

    [Header("Voice")]
    public MixJuiceVoiceController voiceController;
    public bool stopVoiceOnReset = true;
    public MixJuiceVoiceController.JuiceVoiceType[] canVoiceTypes =
    {
        MixJuiceVoiceController.JuiceVoiceType.Apple,
        MixJuiceVoiceController.JuiceVoiceType.Cider,
        MixJuiceVoiceController.JuiceVoiceType.Orange,
        MixJuiceVoiceController.JuiceVoiceType.Lime,
        MixJuiceVoiceController.JuiceVoiceType.Grape
    };

    private int firstOwnedSlot = -1;
    private int secondOwnedSlot = -1;
    private int firstCanDefinitionIndex = -1;
    private int secondCanDefinitionIndex = -1;

    private Coroutine pourRoutine;

    private float leftLiquidBaseWidth;
    private float rightLiquidBaseWidth;
    private float rotateLeftLiquidBaseWidth;
    private float rotateRightLiquidBaseWidth;

    private float rotateAccumulatedDegrees;
    private float rotatePrevAngle;
    private bool rotateHasPrevAngle;
    private int rotateDirection;
    private bool rotateCompleted;

    private Image[] canButtonImages;
    private readonly List<int> selectableOwnedSlots = new List<int>();

    private void Awake()
    {
        CacheRefs();
        CacheBaseWidths();
        CacheVoiceController();
        ResolveInventory();
        EnsureDefaultRecipes();
        CacheCanButtonImages();

        SetRaycastOff(canCursor);
        SetRaycastOff(pageImagePour);
        SetRaycastOff(leftCanImage);
        SetRaycastOff(rightCanImage);
        SetRaycastOff(leftLiquidImage);
        SetRaycastOff(rightLiquidImage);
        SetRaycastOff(rotateLeftLiquidImage);
        SetRaycastOff(rotateRightLiquidImage);

        if (pourPageIdleSprite == null && pageImagePour != null)
        {
            pourPageIdleSprite = pageImagePour.sprite;
        }
    }

    private void OnEnable()
    {
        CacheVoiceController();
        ResolveInventory();
        EnsureDefaultRecipes();
        CacheCanButtonImages();

        if (juiceInventory != null)
        {
            juiceInventory.OnInventoryChanged -= HandleInventoryChanged;
            juiceInventory.OnInventoryChanged += HandleInventoryChanged;
        }

        if (okButton != null)
        {
            okButton.onClick.RemoveListener(OnOkButtonPressed);
            okButton.onClick.AddListener(OnOkButtonPressed);
        }

        ResetPage();
    }

    private void OnDisable()
    {
        if (juiceInventory != null)
        {
            juiceInventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        if (okButton != null)
        {
            okButton.onClick.RemoveListener(OnOkButtonPressed);
        }
    }

    private void Update()
    {
        if (rotateState != null && rotateState.activeInHierarchy)
        {
            UpdateRotateInput();
        }
    }

    private void CacheRefs()
    {
        if (pageImagePour == null && pourState != null)
        {
            Transform t = pourState.transform.Find("PageImage_Pour");
            if (t != null) pageImagePour = t.GetComponent<Image>();
        }

        if (leftCanImage != null) leftCanRect = leftCanImage.rectTransform;
        if (rightCanImage != null) rightCanRect = rightCanImage.rectTransform;
        if (leftLiquidImage != null) leftLiquidRect = leftLiquidImage.rectTransform;
        if (rightLiquidImage != null) rightLiquidRect = rightLiquidImage.rectTransform;
        if (rotateLeftLiquidImage != null) rotateLeftLiquidRect = rotateLeftLiquidImage.rectTransform;
        if (rotateRightLiquidImage != null) rotateRightLiquidRect = rotateRightLiquidImage.rectTransform;
    }

    private void CacheBaseWidths()
    {
        if (leftLiquidRect != null) leftLiquidBaseWidth = leftLiquidRect.sizeDelta.x;
        if (rightLiquidRect != null) rightLiquidBaseWidth = rightLiquidRect.sizeDelta.x;
        if (rotateLeftLiquidRect != null) rotateLeftLiquidBaseWidth = rotateLeftLiquidRect.sizeDelta.x;
        if (rotateRightLiquidRect != null) rotateRightLiquidBaseWidth = rotateRightLiquidRect.sizeDelta.x;
    }

    private void CacheVoiceController()
    {
        if (voiceController == null)
        {
            voiceController = GetComponent<MixJuiceVoiceController>();
        }
    }

    private void ResolveInventory()
    {
        if (juiceInventory == null)
        {
            juiceInventory = JuiceInventory.Instance;
        }
    }

    private void EnsureDefaultRecipes()
    {
        if (recipes != null && recipes.Length == 15)
            return;

        recipes = new MixRecipe[15];
        int index = 0;

        void Add(int a, int b, string resultId, string resultName)
        {
            recipes[index] = new MixRecipe
            {
                canA = a,
                canB = b,
                resultJuiceId = resultId,
                resultDrinkName = resultName,
                resultMessage = resultName + "を手持ちに入れた。"
            };
            index++;
        }

        Add(0, 0, "mix_red_red", "レッドレッドミックス");
        Add(0, 1, "mix_red_blue", "レッドブルーミックス");
        Add(0, 2, "mix_red_green", "レッドグリーンミックス");
        Add(0, 3, "mix_red_orange", "レッドオレンジミックス");
        Add(0, 4, "mix_red_purple", "レッドパープルミックス");

        Add(1, 1, "mix_blue_blue", "ブルーブルーミックス");
        Add(1, 2, "mix_blue_green", "ブルーグリーンミックス");
        Add(1, 3, "mix_blue_orange", "ブルーオレンジミックス");
        Add(1, 4, "mix_blue_purple", "ブルーパープルミックス");

        Add(2, 2, "mix_green_green", "グリーングリーンミックス");
        Add(2, 3, "mix_green_orange", "グリーンオレンジミックス");
        Add(2, 4, "mix_green_purple", "グリーンパープルミックス");

        Add(3, 3, "mix_orange_orange", "オレンジオレンジミックス");
        Add(3, 4, "mix_orange_purple", "オレンジパープルミックス");

        Add(4, 4, "mix_purple_purple", "パープルパープルミックス");
    }

    private void CacheCanButtonImages()
    {
        if (canButtons == null)
        {
            canButtonImages = null;
            return;
        }

        canButtonImages = new Image[canButtons.Length];

        for (int i = 0; i < canButtons.Length; i++)
        {
            Button button = canButtons[i];
            if (button == null) continue;

            Image img = button.targetGraphic as Image;
            if (img == null) img = button.GetComponent<Image>();
            if (img == null) img = button.GetComponentInChildren<Image>(true);
            canButtonImages[i] = img;
        }
    }

    private void HandleInventoryChanged()
    {
        RefreshSelectButtons();
    }

    private void SetRaycastOff(Component c)
    {
        if (c == null) return;

        Image img = c.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
    }

    public void OpenFresh()
    {
        ResetPage();
    }

    private void ResetPage()
    {
        if (pourRoutine != null)
        {
            StopCoroutine(pourRoutine);
            pourRoutine = null;
        }

        CacheRefs();
        CacheBaseWidths();
        CacheVoiceController();
        ResolveInventory();
        EnsureDefaultRecipes();
        CacheCanButtonImages();

        firstOwnedSlot = -1;
        secondOwnedSlot = -1;
        firstCanDefinitionIndex = -1;
        secondCanDefinitionIndex = -1;

        ResetRotateTracking();

        if (stopVoiceOnReset && voiceController != null)
        {
            voiceController.StopAllVoices();
        }

        ShowOnly(selectState);

        RestoreIdlePourPage();
        HideAllPourCanImages();
        HideAllPourLiquids();
        HideAllRotateLiquids();

        if (leftCanRect != null) leftCanRect.anchoredPosition = leftCanStartPos;
        if (rightCanRect != null) rightCanRect.anchoredPosition = rightCanStartPos;

        SetLiquidHeight(leftLiquidRect, leftLiquidBaseWidth, liquidStartHeight);
        SetLiquidHeight(rightLiquidRect, rightLiquidBaseWidth, liquidStartHeight);
        SetLiquidHeight(rotateLeftLiquidRect, rotateLeftLiquidBaseWidth, liquidStartHeight);
        SetLiquidHeight(rotateRightLiquidRect, rotateRightLiquidBaseWidth, liquidStartHeight);

        if (resultText != null) resultText.text = string.Empty;
        if (skillText != null) skillText.text = string.Empty;

        if (resultDrinkCard != null)
        {
            resultDrinkCard.enabled = false;
        }

        RefreshSelectButtons();

        Debug.Log("[Mix] ResetPage");
    }

    private void LateUpdate()
    {
        if (selectState == null || !selectState.activeInHierarchy) return;
        if (canCursor == null) return;
        if (EventSystem.current == null) return;

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current == null) return;

        RectTransform target = current.GetComponent<RectTransform>();
        if (target == null) return;
        if (selectStateRoot != null && !target.IsChildOf(selectStateRoot)) return;

        canCursor.anchoredPosition = target.anchoredPosition + cursorOffset;
    }

    private void RefreshSelectableOwnedSlots()
    {
        selectableOwnedSlots.Clear();

        if (juiceInventory == null)
            return;

        List<int> slots = juiceInventory.GetOwnedMixIngredientSlotIndicesSnapshot();
        for (int i = 0; i < slots.Count; i++)
        {
            selectableOwnedSlots.Add(slots[i]);
        }
    }

    private void RefreshSelectButtons()
    {
        ResolveInventory();
        CacheCanButtonImages();
        RefreshSelectableOwnedSlots();

        int selectableCount = selectableOwnedSlots.Count;

        if (canButtons != null)
        {
            for (int i = 0; i < canButtons.Length; i++)
            {
                Button button = canButtons[i];
                if (button == null) continue;

                bool hasItem = i < selectableCount;
                button.gameObject.SetActive(hasItem);
                button.interactable = hasItem;

                Image img = canButtonImages != null && i < canButtonImages.Length ? canButtonImages[i] : null;
                if (img == null) continue;

                if (!hasItem)
                {
                    img.sprite = null;
                    img.enabled = false;
                    continue;
                }

                int ownedSlotIndex = selectableOwnedSlots[i];
                JuiceInventory.JuiceDefinition definition = juiceInventory.GetOwnedDefinitionAt(ownedSlotIndex);
                int definitionIndex = juiceInventory.GetOwnedDefinitionIndexAt(ownedSlotIndex);
                Sprite sprite = GetCanSpriteForDefinition(definition, definitionIndex);

                img.sprite = sprite;
                img.enabled = sprite != null;
                img.color = Color.white;
            }
        }

        RefreshSelectButtonNavigation(selectableCount);
        EnsureValidSelection(selectableCount);
    }

    private void RefreshSelectButtonNavigation(int selectableCount)
    {
        if (canButtons == null) return;

        for (int i = 0; i < canButtons.Length; i++)
        {
            Button button = canButtons[i];
            if (button == null) continue;

            Navigation nav = button.navigation;
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnLeft = null;
            nav.selectOnRight = null;
            nav.selectOnUp = null;
            nav.selectOnDown = null;

            if (i < selectableCount)
            {
                int prev = i - 1;
                int next = i + 1;

                nav.selectOnUp = prev >= 0 && prev < selectableCount ? canButtons[prev] : null;
                nav.selectOnDown = next >= 0 && next < selectableCount ? canButtons[next] : null;
            }

            button.navigation = nav;
        }
    }

    private void EnsureValidSelection(int selectableCount)
    {
        if (EventSystem.current == null) return;

        if (selectableCount <= 0)
        {
            EventSystem.current.SetSelectedGameObject(null);
            return;
        }

        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current != null)
        {
            for (int i = 0; i < selectableCount && i < canButtons.Length; i++)
            {
                if (canButtons[i] != null && current == canButtons[i].gameObject)
                    return;
            }
        }

        EventSystem.current.SetSelectedGameObject(null);
        if (canButtons != null && canButtons.Length > 0 && canButtons[0] != null)
        {
            EventSystem.current.SetSelectedGameObject(canButtons[0].gameObject);
        }
    }

    private Sprite GetCanSpriteForDefinition(JuiceInventory.JuiceDefinition definition, int definitionIndex)
    {
        if (definition != null && definition.icon != null)
            return definition.icon;

        if (canSprites != null && definitionIndex >= 0 && definitionIndex < canSprites.Length)
            return canSprites[definitionIndex];

        return null;
    }

    public void SelectCan0() { OnCanPressed(0); }
    public void SelectCan1() { OnCanPressed(1); }
    public void SelectCan2() { OnCanPressed(2); }
    public void SelectCan3() { OnCanPressed(3); }
    public void SelectCan4() { OnCanPressed(4); }

    private void OnCanPressed(int uiButtonIndex)
    {
        ResolveInventory();
        RefreshSelectableOwnedSlots();

        if (juiceInventory == null)
        {
            Debug.LogWarning("[Mix] JuiceInventory が見つからない");
            return;
        }

        if (selectableOwnedSlots.Count < 2)
        {
            Debug.LogWarning("[Mix] ミックスには素材缶が2本以上必要");
            return;
        }

        if (uiButtonIndex < 0 || uiButtonIndex >= selectableOwnedSlots.Count)
        {
            Debug.LogWarning($"[Mix] UI選択インデックスが不正: {uiButtonIndex}");
            return;
        }

        int ownedSlotIndex = selectableOwnedSlots[uiButtonIndex];
        int definitionIndex = juiceInventory.GetOwnedDefinitionIndexAt(ownedSlotIndex);
        if (!juiceInventory.IsValidDefinitionIndex(definitionIndex))
        {
            Debug.LogWarning($"[Mix] 定義インデックスが不正: {definitionIndex}");
            return;
        }

        JuiceInventory.JuiceDefinition definition = juiceInventory.GetOwnedDefinitionAt(ownedSlotIndex);
        if (definition == null || !definition.canUseAsMixIngredient)
        {
            Debug.LogWarning("[Mix] この缶は素材缶ではない");
            return;
        }

        if (firstOwnedSlot < 0)
        {
            firstOwnedSlot = ownedSlotIndex;
            firstCanDefinitionIndex = definitionIndex;
            PlayCanVoice(firstCanDefinitionIndex);
            Debug.Log($"[Mix] firstOwnedSlot={firstOwnedSlot} firstCanDefinitionIndex={firstCanDefinitionIndex}");
            return;
        }

        if (ownedSlotIndex == firstOwnedSlot)
        {
            Debug.Log("[Mix] 同じ缶スロットは2回選べない");
            return;
        }

        secondOwnedSlot = ownedSlotIndex;
        secondCanDefinitionIndex = definitionIndex;
        PlayCanVoice(secondCanDefinitionIndex);

        Debug.Log($"[Mix] secondOwnedSlot={secondOwnedSlot} secondCanDefinitionIndex={secondCanDefinitionIndex}");
        StartPourAnimation();
    }

    private void StartPourAnimation()
    {
        CacheRefs();
        CacheBaseWidths();
        CacheVoiceController();

        if (pourRoutine != null)
        {
            StopCoroutine(pourRoutine);
            pourRoutine = null;
        }

        if (firstCanDefinitionIndex < 0 || secondCanDefinitionIndex < 0) return;

        ShowOnly(pourState);
        RestoreIdlePourPage();
        HideAllPourCanImages();
        HideAllPourLiquids();

        if (leftCanRect != null) leftCanRect.anchoredPosition = leftCanStartPos;
        if (rightCanRect != null) rightCanRect.anchoredPosition = rightCanStartPos;

        SetLiquidHeight(leftLiquidRect, leftLiquidBaseWidth, liquidStartHeight);
        SetLiquidHeight(rightLiquidRect, rightLiquidBaseWidth, liquidStartHeight);

        pourRoutine = StartCoroutine(PlayPourRoutine());
    }

    private IEnumerator PlayPourRoutine()
    {
        yield return PlaySingleSidePour(true, firstCanDefinitionIndex);
        yield return WaitRealtime(waitAfterEachSide);

        yield return PlaySingleSidePour(false, secondCanDefinitionIndex);

        if (voiceController != null)
        {
            voiceController.PlayMixJuiceVoice();
        }

        yield return WaitRealtime(waitBeforeRotate);

        GoToRotate();
        pourRoutine = null;
    }

    private IEnumerator PlaySingleSidePour(bool isLeft, int canDefinitionIndex)
    {
        Sprite[] frames = GetPourFrames(canDefinitionIndex, isLeft);
        bool usePageFrames = frames != null && frames.Length > 0;

        if (usePageFrames)
        {
            yield return PlayPageFrameAnimation(frames);
            FinalizeLiquidState(isLeft, canDefinitionIndex, false);
        }
        else
        {
            yield return PlayFallbackSidePour(isLeft, canDefinitionIndex);
            FinalizeLiquidState(isLeft, canDefinitionIndex, true);
        }

        RestoreIdlePourPage();
    }

    private Sprite[] GetPourFrames(int canDefinitionIndex, bool isLeft)
    {
        if (pourAnimations == null) return null;
        if (canDefinitionIndex < 0 || canDefinitionIndex >= pourAnimations.Length) return null;
        if (pourAnimations[canDefinitionIndex] == null) return null;

        return isLeft ? pourAnimations[canDefinitionIndex].leftFrames : pourAnimations[canDefinitionIndex].rightFrames;
    }

    private IEnumerator PlayPageFrameAnimation(Sprite[] frames)
    {
        HideAllPourCanImages();
        HideAllPourLiquids();

        if (pageImagePour != null)
        {
            pageImagePour.enabled = true;
        }

        for (int i = 0; i < frames.Length; i++)
        {
            if (pageImagePour != null && frames[i] != null)
            {
                pageImagePour.sprite = frames[i];
            }

            yield return WaitRealtime(pageFrameDuration);
        }

        HideAllPourLiquids();
    }

    private IEnumerator PlayFallbackSidePour(bool isLeft, int canDefinitionIndex)
    {
        ResolveInventory();

        Image activeCanImage = isLeft ? leftCanImage : rightCanImage;
        RectTransform activeCanRect = isLeft ? leftCanRect : rightCanRect;
        Vector2 startPos = isLeft ? leftCanStartPos : rightCanStartPos;
        Vector2 pourPos = isLeft ? leftCanPourPos : rightCanPourPos;

        Image activeLiquidImage = isLeft ? leftLiquidImage : rightLiquidImage;
        RectTransform activeLiquidRect = isLeft ? leftLiquidRect : rightLiquidRect;
        float activeLiquidWidth = isLeft ? leftLiquidBaseWidth : rightLiquidBaseWidth;

        if (activeCanImage != null)
        {
            JuiceInventory.JuiceDefinition definition = juiceInventory != null ? juiceInventory.GetDefinitionAt(canDefinitionIndex) : null;
            activeCanImage.sprite = GetCanSpriteForDefinition(definition, canDefinitionIndex);
            activeCanImage.enabled = activeCanImage.sprite != null;
        }

        if (activeCanRect != null)
        {
            activeCanRect.anchoredPosition = startPos;
        }

        if (activeLiquidImage != null)
        {
            activeLiquidImage.color = GetCanColor(canDefinitionIndex);
            activeLiquidImage.enabled = false;
        }

        SetLiquidHeight(activeLiquidRect, activeLiquidWidth, liquidStartHeight);

        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / moveDuration);

            if (activeCanRect != null)
            {
                activeCanRect.anchoredPosition = Vector2.Lerp(startPos, pourPos, p);
            }

            yield return null;
        }

        if (activeCanRect != null)
        {
            activeCanRect.anchoredPosition = pourPos;
        }

        if (activeLiquidImage != null)
        {
            activeLiquidImage.enabled = true;
        }

        t = 0f;
        while (t < pourDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / pourDuration);
            float h = Mathf.Lerp(liquidStartHeight, liquidFilledHeight, p);

            SetLiquidHeight(activeLiquidRect, activeLiquidWidth, h);
            yield return null;
        }

        SetLiquidHeight(activeLiquidRect, activeLiquidWidth, liquidFilledHeight);

        if (activeCanImage != null)
        {
            activeCanImage.enabled = false;
        }
    }

    private void FinalizeLiquidState(bool isLeft, int canDefinitionIndex, bool visibleInPourState)
    {
        Image liquidImage = isLeft ? leftLiquidImage : rightLiquidImage;
        RectTransform liquidRect = isLeft ? leftLiquidRect : rightLiquidRect;
        float liquidWidth = isLeft ? leftLiquidBaseWidth : rightLiquidBaseWidth;

        if (liquidImage != null)
        {
            liquidImage.color = GetCanColor(canDefinitionIndex);
            liquidImage.enabled = visibleInPourState;
        }

        SetLiquidHeight(liquidRect, liquidWidth, liquidFilledHeight);

        if (isLeft)
        {
            if (leftCanImage != null) leftCanImage.enabled = false;
        }
        else
        {
            if (rightCanImage != null) rightCanImage.enabled = false;
        }
    }

    private void HideAllPourCanImages()
    {
        if (leftCanImage != null)
        {
            leftCanImage.sprite = null;
            leftCanImage.enabled = false;
        }

        if (rightCanImage != null)
        {
            rightCanImage.sprite = null;
            rightCanImage.enabled = false;
        }
    }

    private void HideAllPourLiquids()
    {
        if (leftLiquidImage != null)
        {
            leftLiquidImage.enabled = false;
            leftLiquidImage.color = Color.white;
        }

        if (rightLiquidImage != null)
        {
            rightLiquidImage.enabled = false;
            rightLiquidImage.color = Color.white;
        }
    }

    private void HideAllRotateLiquids()
    {
        if (rotateLeftLiquidImage != null)
        {
            rotateLeftLiquidImage.enabled = false;
            rotateLeftLiquidImage.color = Color.white;
        }

        if (rotateRightLiquidImage != null)
        {
            rotateRightLiquidImage.enabled = false;
            rotateRightLiquidImage.color = Color.white;
        }
    }

    private void RestoreIdlePourPage()
    {
        if (pageImagePour == null) return;

        pageImagePour.enabled = true;

        if (pourPageIdleSprite != null)
        {
            pageImagePour.sprite = pourPageIdleSprite;
        }
    }

    private IEnumerator WaitRealtime(float seconds)
    {
        if (seconds <= 0f) yield break;

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void ApplyRotatePreview()
    {
        if (firstCanDefinitionIndex < 0 || secondCanDefinitionIndex < 0) return;

        if (rotateLeftLiquidImage != null)
        {
            rotateLeftLiquidImage.color = GetCanColor(firstCanDefinitionIndex);
            rotateLeftLiquidImage.enabled = true;
        }

        if (rotateRightLiquidImage != null)
        {
            rotateRightLiquidImage.color = GetCanColor(secondCanDefinitionIndex);
            rotateRightLiquidImage.enabled = true;
        }

        SetLiquidHeight(rotateLeftLiquidRect, rotateLeftLiquidBaseWidth, liquidFilledHeight);
        SetLiquidHeight(rotateRightLiquidRect, rotateRightLiquidBaseWidth, liquidFilledHeight);
    }

    private void ResetRotateTracking()
    {
        rotateAccumulatedDegrees = 0f;
        rotatePrevAngle = 0f;
        rotateHasPrevAngle = false;
        rotateDirection = 0;
        rotateCompleted = false;
    }

    private void UpdateRotateInput()
    {
        if (rotateCompleted) return;

        Vector2 input = ReadRotateVector();
        if (input.magnitude < rotateInputDeadZone)
        {
            rotateHasPrevAngle = false;
            return;
        }

        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;

        if (!rotateHasPrevAngle)
        {
            rotatePrevAngle = angle;
            rotateHasPrevAngle = true;
            return;
        }

        float delta = Mathf.DeltaAngle(rotatePrevAngle, angle);
        rotatePrevAngle = angle;

        if (Mathf.Abs(delta) < rotateMinDeltaPerFrame) return;

        int currentDirection = delta > 0f ? 1 : -1;

        if (rotateDirection == 0)
        {
            rotateDirection = currentDirection;
        }
        else if (currentDirection != rotateDirection && resetProgressWhenReverse)
        {
            rotateDirection = currentDirection;
            rotateAccumulatedDegrees = 0f;
        }

        rotateAccumulatedDegrees += Mathf.Abs(delta);

        if (rotateAccumulatedDegrees >= rotateCompleteDegrees)
        {
            rotateCompleted = true;

            if (voiceController != null)
            {
                voiceController.PlayMixingFinishVoice();
            }

            CompleteMixAndShowResult();
        }
    }

    private Vector2 ReadRotateVector()
    {
        Vector2 v = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Gamepad.current != null)
        {
            Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
            Vector2 dpad = Gamepad.current.dpad.ReadValue();
            v = leftStick.sqrMagnitude >= dpad.sqrMagnitude ? leftStick : dpad;
        }

        if (v.sqrMagnitude < 0.001f && Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed) v.x -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed) v.x += 1f;
            if (Keyboard.current.downArrowKey.isPressed) v.y -= 1f;
            if (Keyboard.current.upArrowKey.isPressed) v.y += 1f;
        }
#else
        v.x = Input.GetAxisRaw("Horizontal");
        v.y = Input.GetAxisRaw("Vertical");
#endif

        if (v.sqrMagnitude > 1f) v.Normalize();
        return v;
    }

    private void CompleteMixAndShowResult()
    {
        ResolveInventory();

        if (juiceInventory == null)
        {
            ShowResultError("JuiceInventory が見つからない。", string.Empty);
            return;
        }

        if (firstOwnedSlot < 0 || secondOwnedSlot < 0 || firstOwnedSlot == secondOwnedSlot)
        {
            ShowResultError("材料の選択が不正。", string.Empty);
            return;
        }

        MixRecipe recipe = FindRecipe(firstCanDefinitionIndex, secondCanDefinitionIndex);

        string resultJuiceId = recipe != null && !string.IsNullOrWhiteSpace(recipe.resultJuiceId)
            ? recipe.resultJuiceId
            : fallbackResultJuiceId;

        if (string.IsNullOrWhiteSpace(resultJuiceId))
        {
            ShowResultError("完成ジュースIDが未設定。", string.Empty);
            return;
        }

        int resultDefinitionIndex = juiceInventory.GetDefinitionIndexById(resultJuiceId);
        if (!juiceInventory.IsValidDefinitionIndex(resultDefinitionIndex))
        {
            ShowResultError($"完成ジュースID '{resultJuiceId}' が JuiceInventory に存在しない。", string.Empty);
            return;
        }

        string drinkName = BuildResultDrinkName(recipe, resultDefinitionIndex);
        string subMessage = BuildResultMessage(recipe, drinkName);
        Sprite cardSprite = BuildResultCardSprite(recipe, resultDefinitionIndex);

        int storedFirstDefinitionIndex = firstCanDefinitionIndex;
        int storedSecondDefinitionIndex = secondCanDefinitionIndex;

        int removeA = Mathf.Max(firstOwnedSlot, secondOwnedSlot);
        int removeB = Mathf.Min(firstOwnedSlot, secondOwnedSlot);

        if (!juiceInventory.TryRemoveAt(removeA))
        {
            ShowResultError("材料ジュースの消費に失敗した。", string.Empty);
            return;
        }

        if (!juiceInventory.TryRemoveAt(removeB))
        {
            if (storedFirstDefinitionIndex >= 0)
                juiceInventory.TryAddByDefinitionIndex(storedFirstDefinitionIndex);

            ShowResultError("材料ジュースの消費に失敗した。", string.Empty);
            return;
        }

        if (!juiceInventory.TryAddByDefinitionIndex(resultDefinitionIndex, out JuiceInventory.JuiceDefinition obtainedDefinition, out _))
        {
            if (storedFirstDefinitionIndex >= 0)
                juiceInventory.TryAddByDefinitionIndex(storedFirstDefinitionIndex);

            if (storedSecondDefinitionIndex >= 0)
                juiceInventory.TryAddByDefinitionIndex(storedSecondDefinitionIndex);

            ShowResultError("完成ジュースの追加に失敗した。", string.Empty);
            return;
        }

        if (resultText != null) resultText.text = drinkName + "が完成";
        if (skillText != null) skillText.text = subMessage;

        if (resultDrinkCard != null)
        {
            Sprite sprite = cardSprite;
            if (sprite == null && obtainedDefinition != null)
                sprite = obtainedDefinition.icon;

            resultDrinkCard.sprite = sprite;
            resultDrinkCard.enabled = sprite != null;
        }

        ShowOnly(resultState);

        if (EventSystem.current != null && okButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(okButton.gameObject);
        }

        Debug.Log($"[Mix] Result => {drinkName} / {subMessage}");
    }

    private string BuildResultDrinkName(MixRecipe recipe, int resultDefinitionIndex)
    {
        if (recipe != null && !string.IsNullOrWhiteSpace(recipe.resultDrinkName))
            return recipe.resultDrinkName;

        JuiceInventory.JuiceDefinition definition = juiceInventory != null ? juiceInventory.GetDefinitionAt(resultDefinitionIndex) : null;
        if (definition != null && !string.IsNullOrWhiteSpace(definition.displayName))
            return definition.displayName;

        return fallbackDrinkName;
    }

    private string BuildResultMessage(MixRecipe recipe, string drinkName)
    {
        if (recipe != null && !string.IsNullOrWhiteSpace(recipe.resultMessage))
            return recipe.resultMessage;

        if (!string.IsNullOrWhiteSpace(fallbackResultMessage))
            return fallbackResultMessage;

        return drinkName + "を手持ちに入れた。";
    }

    private Sprite BuildResultCardSprite(MixRecipe recipe, int resultDefinitionIndex)
    {
        if (recipe != null && recipe.resultDrinkCardSprite != null)
            return recipe.resultDrinkCardSprite;

        if (fallbackResultDrinkCardSprite != null)
            return fallbackResultDrinkCardSprite;

        JuiceInventory.JuiceDefinition definition = juiceInventory != null ? juiceInventory.GetDefinitionAt(resultDefinitionIndex) : null;
        if (definition != null)
            return definition.icon;

        return null;
    }

    private void ShowResultError(string title, string message)
    {
        if (resultText != null) resultText.text = title;
        if (skillText != null) skillText.text = message;

        if (resultDrinkCard != null)
        {
            resultDrinkCard.enabled = false;
        }

        ShowOnly(resultState);

        if (EventSystem.current != null && okButton != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(okButton.gameObject);
        }

        Debug.LogWarning($"[Mix] {title} {message}");
    }

    private MixRecipe FindRecipe(int a, int b)
    {
        if (recipes == null) return null;

        for (int i = 0; i < recipes.Length; i++)
        {
            if (recipes[i] == null) continue;
            if (recipes[i].Matches(a, b)) return recipes[i];
        }

        return null;
    }

    private void OnOkButtonPressed()
    {
        ReturnToStartPage();
    }

    public void ReturnToStartPage()
    {
        ResetPage();

        if (startPage != null)
        {
            startPage.SetActive(true);
        }

        gameObject.SetActive(false);
    }

    private Color GetCanColor(int definitionIndex)
    {
        switch (definitionIndex)
        {
            case 0: return new Color32(166, 93, 58, 255);
            case 1: return new Color32(142, 219, 230, 255);
            case 2: return new Color32(186, 217, 116, 255);
            case 3: return new Color32(236, 210, 94, 255);
            case 4: return new Color32(191, 107, 223, 255);
            default: return Color.white;
        }
    }

    private void SetLiquidHeight(RectTransform rect, float width, float height)
    {
        if (rect == null) return;

        Vector2 size = rect.sizeDelta;
        size.x = width;
        size.y = height;
        rect.sizeDelta = size;
    }

    private void ShowOnly(GameObject target)
    {
        if (selectState != null) selectState.SetActive(target == selectState);
        if (pourState != null) pourState.SetActive(target == pourState);
        if (rotateState != null) rotateState.SetActive(target == rotateState);
        if (resultState != null) resultState.SetActive(target == resultState);
    }

    public void GoToRotate()
    {
        ShowOnly(rotateState);
        ApplyRotatePreview();
        ResetRotateTracking();
    }

    public void GoToResult()
    {
        CompleteMixAndShowResult();
    }

    public void BackToSelect()
    {
        ResetPage();
    }

    public void ForceCompleteRotateForTest()
    {
        if (firstCanDefinitionIndex < 0 || secondCanDefinitionIndex < 0) return;

        rotateCompleted = true;

        if (voiceController != null)
        {
            voiceController.PlayMixingFinishVoice();
        }

        CompleteMixAndShowResult();
    }

    private void PlayCanVoice(int canDefinitionIndex)
    {
        if (voiceController == null) return;

        MixJuiceVoiceController.JuiceVoiceType voiceType = GetVoiceTypeForCanIndex(canDefinitionIndex);
        if (voiceType == MixJuiceVoiceController.JuiceVoiceType.None) return;

        voiceController.PlayJuiceVoice(voiceType);
    }

    private MixJuiceVoiceController.JuiceVoiceType GetVoiceTypeForCanIndex(int canDefinitionIndex)
    {
        if (canVoiceTypes != null && canDefinitionIndex >= 0 && canDefinitionIndex < canVoiceTypes.Length)
        {
            return canVoiceTypes[canDefinitionIndex];
        }

        switch (canDefinitionIndex)
        {
            case 0: return MixJuiceVoiceController.JuiceVoiceType.Apple;
            case 1: return MixJuiceVoiceController.JuiceVoiceType.Cider;
            case 2: return MixJuiceVoiceController.JuiceVoiceType.Orange;
            case 3: return MixJuiceVoiceController.JuiceVoiceType.Lime;
            case 4: return MixJuiceVoiceController.JuiceVoiceType.Grape;
            default: return MixJuiceVoiceController.JuiceVoiceType.None;
        }
    }
}