using UnityEngine;
using UnityEngine.UI;

public class ActiveHpBinder : MonoBehaviour
{
    [Header("参照")]
    public HpBarController hpBar;      // HpPanel に付いてるやつ
    public Image portraitImage;        // 顔アイコン Image

    [Header("顔アイコン")]
    public Sprite playerPortrait;      // IMG_9801
    public Sprite mawaruPortrait;      // IMG_9808

    [Header("キャラ")]
    public PlayerController player;
    public MawaruController mawaru;

    [Header("動作")]
    [SerializeField] private bool autoDetectEveryFrame = true;
    [SerializeField] private bool preferPlayerWhenBothActive = true;

    enum Owner
    {
        None,
        Player,
        Mawaru
    }

    Owner bound = Owner.None;

    void Awake()
    {
        if (!hpBar) hpBar = GetComponent<HpBarController>();
    }

    void Start()
    {
        RefreshBinding(true);
    }

    void OnEnable()
    {
        RefreshBinding(true);
    }

    void Update()
    {
        if (!autoDetectEveryFrame) return;
        RefreshBinding(false);
    }

    void OnDisable()
    {
        UnbindAll();
    }

    void RefreshBinding(bool force)
    {
        Owner desired = DetectDesiredOwner();

        if (!force && desired == bound) return;

        switch (desired)
        {
            case Owner.Player:
                BindToPlayer();
                break;

            case Owner.Mawaru:
                BindToMawaru();
                break;

            default:
                if (force)
                {
                    UnbindAll();
                    if (portraitImage != null && playerPortrait != null)
                    {
                        portraitImage.sprite = playerPortrait;
                    }

                    if (player != null)
                    {
                        OnHpChanged(player.currentHP, player.maxHP);
                    }
                    else if (mawaru != null)
                    {
                        OnHpChanged(mawaru.currentHP, mawaru.maxHP);
                    }
                }
                break;
        }
    }

    Owner DetectDesiredOwner()
    {
        bool playerUsable = IsPlayerUsable();
        bool mawaruUsable = IsMawaruUsable();

        if (playerUsable && !mawaruUsable) return Owner.Player;
        if (mawaruUsable && !playerUsable) return Owner.Mawaru;

        if (playerUsable && mawaruUsable)
        {
            return preferPlayerWhenBothActive ? Owner.Player : Owner.Mawaru;
        }

        return Owner.None;
    }

    bool IsPlayerUsable()
    {
        if (player == null) return false;
        if (!player.gameObject.activeInHierarchy) return false;
        if (!player.enabled) return false;
        return true;
    }

    bool IsMawaruUsable()
    {
        if (mawaru == null) return false;
        if (!mawaru.gameObject.activeInHierarchy) return false;
        if (!mawaru.enabled) return false;
        return true;
    }

    void UnbindAll()
    {
        if (player != null) player.OnHpChanged -= OnHpChanged;
        if (mawaru != null) mawaru.OnHpChanged -= OnHpChanged;
        bound = Owner.None;
    }

    void OnHpChanged(int cur, int max)
    {
        hpBar?.SetHp(cur, max);
    }

    public void BindToPlayer()
    {
        if (bound == Owner.Player) return;

        UnbindAll();

        if (portraitImage != null)
            portraitImage.sprite = playerPortrait;

        if (player != null)
        {
            player.OnHpChanged += OnHpChanged;
            OnHpChanged(player.currentHP, player.maxHP);
        }

        bound = Owner.Player;
    }

    public void BindToMawaru()
    {
        if (bound == Owner.Mawaru) return;

        UnbindAll();

        if (portraitImage != null)
            portraitImage.sprite = mawaruPortrait;

        if (mawaru != null)
        {
            mawaru.OnHpChanged += OnHpChanged;
            OnHpChanged(mawaru.currentHP, mawaru.maxHP);
        }

        bound = Owner.Mawaru;
    }

    public void Bind(bool toMawaru)
    {
        if (toMawaru) BindToMawaru();
        else BindToPlayer();
    }
}