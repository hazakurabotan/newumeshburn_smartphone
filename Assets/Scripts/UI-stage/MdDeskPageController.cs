using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MdDeskPageController : MonoBehaviour
{
    public enum CharacterKind
    {
        None,
        Mawaru13,
        Player
    }

    public enum DiskEffectType
    {
        None,
        Barrier,
        Clone,
        AttackUp
    }

    public enum CharacterDetectMode
    {
        AutoDetect,
        ForceMawaru13,
        ForcePlayer
    }

    [Serializable]
    public class MdDiskData
    {
        [Header("Basic")]
        public string id;
        public bool owned = true;

        [Header("UI")]
        public Sprite artworkSprite;
        public string title;
        [TextArea(2, 4)] public string effectDescription;

        [Header("BGM")]
        public AudioClip bgmClip;

        [Header("Effect")]
        public DiskEffectType effectType = DiskEffectType.None;
        [Range(0.1f, 1f)] public float barrierDamageMultiplier = 0.7f;
        [Min(1f)] public float attackPowerMultiplier = 1.5f;
    }

    [Header("Disk List")]
    [SerializeField] private List<MdDiskData> mdDisks = new List<MdDiskData>();

    [Header("UI Refs")]
    [SerializeField] private Image slotTopImage;
    [SerializeField] private Image slotCenterImage;
    [SerializeField] private Image slotBottomImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI effectText;

    [Header("Input")]
    [SerializeField] private InputActionReference navigateAction;
    [SerializeField] private float deadZone = 0.6f;
    [SerializeField] private float firstRepeatDelay = 0.28f;
    [SerializeField] private float repeatDelay = 0.12f;

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource uiSeSource;
    [SerializeField] private AudioClip moveSe;
    [SerializeField] private bool loopBgm = true;

    [Header("Current Character Detect")]
    [SerializeField] private CharacterDetectMode detectMode = CharacterDetectMode.AutoDetect;
    [SerializeField] private GameObject mawaru13Object;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private Behaviour mawaru13ControlScript;
    [SerializeField] private Behaviour playerControlScript;

    [Header("Optional Effect Visuals (Player side)")]
    [SerializeField] private GameObject barrierVisualObject;
    [SerializeField] private GameObject cloneObject;
    [SerializeField] private GameObject attackUpVisualObject;

    [Header("Behavior")]
    [SerializeField] private bool applySelectionOnEnable = true;
    [SerializeField] private bool clearEffectWhenNotPlayer = true;

    [Header("State")]
    [SerializeField] private int selectedIndex = 0;

    [Header("Runtime Effect State")]
    [SerializeField] private DiskEffectType currentEffectType = DiskEffectType.None;
    [SerializeField] private float incomingDamageMultiplier = 1f;
    [SerializeField] private float currentAttackPowerMultiplier = 1f;
    [SerializeField] private bool cloneEnabled = false;

    private int holdDirection = 0;
    private float nextRepeatTime = 0f;
    private bool pageOpened = false;

    public int SelectedIndex => selectedIndex;
    public int Count => mdDisks != null ? mdDisks.Count : 0;
    public DiskEffectType CurrentEffectType => currentEffectType;
    public float IncomingDamageMultiplier => incomingDamageMultiplier;
    public float AttackPowerMultiplier => currentAttackPowerMultiplier;
    public bool CloneEnabled => cloneEnabled;

    private void Awake()
    {
        TryAutoFindBgmSource();
        ClampSelectedIndex();
        RefreshVisuals();
        ApplyEffectVisualState();
    }

    private void OnEnable()
    {
        pageOpened = true;
        ResetHoldInput();
        ClampSelectedIndex();
        RefreshVisuals();

        if (applySelectionOnEnable)
        {
            ApplyCurrentSelection();
        }
    }

    private void OnDisable()
    {
        pageOpened = false;
        ResetHoldInput();
    }

    private void Update()
    {
        if (!pageOpened) return;
        if (mdDisks == null || mdDisks.Count == 0) return;
        if (navigateAction == null || navigateAction.action == null) return;

        Vector2 nav = navigateAction.action.ReadValue<Vector2>();
        int dir = 0;

        if (nav.y >= deadZone) dir = -1;
        else if (nav.y <= -deadZone) dir = 1;

        if (dir == 0)
        {
            holdDirection = 0;
            return;
        }

        float now = Time.unscaledTime;

        if (holdDirection != dir)
        {
            holdDirection = dir;
            MoveSelection(dir);
            nextRepeatTime = now + firstRepeatDelay;
            return;
        }

        if (now >= nextRepeatTime)
        {
            MoveSelection(dir);
            nextRepeatTime = now + repeatDelay;
        }
    }

    public void OpenPage()
    {
        pageOpened = true;
        ResetHoldInput();
        ClampSelectedIndex();
        RefreshVisuals();
        ApplyCurrentSelection();
    }

    public void ClosePage()
    {
        pageOpened = false;
        ResetHoldInput();
    }

    public void MoveUp()
    {
        MoveSelection(-1);
    }

    public void MoveDown()
    {
        MoveSelection(1);
    }

    public void SetSelectedIndex(int index, bool apply = true)
    {
        if (mdDisks == null || mdDisks.Count == 0) return;

        selectedIndex = WrapIndex(index, mdDisks.Count);
        RefreshVisuals();

        if (apply)
        {
            ApplyCurrentSelection();
        }
    }

    public MdDiskData GetCurrentDisk()
    {
        if (mdDisks == null || mdDisks.Count == 0) return null;
        return mdDisks[WrapIndex(selectedIndex, mdDisks.Count)];
    }

    public MdDiskData GetDiskAtIndex(int index)
    {
        if (mdDisks == null || mdDisks.Count == 0) return null;
        if (index < 0 || index >= mdDisks.Count) return null;

        return mdDisks[index];
    }

    public int FindDiskIndexById(string diskId)
    {
        if (mdDisks == null || mdDisks.Count == 0) return -1;
        if (string.IsNullOrWhiteSpace(diskId)) return -1;

        for (int i = 0; i < mdDisks.Count; i++)
        {
            MdDiskData disk = mdDisks[i];
            if (disk == null) continue;

            if (disk.id == diskId)
                return i;
        }

        return -1;
    }

    public int FindDiskIndexByTitle(string diskTitle)
    {
        if (mdDisks == null || mdDisks.Count == 0) return -1;
        if (string.IsNullOrWhiteSpace(diskTitle)) return -1;

        for (int i = 0; i < mdDisks.Count; i++)
        {
            MdDiskData disk = mdDisks[i];
            if (disk == null) continue;

            if (disk.title == diskTitle)
                return i;
        }

        return -1;
    }

    public bool IsDiskOwnedById(string diskId)
    {
        int index = FindDiskIndexById(diskId);
        if (index < 0) return false;

        MdDiskData disk = mdDisks[index];
        return disk != null && disk.owned;
    }

    public bool IsDiskOwnedAtIndex(int index)
    {
        MdDiskData disk = GetDiskAtIndex(index);
        return disk != null && disk.owned;
    }

    public bool UnlockDiskById(string diskId, bool registerToResult = true)
    {
        int index = FindDiskIndexById(diskId);
        if (index < 0)
        {
            Debug.LogWarning("[MdDeskPageController] UnlockDiskById failed. id=" + diskId);
            return false;
        }

        return UnlockDiskAtIndex(index, registerToResult);
    }

    public bool UnlockDiskByTitle(string diskTitle, bool registerToResult = true)
    {
        int index = FindDiskIndexByTitle(diskTitle);
        if (index < 0)
        {
            Debug.LogWarning("[MdDeskPageController] UnlockDiskByTitle failed. title=" + diskTitle);
            return false;
        }

        return UnlockDiskAtIndex(index, registerToResult);
    }

    public bool UnlockDiskAtIndex(int index, bool registerToResult = true)
    {
        if (mdDisks == null || mdDisks.Count == 0)
        {
            Debug.LogWarning("[MdDeskPageController] UnlockDiskAtIndex failed. mdDisks is empty.");
            return false;
        }

        if (index < 0 || index >= mdDisks.Count)
        {
            Debug.LogWarning("[MdDeskPageController] UnlockDiskAtIndex failed. index=" + index);
            return false;
        }

        MdDiskData disk = mdDisks[index];
        if (disk == null)
        {
            Debug.LogWarning("[MdDeskPageController] UnlockDiskAtIndex failed. disk is null. index=" + index);
            return false;
        }

        bool wasOwned = disk.owned;
        disk.owned = true;

        RefreshVisuals();

        if (selectedIndex == index)
        {
            ApplyCurrentSelection();
        }

        if (!wasOwned && registerToResult)
        {
            string displayName = string.IsNullOrWhiteSpace(disk.title) ? "MD" : disk.title;
            StageResultSession.EnsureInstance().RegisterMd(displayName, disk.artworkSprite);
            Debug.Log("[MdDeskPageController] Unlocked MD and registered to result: " + displayName);
        }
        else if (!wasOwned)
        {
            string displayName = string.IsNullOrWhiteSpace(disk.title) ? "MD" : disk.title;
            Debug.Log("[MdDeskPageController] Unlocked MD: " + displayName);
        }
        else
        {
            string displayName = string.IsNullOrWhiteSpace(disk.title) ? "MD" : disk.title;
            Debug.Log("[MdDeskPageController] MD already owned: " + displayName);
        }

        return !wasOwned;
    }

    public void SetDiskOwnedById(string diskId, bool owned)
    {
        int index = FindDiskIndexById(diskId);
        if (index < 0)
        {
            Debug.LogWarning("[MdDeskPageController] SetDiskOwnedById failed. id=" + diskId);
            return;
        }

        SetDiskOwnedAtIndex(index, owned);
    }

    public void SetDiskOwnedAtIndex(int index, bool owned)
    {
        MdDiskData disk = GetDiskAtIndex(index);
        if (disk == null)
        {
            Debug.LogWarning("[MdDeskPageController] SetDiskOwnedAtIndex failed. index=" + index);
            return;
        }

        disk.owned = owned;
        RefreshVisuals();

        if (selectedIndex == index)
        {
            ApplyCurrentSelection();
        }
    }

    public CharacterKind GetCurrentCharacter()
    {
        if (detectMode == CharacterDetectMode.ForceMawaru13)
            return CharacterKind.Mawaru13;

        if (detectMode == CharacterDetectMode.ForcePlayer)
            return CharacterKind.Player;

        bool playerActive = IsCharacterActive(playerObject, playerControlScript);
        bool mawaruActive = IsCharacterActive(mawaru13Object, mawaru13ControlScript);

        if (playerActive && !mawaruActive)
            return CharacterKind.Player;

        if (mawaruActive && !playerActive)
            return CharacterKind.Mawaru13;

        if (playerActive)
            return CharacterKind.Player;

        if (mawaruActive)
            return CharacterKind.Mawaru13;

        return CharacterKind.None;
    }

    public int GetModifiedIncomingDamage(int rawDamage)
    {
        int result = Mathf.RoundToInt(rawDamage * incomingDamageMultiplier);
        return Mathf.Max(1, result);
    }

    public float GetModifiedAttackPower(float rawPower)
    {
        return rawPower * currentAttackPowerMultiplier;
    }

    public void ReapplyCurrentSelection()
    {
        ApplyCurrentSelection();
    }

    public void ClearEffectState()
    {
        currentEffectType = DiskEffectType.None;
        incomingDamageMultiplier = 1f;
        currentAttackPowerMultiplier = 1f;
        cloneEnabled = false;
        ApplyEffectVisualState();
    }

    private void MoveSelection(int step)
    {
        if (mdDisks == null || mdDisks.Count == 0) return;

        selectedIndex = WrapIndex(selectedIndex + step, mdDisks.Count);

        RefreshVisuals();
        ApplyCurrentSelection();

        if (uiSeSource != null && moveSe != null)
        {
            uiSeSource.PlayOneShot(moveSe);
        }
    }

    private void RefreshVisuals()
    {
        if (mdDisks == null || mdDisks.Count == 0)
        {
            SetSlotImage(slotTopImage, null, false);
            SetSlotImage(slotCenterImage, null, true);
            SetSlotImage(slotBottomImage, null, false);

            if (titleText != null) titleText.text = string.Empty;
            if (effectText != null) effectText.text = string.Empty;
            return;
        }

        int topIndex = WrapIndex(selectedIndex - 1, mdDisks.Count);
        int centerIndex = WrapIndex(selectedIndex, mdDisks.Count);
        int bottomIndex = WrapIndex(selectedIndex + 1, mdDisks.Count);

        MdDiskData top = mdDisks[topIndex];
        MdDiskData center = mdDisks[centerIndex];
        MdDiskData bottom = mdDisks[bottomIndex];

        SetSlotImage(slotTopImage, top, false);
        SetSlotImage(slotCenterImage, center, true);
        SetSlotImage(slotBottomImage, bottom, false);

        if (center.owned)
        {
            if (titleText != null) titleText.text = center.title;
            if (effectText != null) effectText.text = center.effectDescription;
        }
        else
        {
            if (titleText != null) titleText.text = "未入手";
            if (effectText != null) effectText.text = "このミラクルデスクはまだ使えません";
        }
    }

    private void SetSlotImage(Image target, MdDiskData data, bool isCenter)
    {
        if (target == null) return;

        if (data == null || data.artworkSprite == null)
        {
            target.enabled = false;
            target.sprite = null;
            return;
        }

        target.enabled = true;
        target.sprite = data.artworkSprite;
        target.preserveAspect = true;

        Color c = Color.white;

        if (!data.owned)
        {
            c.a = 0.35f;
        }
        else if (!isCenter)
        {
            c.a = 0.9f;
        }

        target.color = c;
    }

    private void ApplyCurrentSelection()
    {
        MdDiskData current = GetCurrentDisk();
        if (current == null) return;

        if (!current.owned)
        {
            if (clearEffectWhenNotPlayer)
            {
                ClearEffectState();
            }
            return;
        }

        ApplyBgm(current);

        CharacterKind currentCharacter = GetCurrentCharacter();
        if (currentCharacter == CharacterKind.Player)
        {
            ApplyEffectState(current);
        }
        else
        {
            if (clearEffectWhenNotPlayer)
            {
                ClearEffectState();
            }
        }
    }

    private void ApplyBgm(MdDiskData disk)
    {
        if (bgmSource == null) return;
        if (disk == null) return;
        if (disk.bgmClip == null) return;

        bool changedClip = bgmSource.clip != disk.bgmClip;

        bgmSource.clip = disk.bgmClip;
        bgmSource.loop = loopBgm;

        if (changedClip || !bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    private void ApplyEffectState(MdDiskData disk)
    {
        currentEffectType = disk.effectType;

        switch (disk.effectType)
        {
            case DiskEffectType.Barrier:
                incomingDamageMultiplier = Mathf.Clamp(disk.barrierDamageMultiplier, 0.1f, 1f);
                currentAttackPowerMultiplier = 1f;
                cloneEnabled = false;
                break;

            case DiskEffectType.Clone:
                incomingDamageMultiplier = 1f;
                currentAttackPowerMultiplier = 1f;
                cloneEnabled = true;
                break;

            case DiskEffectType.AttackUp:
                incomingDamageMultiplier = 1f;
                currentAttackPowerMultiplier = Mathf.Max(1f, disk.attackPowerMultiplier);
                cloneEnabled = false;
                break;

            default:
                incomingDamageMultiplier = 1f;
                currentAttackPowerMultiplier = 1f;
                cloneEnabled = false;
                break;
        }

        ApplyEffectVisualState();
    }

    private void ApplyEffectVisualState()
    {
        if (barrierVisualObject != null)
        {
            barrierVisualObject.SetActive(currentEffectType == DiskEffectType.Barrier);
        }

        if (cloneObject != null)
        {
            cloneObject.SetActive(currentEffectType == DiskEffectType.Clone && cloneEnabled);
        }

        if (attackUpVisualObject != null)
        {
            attackUpVisualObject.SetActive(currentEffectType == DiskEffectType.AttackUp && currentAttackPowerMultiplier > 1f);
        }
    }

    private bool IsCharacterActive(GameObject obj, Behaviour controlScript)
    {
        if (obj == null) return false;
        if (!obj.activeInHierarchy) return false;

        if (controlScript != null)
        {
            return controlScript.enabled;
        }

        return true;
    }

    private void TryAutoFindBgmSource()
    {
        if (bgmSource != null) return;

        try
        {
            GameObject tagged = GameObject.FindGameObjectWithTag("BGM");
            if (tagged != null)
            {
                bgmSource = tagged.GetComponent<AudioSource>();
            }
        }
        catch
        {
        }

        if (bgmSource == null)
        {
            bgmSource = FindObjectOfType<AudioSource>();
        }
    }

    private void ClampSelectedIndex()
    {
        if (mdDisks == null || mdDisks.Count == 0)
        {
            selectedIndex = 0;
            return;
        }

        selectedIndex = WrapIndex(selectedIndex, mdDisks.Count);
    }

    private int WrapIndex(int index, int count)
    {
        if (count <= 0) return 0;

        index %= count;
        if (index < 0) index += count;
        return index;
    }

    private void ResetHoldInput()
    {
        holdDirection = 0;
        nextRepeatTime = 0f;
    }
}