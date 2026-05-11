using UnityEngine;

public class EliteEnemy : Enemy
{
    protected enum AimMode
    {
        None,
        BodyFaceTarget,
        AimFaceTarget
    }

    [Header("Elite Test Fallback")]
    [SerializeField] protected GameObject defaultGunWeaponPrefab;

    [Header("Elite Common")]
    [SerializeField] protected Transform aimPivot;
    [SerializeField] protected float bodyMoveSpeed = 5f;
    [SerializeField] protected float bodyTurnSpeed = 10f;

    [Header("Elite Shoot")]
    [SerializeField] protected float fireAngleTolerance = 12f;

    [Header("Elite Execution")]
    [SerializeField] private bool canBeExecuted = false;
    [SerializeField, Range(0f, 1f)] private float executionDamageRatio = 0.2f;

    protected GunWeapon gunWeapon;

    private Vector3 targetPreviousPosition;
    private Vector3 targetVelocity;

    private AimMode currentAimMode = AimMode.BodyFaceTarget;
    private bool shootRequested = false;

    private float shootStateTimer = 0f;
    private float shootStartDelayTimer = 0f;
    private bool isShootStateActive = false;

    protected float ShootStateRemainingTime => shootStateTimer;
    protected bool IsShootStateActive => isShootStateActive;

    protected override void Start()
    {
        base.Start();

        SetupTargetIfNeeded();
        SetupWeaponIfNeeded();

        if (Target != null)
        {
            targetPreviousPosition = Target.position;
        }
    }

    protected override void Update()
    {
        if (Target == null)
            return;

        UpdateTargetVelocity();

        currentAimMode = AimMode.BodyFaceTarget;
        shootRequested = false;

        HandleBehavior();
        ProcessAim();

        if (isShootStateActive)
        {
            UpdateShootRequest();
        }

        ProcessAttack();
    }

    protected virtual void HandleBehavior()
    {
    }

    public override ExecutionResult HandleExecution(ExecutionContext context)
    {
        if (CanAcceptExecution())
        {
            return base.HandleExecution(context);
        }

        if (context.SlashVfx != null)
        {
            context.SlashVfx.Play(context.SlicePosition, context.SlashDirection);
        }

        float damageAmount = MaxHp * executionDamageRatio;
        float newHp = Mathf.Max(0f, Hp - damageAmount);
        SetHp(newHp);

        if (Hp <= 0f)
        {
            Die();
        }
        else
        {
            InvokeOnHit();
        }

        return new ExecutionResult
        {
            Target = this,
            Position = context.SlicePosition
        };
    }

    protected virtual bool CanAcceptExecution()
    {
        if (canBeExecuted)
            return true;

        return HpRatio <= executionDamageRatio;
    }

    #region Target / Weapon Setup

    protected void SetupTargetIfNeeded()
    {
        if (Target != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            SetTarget(player.transform);
        }
    }

    protected void SetupWeaponIfNeeded()
    {
        if (gunWeapon != null)
            return;

        GunWeapon existingGun = GetComponentInChildren<GunWeapon>();
        if (existingGun != null)
        {
            gunWeapon = existingGun;
            return;
        }

        if (defaultGunWeaponPrefab == null)
            return;

        GameObject weaponObj = Instantiate(defaultGunWeaponPrefab);
        Weapon weaponComponent = weaponObj.GetComponent<Weapon>();

        if (weaponComponent == null)
        {
            Destroy(weaponObj);
            return;
        }

        EquipWeapon(weaponComponent);
        gunWeapon = weaponComponent as GunWeapon;
    }

    #endregion

    #region Target Tracking

    private void UpdateTargetVelocity()
    {
        if (Target == null)
            return;

        float dt = Time.deltaTime;
        if (dt > 0f)
        {
            targetVelocity = (Target.position - targetPreviousPosition) / dt;
        }
        else
        {
            targetVelocity = Vector3.zero;
        }

        targetPreviousPosition = Target.position;
    }

    #endregion

    #region Aim / Attack

    private void ProcessAim()
    {
        switch (currentAimMode)
        {
            case AimMode.None:
                break;

            case AimMode.BodyFaceTarget:
                RotateBodyTowardsTarget(bodyTurnSpeed);
                break;

            case AimMode.AimFaceTarget:
                RotateAimToLeadTarget(gunWeapon);
                break;
        }
    }

    private void UpdateShootRequest()
    {
        if (shootStartDelayTimer > 0f)
        {
            shootStartDelayTimer -= Time.deltaTime;
            return;
        }

        if (CanShootNow())
        {
            RequestShoot();
        }
    }

    private void ProcessAttack()
    {
        if (shootRequested)
        {
            TryFire();
        }
    }

    protected void BeginShootState(float duration, float startDelay)
    {
        shootStateTimer = duration;
        shootStartDelayTimer = startDelay;
        isShootStateActive = true;
        currentAimMode = AimMode.AimFaceTarget;
    }

    protected bool UpdateShootState()
    {
        if (!isShootStateActive)
            return true;

        currentAimMode = AimMode.AimFaceTarget;

        shootStateTimer -= Time.deltaTime;

        if (shootStateTimer <= 0f)
        {
            EndShootState();
            return true;
        }

        return false;
    }

