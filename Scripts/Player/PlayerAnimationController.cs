using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsGuardingHash = Animator.StringToHash("IsGuarding");
    private static readonly int IsExecutionDashingHash = Animator.StringToHash("IsExecutionDashing");
    private static readonly int ThrowSwordHash = Animator.StringToHash("ThrowSword");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    private Animator animator;
    private PlayerMovementController movementController;
    private PlayerHitController hitController;

    public void Initialize(
        PlayerMovementController movementController,
        PlayerHitController hitController,
        PlayerStateController stateController,
        PlayerSwordController swordController)
    {
        this.movementController = movementController;
        this.hitController = hitController;

        animator = GetComponentInChildren<Animator>();

        hitController.OnDamaged += () => SetTriggerSafe(HitHash);
        stateController.OnDeath += () => SetTriggerSafe(DeathHash);
        if (swordController != null)
            swordController.OnSwordThrown += () => SetTriggerSafe(ThrowSwordHash);
    }

    public void UpdateAnimation()
    {
        if (animator == null) return;

        animator.SetFloat(SpeedHash, movementController.NormalizedSpeed);
        animator.SetFloat(MoveXHash, movementController.LocalMoveX);
        animator.SetFloat(MoveZHash, movementController.LocalMoveZ);
        animator.SetBool(IsMovingHash, movementController.IsMoving);
        animator.SetBool(IsGuardingHash, hitController.IsGuarding);
        animator.SetBool(IsExecutionDashingHash, movementController.IsExecutionDashing);
    }

    private void SetTriggerSafe(int hash)
    {
        if (animator == null) return;
        animator.SetTrigger(hash);
    }
}
