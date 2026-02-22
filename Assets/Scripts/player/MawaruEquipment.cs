using UnityEngine;

public class MawaruEquipment : MonoBehaviour
{
    public enum WeaponMode { MechArm, CarryCase }

    [Header("State")]
    [SerializeField] private WeaponMode current = WeaponMode.MechArm;
    [SerializeField] private bool hasCarryCase = false;

    [Header("Animator")]
    [SerializeField] private Animator animator;

    [Tooltip("hand00(初期装備)の通常コントローラ。未設定なら起動時のControllerを採用")]
    [SerializeField] private RuntimeAnimatorController baseController;

    [Tooltip("CarryCaseBomb装備時に使うOverrideController")]
    [SerializeField] private AnimatorOverrideController carryOverrideController;

    static readonly int HashIsCarry = Animator.StringToHash("IsCarry");

    public GameObject hand00Object;          // hand00 の実体（子オブジェクトなど）
    public GameObject carryCaseBombObject;   // CarryCaseBomb の実体（子オブジェクト or 生成済み）

    // ★MawaruController側が判定できるように公開
    public bool IsCarryActive => current == WeaponMode.CarryCase;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!baseController && animator) baseController = animator.runtimeAnimatorController;

        Apply(current);
    }

    public void SetHasCarryCase(bool value)
    {
        hasCarryCase = value;

        if (!hasCarryCase && current == WeaponMode.CarryCase)
        {
            current = WeaponMode.MechArm;
            Apply(current);
        }
    }

    public void ToggleWeapon()
    {
        if (!hasCarryCase) return;

        current = (current == WeaponMode.MechArm) ? WeaponMode.CarryCase : WeaponMode.MechArm;
        Apply(current);
    }

    void Apply(WeaponMode mode)
    {
        if (!animator) return;

        // ★トリガー残留で変な遷移を起こすのを防ぐ
        animator.ResetTrigger("Jump2");
        animator.ResetTrigger("Punch");
        animator.ResetTrigger("Punch2");
        animator.ResetTrigger("Punch3");

        bool isCarry = (mode == WeaponMode.CarryCase);
        animator.SetBool(HashIsCarry, isCarry);

        if (isCarry)
        {
            if (carryOverrideController) animator.runtimeAnimatorController = carryOverrideController;
        }
        else
        {
            if (baseController) animator.runtimeAnimatorController = baseController;
        }

        // 見た目
        if (hand00Object) hand00Object.SetActive(!isCarry);
        if (carryCaseBombObject) carryCaseBombObject.SetActive(isCarry);
    }
}