    protected void EndShootState()
    {
        isShootStateActive = false;
        shootStateTimer = 0f;
        shootStartDelayTimer = 0f;
    }

    protected bool TryFire()
    {
        if (gunWeapon == null)
            return false;

        if (gunWeapon.CanFire())
        {
            gunWeapon.TryUse();
            return true;
        }

        gunWeapon.Reload();
        return false;
    }

    protected void RequestShoot()
    {
        shootRequested = true;
    }

    protected bool CanShootNow()
    {
        return IsFacingLeadTarget(gunWeapon, fireAngleTolerance);
    }

    protected void SetAimMode(AimMode mode)
    {
        currentAimMode = mode;
    }

    #endregion

    #region Direction / Rotation

    protected Vector3 GetDirectionToTarget()
    {
        if (Target == null)
            return transform.forward;

        Vector3 direction = Target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return transform.forward;
        }

        return direction.normalized;
    }

    protected Vector3 GetDirectionAwayFromTarget()
    {
        return -GetDirectionToTarget();
    }

    protected void RotateBodyTowardsTarget(float turnSpeed)
    {
        RotateBodyTowardsDirection(GetDirectionToTarget(), turnSpeed);
    }

    protected void RotateBodyTowardsDirection(Vector3 direction, float turnSpeed)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    protected void SnapBodyToTarget()
    {
        SnapBodyToDirection(GetDirectionToTarget());
    }

    protected void SnapBodyToDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    protected void RotateAimToTarget()
    {
        if (Target == null)
            return;

        Transform pivot = aimPivot != null ? aimPivot : transform;

        Vector3 targetDir = Target.position - pivot.position;
        targetDir.y = 0f;

        if (targetDir.sqrMagnitude < 0.001f)
            return;

        Vector3 currentAimDir = pivot.forward;
        currentAimDir.y = 0f;

        if (currentAimDir.sqrMagnitude < 0.001f)
            return;

        float angleDelta = Vector3.SignedAngle(
            currentAimDir.normalized,
            targetDir.normalized,
            Vector3.up
        );

        Quaternion deltaRotation = Quaternion.AngleAxis(angleDelta, Vector3.up);
        Quaternion targetRootRotation = deltaRotation * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRootRotation,
            bodyTurnSpeed * Time.deltaTime
        );
    }

    protected void RotateAimToLeadTarget(GunWeapon gun)
    {
        if (Target == null || gun == null)
        {
            RotateAimToTarget();
            return;
        }

        Transform pivot = aimPivot != null ? aimPivot : transform;

        Vector3 leadPosition = CalculateLeadPosition(gun);
        Vector3 targetDir = leadPosition - pivot.position;
        targetDir.y = 0f;

        if (targetDir.sqrMagnitude < 0.001f)
            return;

        Vector3 currentAimDir = pivot.forward;
        currentAimDir.y = 0f;

        if (currentAimDir.sqrMagnitude < 0.001f)
            return;

        float angleDelta = Vector3.SignedAngle(
            currentAimDir.normalized,
            targetDir.normalized,
            Vector3.up
        );

        Quaternion deltaRotation = Quaternion.AngleAxis(angleDelta, Vector3.up);
        Quaternion targetRootRotation = deltaRotation * transform.rotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRootRotation,
            bodyTurnSpeed * Time.deltaTime
        );
    }

    #endregion

    #region Movement

    protected float DistanceToTarget()
    {
        if (Target == null)
            return Mathf.Infinity;

        return Vector3.Distance(transform.position, Target.position);
    }

    protected void MoveTowardsTarget(float speed)
    {
        MoveInDirection(GetDirectionToTarget(), speed);
    }

    protected void MoveAwayFromTarget(float speed)
    {
        MoveInDirection(GetDirectionAwayFromTarget(), speed);
    }

    protected void MoveInDirection(Vector3 direction, float speed)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.position += direction.normalized * speed * Time.deltaTime;
    }

    #endregion

    #region Facing Check

    protected bool IsFacingPosition(Vector3 worldPosition, float maxAngle)
    {
        Transform pivot = aimPivot != null ? aimPivot : transform;

        Vector3 targetDir = worldPosition - pivot.position;
        targetDir.y = 0f;

        if (targetDir.sqrMagnitude < 0.001f)
            return true;

        Vector3 currentDir = pivot.forward;
        currentDir.y = 0f;

        if (currentDir.sqrMagnitude < 0.001f)
            return false;

        float angle = Vector3.Angle(currentDir.normalized, targetDir.normalized);
        return angle <= maxAngle;
    }

    protected bool IsFacingLeadTarget(GunWeapon gun, float maxAngle)
    {
        if (Target == null)
            return false;

        Vector3 aimPoint = gun != null
            ? CalculateLeadPosition(gun)
            : Target.position;

        return IsFacingPosition(aimPoint, maxAngle);
    }

    protected Vector3 CalculateLeadPosition(GunWeapon gun)
    {
        if (Target == null)
            return transform.position;

        if (gun == null || gun.BulletSpeed <= 0f)
            return Target.position;

        float distance = Vector3.Distance(transform.position, Target.position);
        float timeToTarget = distance / gun.BulletSpeed;

        return Target.position + (targetVelocity * timeToTarget);
    }

    #endregion
}