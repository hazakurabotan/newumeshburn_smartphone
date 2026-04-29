using UnityEngine;

public class RoboBattleCarryOverApplier : MonoBehaviour
{
    public PlayerHP playerHP;
    public BeamAmmo beamAmmo;

    [Header("Apply Options")]
    public bool clearSavedDataAfterApply = true;

    private void Start()
    {
        if (!ImpactRunToRoboBattleState.HasData)
            return;

        if (playerHP == null)
            playerHP = FindObjectOfType<PlayerHP>();

        if (beamAmmo == null)
            beamAmmo = FindObjectOfType<BeamAmmo>();

        if (playerHP != null)
        {
            playerHP.SetHPDirect(
                ImpactRunToRoboBattleState.CurrentHP,
                ImpactRunToRoboBattleState.MaxHP
            );
        }

        if (beamAmmo != null)
        {
            beamAmmo.SetAmmoDirect(
                ImpactRunToRoboBattleState.CurrentEnergy,
                ImpactRunToRoboBattleState.MaxEnergy
            );
        }

        if (clearSavedDataAfterApply)
            ImpactRunToRoboBattleState.Clear();
    }
}