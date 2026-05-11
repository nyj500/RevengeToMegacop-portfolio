using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    private static readonly int StateHash = Animator.StringToHash("State");

    private const int StateIdle = 0;
    private const int StateMove = 1;
    private const int StateShoot = 2;

    private Animator animator;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void UpdateAnimation(Enemy enemy)
    {
        if (animator == null) return;
        if (enemy == null) return;

        int state;
        if (enemy.Target == null)
            state = StateIdle;
        else if (enemy.IsTargetInRange())
            state = StateShoot;
        else
            state = StateMove;

        animator.SetInteger(StateHash, state);
    }
}
