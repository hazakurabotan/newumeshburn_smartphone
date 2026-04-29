using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class DropPickup : MonoBehaviour
{
    public enum PickupType
    {
        Coin,
        Heal
    }

    [Header("Pickup")]
    public PickupType pickupType = PickupType.Coin;
    public int amount = 1;

    [Header("Physics")]
    [Tooltip("地面に落ちるための重力。0以下なら1を使う")]
    public float gravityScale = 1f;

    private bool picked = false;
    private Collider2D col2d;
    private Rigidbody2D rb2d;

    private void Awake()
    {
        col2d = GetComponent<Collider2D>();
        rb2d = GetComponent<Rigidbody2D>();

        if (col2d != null)
        {
            // 地面に乗るように Trigger を強制OFF
            col2d.isTrigger = false;
        }

        if (rb2d != null)
        {
            rb2d.gravityScale = gravityScale > 0f ? gravityScale : 1f;
            rb2d.freezeRotation = true;
            rb2d.velocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryPickup(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null) return;
        TryPickup(collision.collider);
    }

    private void TryPickup(Collider2D other)
    {
        if (picked) return;
        if (other == null) return;

        PlayerController player = FindPlayerController(other);
        if (player != null)
        {
            if (pickupType == PickupType.Coin)
            {
                GameCurrency.EnsureInstance().AddCoins(amount);
                Consume();
                return;
            }

            if (pickupType == PickupType.Heal)
            {
                player.Heal(amount);
                player.UpdateHpUI();
                Consume();
                return;
            }
        }

        MawaruController mawaru = FindMawaruController(other);
        if (mawaru != null)
        {
            if (pickupType == PickupType.Coin)
            {
                GameCurrency.EnsureInstance().AddCoins(amount);
                Consume();
                return;
            }

            if (pickupType == PickupType.Heal)
            {
                mawaru.currentHP = Mathf.Clamp(mawaru.currentHP + amount, 0, mawaru.maxHP);

                if (mawaru.hpBar == null)
                    mawaru.hpBar = FindObjectOfType<HpBarController>();

                mawaru.hpBar?.SetHp(mawaru.currentHP, mawaru.maxHP);
                Consume();
                return;
            }
        }
    }

    private PlayerController FindPlayerController(Collider2D other)
    {
        if (other == null) return null;

        PlayerController p = other.GetComponent<PlayerController>();
        if (p != null) return p;

        if (other.attachedRigidbody != null)
        {
            p = other.attachedRigidbody.GetComponent<PlayerController>();
            if (p != null) return p;
        }

        p = other.GetComponentInParent<PlayerController>();
        if (p != null) return p;

        p = other.GetComponentInChildren<PlayerController>();
        return p;
    }

    private MawaruController FindMawaruController(Collider2D other)
    {
        if (other == null) return null;

        MawaruController m = other.GetComponent<MawaruController>();
        if (m != null) return m;

        if (other.attachedRigidbody != null)
        {
            m = other.attachedRigidbody.GetComponent<MawaruController>();
            if (m != null) return m;
        }

        m = other.GetComponentInParent<MawaruController>();
        if (m != null) return m;

        m = other.GetComponentInChildren<MawaruController>();
        return m;
    }

    private void Consume()
    {
        picked = true;

        if (col2d != null)
            col2d.enabled = false;

        if (rb2d != null)
        {
            rb2d.velocity = Vector2.zero;
            rb2d.angularVelocity = 0f;
            rb2d.simulated = false;
        }

        Destroy(gameObject);
    }
}