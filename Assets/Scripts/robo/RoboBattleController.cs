using UnityEngine;
using UnityEngine.InputSystem;   // © ‚±‚ê‚ª‚È‚¢‚Æ PlayerControls ‚ªŒ©‚¦‚È‚¢

public class RoboBattleController : MonoBehaviour
{
    PlayerControls controls;
    public AimCursorController cursor;
    public MechaPunchController punch;
    public MechaGuardController guard;
    public AimShooter shooter;

    Vector2 moveInput;
    bool shootHold;

    void Awake()
    {
        controls = new PlayerControls();

        controls.RoboBattle.MoveCursor.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        controls.RoboBattle.MoveCursor.canceled += ctx => moveInput = Vector2.zero;

        controls.RoboBattle.Shoot.performed += ctx => shootHold = true;
        controls.RoboBattle.Shoot.canceled += ctx => shootHold = false;

        controls.RoboBattle.LeftPunch.performed += ctx => punch.LeftPunch();
        controls.RoboBattle.RightPunch.performed += ctx => punch.RightPunch();

        controls.RoboBattle.LeftGuard.performed += ctx => guard.SetLeft(true);
        controls.RoboBattle.LeftGuard.canceled += ctx => guard.SetLeft(false);
        controls.RoboBattle.RightGuard.performed += ctx => guard.SetRight(true);
        controls.RoboBattle.RightGuard.canceled += ctx => guard.SetRight(false);
    }

    void OnEnable() => controls.RoboBattle.Enable();
    void OnDisable() => controls.RoboBattle.Disable();

    void Update()
    {
        // Æ€‚ÌˆÚ“®
        if (cursor) cursor.SetInput(moveInput);

        // ’e‚Ì˜AË
        if (shootHold && shooter) shooter.TryShoot();
    }
}
