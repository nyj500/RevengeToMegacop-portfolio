using UnityEngine;

public class AgileRifleman : EliteEnemy
{
    private enum AgileRiflemanState
    {
        Idle,
        Move,
        Roll,
        Shoot
    }

    private enum RollType
    {
        Backward,
        StrafeLeft,
        StrafeRight
    }

    private enum AnimState
    {
        Idle = 0,
        Move = 1,
        Shoot = 2,
        Roll = 3
    }

    [Header("Range")]
    [SerializeField] private float preferredMinRange = 7f;
    [SerializeField] private float preferredMaxRange = 14f;

    [Header("Roll")]
    [SerializeField] private float rollSpeed = 8f;
    [SerializeField] private float rollDuration = 1.0f;
    [SerializeField] private float rollDistanceMin = 8.5f;
    [SerializeField] private float rollDistanceMax = 11.5f;

    [Header("Shoot")]
    [SerializeField] private float shootDuration = 1.8f;
    [SerializeField] private float idleDuration = 0.4f;
    [SerializeField] private float shootStartDelay = 0.15f;

    [Header("Shoot -> Roll")]
    [SerializeField, Range(0f, 1f)] private float shootRollChance = 0.65f;
    [SerializeField] private float minShootBeforeRoll = 0.35f;
    [SerializeField] private float maxShootBeforeRoll = 0.9f;

    private AgileRiflemanState agileRiflemanState = AgileRiflemanState.Idle;
    private RollType currentRollType = RollType.StrafeRight;

    private float stateTimer = 0f;
    private float scheduledShootRollTime = -1f;
    private bool hasRolledDuringShoot = false;

    private Vector3 rollDestination;
    private Vector3 rollTargetAnchorPosition;

    private Animator animator;

    private static readonly int StateHash = Animator.StringToHash("State");
    private static readonly int DiveRollStateHash = Animator.StringToHash("Dive Roll");

    protected override void Start()
    {
        base.Start();
        animator = GetComponentInChildren<Animator>();
        ChangeState(AgileRiflemanState.Idle);
    }

    protected override void HandleBehavior()
    {
        switch (agileRiflemanState)
        {
            case AgileRiflemanState.Idle:
                HandleIdle();
                break;

            case AgileRiflemanState.Move:
                HandleMove();
                break;

            case AgileRiflemanState.Roll:
                HandleRoll();
                break;

            case AgileRiflemanState.Shoot:
                HandleShoot();
                break;
        }
    }

    private void ChangeState(AgileRiflemanState newState)
    {
        agileRiflemanState = newState;

        switch (agileRiflemanState)
        {
            case AgileRiflemanState.Idle:
                EndShootState();
                stateTimer = idleDuration;
                SetAnimState(AnimState.Idle);
                break;

            case AgileRiflemanState.Move:
                EndShootState();
                stateTimer = 0f;
                SetAnimState(AnimState.Move);
                break;

            case AgileRiflemanState.Roll:
                EndShootState();
                stateTimer = rollDuration;
                rollTargetAnchorPosition = Target != null ? Target.position : transform.position;
                PrepareRollDestination();
                SnapBodyToDirection(rollDestination - transform.position);
                SetAnimState(AnimState.Roll);
                ForceRestartRollAnimation();
                break;

            case AgileRiflemanState.Shoot:
                PrepareShootRollPattern();
                BeginShootState(shootDuration, shootStartDelay);
                SetAnimState(AnimState.Shoot);
                break;
        }
    }

    private void HandleIdle()
    {
        SetAimMode(AimMode.BodyFaceTarget);

        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f)
            return;

        float distance = DistanceToTarget();

        if (distance < preferredMinRange)
        {
            currentRollType = RollType.Backward;
            ChangeState(AgileRiflemanState.Roll);
            return;
        }

        if (distance > preferredMaxRange)
        {
            ChangeState(AgileRiflemanState.Move);
            return;
        }

        currentRollType = Random.value > 0.5f
            ? RollType.StrafeRight
            : RollType.StrafeLeft;

