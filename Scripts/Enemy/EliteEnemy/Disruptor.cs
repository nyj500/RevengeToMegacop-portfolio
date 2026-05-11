using UnityEngine;

public class Disruptor : EliteEnemy
{
    private enum DisruptorState
    {
        Approach,
        Disrupt,
        Shoot,
        Cooldown
    }

    private enum AnimState
    {
        Idle = 0,
        Move = 1,
        Throw = 2,
        Shoot = 3
    }

    [Header("Range")]
    [SerializeField] private float preferredMinRange = 9f;
    [SerializeField] private float preferredMaxRange = 16f;

    [Header("Disruptor")]
    [SerializeField] private float disruptDuration = 2.0f;
    [SerializeField] private float shootDuration = 2.4f;
    [SerializeField] private float cooldownDuration = 1.1f;

    [Header("Shoot")]
    [SerializeField] private float shootStartDelay = 0.15f;

    [Header("Disrupt -> Shoot Anim")]
    [SerializeField] private float shootAnimTransitionDelay = 1.5f;

    [Header("Flash Grenade")]
    [SerializeField] private GameObject flashGrenadePrefab;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private float throwReleaseDelay = 0.4f;
    [SerializeField] private float throwTravelTime = 0.75f;

    private DisruptorState disruptorState = DisruptorState.Approach;
    private float stateTimer = 0f;
    private bool hasTransitionedToShootAnim = false;
    private bool hasThrownGrenade = false;
    private float throwReleaseTimer = 0f;

    private Animator animator;

    private static readonly int StateHash = Animator.StringToHash("State");
    private static readonly int ThrowAnimHash = Animator.StringToHash("Throw");

    protected override void Start()
    {
        base.Start();
        animator = GetComponentInChildren<Animator>();
        ChangeState(DisruptorState.Cooldown);
    }

    protected override void HandleBehavior()
    {
        switch (disruptorState)
        {
            case DisruptorState.Approach:
                HandleApproach();
                break;

            case DisruptorState.Disrupt:
                HandleDisrupt();
                break;

            case DisruptorState.Shoot:
                HandleShoot();
                break;

            case DisruptorState.Cooldown:
                HandleCooldown();
                break;
        }
    }

    private void ChangeState(DisruptorState newState)
    {
        disruptorState = newState;

        switch (disruptorState)
        {
            case DisruptorState.Approach:
                stateTimer = 0f;
                EndShootState();
                SetAnimState(AnimState.Move);
                break;

            case DisruptorState.Disrupt:
                stateTimer = disruptDuration;
                EndShootState();
                hasTransitionedToShootAnim = false;
                hasThrownGrenade = false;
                throwReleaseTimer = throwReleaseDelay;
                SnapBodyToTarget();
                SetAnimState(AnimState.Throw);
                ForcePlayThrowAnimation();
                break;

            case DisruptorState.Shoot:
                BeginShootState(shootDuration, shootStartDelay);
                SetAnimState(AnimState.Shoot);
                break;

            case DisruptorState.Cooldown:
                stateTimer = cooldownDuration;
                EndShootState();
                SetAnimState(AnimState.Idle);
                break;
        }
    }

    private void HandleApproach()
    {
        SetAimMode(AimMode.BodyFaceTarget);

        float distance = DistanceToTarget();

        if (distance > preferredMaxRange)
        {
            RotateBodyTowardsTarget(bodyTurnSpeed);
            MoveTowardsTarget(bodyMoveSpeed);
            return;
        }

        if (distance < preferredMinRange)
        {
            RotateBodyTowardsTarget(bodyTurnSpeed);
            MoveAwayFromTarget(bodyMoveSpeed);
            return;
        }

        ChangeState(DisruptorState.Disrupt);
    }

    private void HandleDisrupt()
    {
        SetAimMode(AimMode.None);
        RotateBodyTowardsTarget(bodyTurnSpeed);

        if (!hasThrownGrenade)
        {
            throwReleaseTimer -= Time.deltaTime;

            if (throwReleaseTimer <= 0f)
            {
                ThrowFlashGrenade();
                hasThrownGrenade = true;
            }
        }

        float elapsedDisruptTime = disruptDuration - stateTimer;

        if (!hasTransitionedToShootAnim && elapsedDisruptTime >= shootAnimTransitionDelay)
        {
            hasTransitionedToShootAnim = true;
            SetAnimState(AnimState.Shoot);
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChangeState(DisruptorState.Shoot);
        }
    }

    private void HandleShoot()
    {
        SetAimMode(AimMode.AimFaceTarget);

        if (UpdateShootState())
        {
            ChangeState(DisruptorState.Cooldown);
        }
    }

    private void HandleCooldown()
    {
        SetAimMode(AimMode.BodyFaceTarget);

        float distance = DistanceToTarget();

        if (distance > preferredMaxRange + 2f || distance < preferredMinRange - 2f)
        {
            ChangeState(DisruptorState.Approach);
            return;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChangeState(DisruptorState.Disrupt);
        }
    }

    private void ThrowFlashGrenade()
    {
        if (flashGrenadePrefab == null || throwOrigin == null || Target == null)
            return;

        GameObject grenadeObject = Instantiate(flashGrenadePrefab, throwOrigin.position, Quaternion.identity);

        Collider[] grenadeColliders = grenadeObject.GetComponentsInChildren<Collider>();
        foreach (Collider grenadeCollider in grenadeColliders)
        {
            IgnoreCollisionWithTaggedObjects(grenadeCollider, "Enemy");
            IgnoreCollisionWithTaggedObjects(grenadeCollider, "Player");
        }

        Rigidbody rb = grenadeObject.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        Vector3 start = throwOrigin.position;
        Vector3 target = Target.position;
        target.y = 0f;

        float t = Mathf.Max(0.1f, throwTravelTime);
        Vector3 gravity = Physics.gravity;
        Vector3 displacement = target - start;

        Vector3 throwVelocity = new Vector3(
            displacement.x / t,
            (displacement.y - 0.5f * gravity.y * t * t) / t,
            displacement.z / t
        );

        rb.linearVelocity = throwVelocity;
    }

    private void IgnoreCollisionWithTaggedObjects(Collider grenadeCollider, string tag)
    {
        if (grenadeCollider == null)
            return;

        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);

        foreach (GameObject taggedObject in taggedObjects)
        {
            Collider[] targetColliders = taggedObject.GetComponentsInChildren<Collider>();

            foreach (Collider targetCollider in targetColliders)
            {
                if (targetCollider != null)
                {
                    Physics.IgnoreCollision(grenadeCollider, targetCollider);
                }
            }
        }
    }

    private void SetAnimState(AnimState state)
    {
        if (animator == null)
            return;

        animator.SetInteger(StateHash, (int)state);
    }

    private void ForcePlayThrowAnimation()
    {
        if (animator == null)
            return;

        animator.Play(ThrowAnimHash, 0, 0f);
        animator.Update(0f);
    }
}