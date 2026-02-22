using UnityEngine;
using UnityEngine.UI;
using System;

public class ActiveHpBinder : MonoBehaviour
{
    [Header("参照")]
    public HpBarController hpBar;      // HpPanel に付いてるやつ
    public Image portraitImage;        // HpPanel/Image を入れる

    [Header("顔アイコン")]
    public Sprite playerPortrait;      // IMG_9801
    public Sprite mawaruPortrait;      // IMG_9808

    [Header("キャラ")]
    public PlayerController player;
    public MawaruController mawaru;

    // 現在購読している側を外すために保持
    enum Owner { None, Player, Mawaru }
    Owner bound = Owner.None;

    void Awake()
    {
        if (!hpBar) hpBar = GetComponent<HpBarController>();
    }

    void OnDisable() { UnbindAll(); }

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
        if (portraitImage) portraitImage.sprite = playerPortrait;
        if (player != null)
        {
            player.OnHpChanged += OnHpChanged;
            OnHpChanged(player.currentHP, player.maxHP); // 即時反映
        }
        bound = Owner.Player;
    }

    public void BindToMawaru()
    {
        if (bound == Owner.Mawaru) return;
        UnbindAll();
        if (portraitImage) portraitImage.sprite = mawaruPortrait;
        if (mawaru != null)
        {
            mawaru.OnHpChanged += OnHpChanged;
            OnHpChanged(mawaru.currentHP, mawaru.maxHP); // 即時反映
        }
        bound = Owner.Mawaru;
    }

    public void Bind(bool toMawaru)
    {
        if (toMawaru) BindToMawaru();
        else BindToPlayer();
    }
}
