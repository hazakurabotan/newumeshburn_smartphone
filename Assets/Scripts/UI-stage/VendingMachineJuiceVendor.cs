using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class VendingMachineJuiceVendor : MonoBehaviour
{
    [Header("References")]
    public JuiceInventory juiceInventory;
    public JuicePopupUI popupUI;

    [Header("Purchase")]
    public int coinCost = 1;
    public float holdSeconds = 0.6f;

    [Header("Optional Keyboard Fallback")]
    public bool allowKeyboardFallback = true;

    private int insideCount = 0;
    private float holdTimer = 0f;
    private bool purchasedThisHold = false;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Awake()
    {
        if (juiceInventory == null)
            juiceInventory = JuiceInventory.Instance;
    }

    private void Update()
    {
        if (insideCount <= 0)
        {
            ResetHoldState();
            return;
        }

        bool holding = IsPurchaseButtonHeld();

        if (!holding)
        {
            ResetHoldState();
            return;
        }

        if (purchasedThisHold) return;

        holdTimer += Time.deltaTime;

        if (holdTimer >= holdSeconds)
        {
            purchasedThisHold = true;
            TryPurchase();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsValidInteractor(other)) return;
        insideCount++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsValidInteractor(other)) return;

        insideCount = Mathf.Max(0, insideCount - 1);

        if (insideCount <= 0)
            ResetHoldState();
    }

    private bool IsValidInteractor(Collider2D other)
    {
        if (other == null) return false;

        if (other.GetComponentInParent<PlayerController>() != null) return true;
        if (other.GetComponentInParent<MawaruController>() != null) return true;

        return false;
    }

    private bool IsPurchaseButtonHeld()
    {
        if (Gamepad.current != null && Gamepad.current.buttonWest.isPressed)
            return true;

        if (allowKeyboardFallback && Keyboard.current != null)
        {
            if (Keyboard.current.xKey.isPressed) return true;
            if (Keyboard.current.jKey.isPressed) return true;
        }

        return false;
    }

    private void ResetHoldState()
    {
        holdTimer = 0f;
        purchasedThisHold = false;
    }

    private void TryPurchase()
    {
        if (juiceInventory == null)
            juiceInventory = JuiceInventory.Instance;

        if (juiceInventory == null)
        {
            Debug.LogWarning("[VendingMachineJuiceVendor] JuiceInventory が見つかりません。");
            return;
        }

        if (juiceInventory.IsFull)
        {
            popupUI?.ShowMessage("これ以上持てない", $"缶ジュースは最大{juiceInventory.MaxSlots}本まで。");
            return;
        }

        GameCurrency currency = GameCurrency.Instance;
        if (currency == null)
            currency = GameCurrency.EnsureInstance();

        if (currency == null)
        {
            Debug.LogWarning("[VendingMachineJuiceVendor] GameCurrency が見つかりません。");
            return;
        }

        if (!currency.SpendCoins(coinCost))
        {
            popupUI?.ShowMessage("コインが足りない", $"購入には {coinCost} コイン必要。");
            return;
        }

        JuiceInventory.JuiceDefinition obtainedDefinition;
        int obtainedIndex;

        bool success = juiceInventory.TryAddRandom(out obtainedDefinition, out obtainedIndex);

        if (!success)
        {
            currency.AddCoins(coinCost);
            popupUI?.ShowMessage("買えない", "缶ジュースを入手できなかった。");
            return;
        }

        popupUI?.ShowJuice(obtainedDefinition);
    }
}