        ChangeState(AgileRiflemanState.Roll);
    }

    private void HandleMove()
    {
        SetAimMode(AimMode.BodyFaceTarget);
        RotateBodyTowardsTarget(bodyTurnSpeed);

        float distance = DistanceToTarget();

        if (distance > preferredMaxRange)
        {
            MoveTowardsTarget(bodyMoveSpeed);
            return;
        }

        if (distance < preferredMinRange)
        {
            currentRollType = RollType.Backward;
            ChangeState(AgileRiflemanState.Roll);
            return;
        }

        ChangeState(AgileRiflemanState.Shoot);
    }

    private void HandleRoll()
    {
        SetAimMode(AimMode.None);

        Vector3 toDestination = rollDestination - transform.position;
        toDestination.y = 0f;

        float moveStep = rollSpeed * Time.deltaTime;

        if (toDestination.magnitude <= moveStep)
        {
            transform.position = new Vector3(
                rollDestination.x,
                transform.position.y,
                rollDestination.z
            );
        }
        else
        {
            MoveInDirection(toDestination.normalized, rollSpeed);
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            float distance = DistanceToTarget();

            if (distance > preferredMaxRange)
            {
                ChangeState(AgileRiflemanState.Move);
                return;
            }

            if (distance < preferredMinRange)
            {
                currentRollType = RollType.Backward;
                ChangeState(AgileRiflemanState.Roll);
                return;
            }

            ChangeState(AgileRiflemanState.Shoot);
        }
    }

    private void HandleShoot()
    {
        SetAimMode(AimMode.AimFaceTarget);

        float elapsedShootTime = shootDuration - ShootStateRemainingTime;
        float distance = DistanceToTarget();

        bool canDoStrafeRoll =
            distance > preferredMinRange &&
            distance < preferredMaxRange;

        if (!hasRolledDuringShoot && scheduledShootRollTime > 0f)
        {
            if (elapsedShootTime >= scheduledShootRollTime && canDoStrafeRoll)
            {
                hasRolledDuringShoot = true;
                currentRollType = Random.value > 0.5f
                    ? RollType.StrafeRight
                    : RollType.StrafeLeft;

                ChangeState(AgileRiflemanState.Roll);
                return;
            }
        }

        if (UpdateShootState())
        {
            ChangeState(AgileRiflemanState.Idle);
        }
    }

    private void PrepareShootRollPattern()
    {
        hasRolledDuringShoot = false;
        scheduledShootRollTime = -1f;

        bool canRollDuringShoot = Random.value < shootRollChance;
        if (canRollDuringShoot)
        {
            scheduledShootRollTime = Random.Range(minShootBeforeRoll, maxShootBeforeRoll);
        }
    }

    private void PrepareRollDestination()
    {
        Vector3 startPos = transform.position;
        Vector3 toTarget = rollTargetAnchorPosition - startPos;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f)
        {
            rollDestination = startPos;
            return;
        }

        Vector3 awayFromTarget = (-toTarget).normalized;
        Vector3 sideDirection = Vector3.Cross(Vector3.up, toTarget.normalized);

        float desiredDistance = Random.Range(rollDistanceMin, rollDistanceMax);

        switch (currentRollType)
        {
            case RollType.Backward:
                rollDestination = rollTargetAnchorPosition + awayFromTarget * desiredDistance;
                break;

            case RollType.StrafeLeft:
                {
                    Vector3 candidate = startPos - sideDirection * rollSpeed * rollDuration;
                    Vector3 fromTarget = candidate - rollTargetAnchorPosition;
                    fromTarget.y = 0f;

                    if (fromTarget.sqrMagnitude < 0.001f)
                        fromTarget = -sideDirection;

                    rollDestination = rollTargetAnchorPosition + fromTarget.normalized * desiredDistance;
                    break;
                }

            case RollType.StrafeRight:
                {
                    Vector3 candidate = startPos + sideDirection * rollSpeed * rollDuration;
                    Vector3 fromTarget = candidate - rollTargetAnchorPosition;
                    fromTarget.y = 0f;

                    if (fromTarget.sqrMagnitude < 0.001f)
                        fromTarget = sideDirection;

                    rollDestination = rollTargetAnchorPosition + fromTarget.normalized * desiredDistance;
                    break;
                }
        }

        rollDestination.y = transform.position.y;
    }

    private void ForceRestartRollAnimation()
    {
        if (animator == null)
            return;

        animator.Play(DiveRollStateHash, 0, 0f);
        animator.Update(0f);
    }

    private void SetAnimState(AnimState state)
    {
        if (animator == null)
            return;

        animator.SetInteger(StateHash, (int)state);
    }
}