using UnityEngine;

public class ShieldCharger : EliteEnemy
{
    private enum ShieldChargerState
    {
        Chase,
        Prepare,
        Dash,
        Recover,
        Shoot,
        Cooldown
    }

    [Header("Shield Charger")]
    [SerializeField] private float dashTriggerRange = 8f;

    [SerializeField] private float prepareDuration = 0.45f;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.7f;
    [SerializeField] private float recoverDuration = 0.8f;
    [SerializeField] private float shootDuration = 2.2f;
    [SerializeField] private float cooldownDuration = 1f;

    [Header("Shoot")]
    [SerializeField] private float shootStartDelay = 0f;

    [Header("Dash Collision")]
    [SerializeField] private float dashCollisionDamage = 20f;
    [SerializeField] private float dashKnockbackDuration = 0.5f;
    [SerializeField] private float dashKnockbackDistance = 10f;
    [SerializeField] private float dashKnockbackHeight = 3f;
    [SerializeField] private string playerTag = "Player";

    [Header("SFX")]
    [SerializeField] private AudioClip dashStartSfx;
    [SerializeField][Range(0f, 1f)] private float dashStartSfxVolume = 1f;

    [SerializeField] private AudioClip dashHitPlayerSfx;
    [SerializeField][Range(0f, 1f)] private float dashHitPlayerSfxVolume = 1f;

    [SerializeField] private AudioClip blockedBulletSfx;
    [SerializeField][Range(0f, 1f)] private float blockedBulletSfxVolume = 1f;

    private static readonly int BaseStateHash = Animator.StringToHash("BaseState");
    private static readonly int LeftArmStateHash = Animator.StringToHash("LeftArmState");

    private ShieldChargerState shieldChargerState = ShieldChargerState.Chase;
    private float stateTimer = 0f;
    private Vector3 dashDirection;
    private Animator animator;

    private bool hasHitPlayerThisDash = false;

    protected override void Start()
    {
        base.Start();

        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning($"{name}: Animator component not found.");
        }

        ChangeState(ShieldChargerState.Cooldown);
    }

    protected override void HandleBehavior()
    {
        switch (shieldChargerState)
        {
            case ShieldChargerState.Chase:
                HandleChase();
                break;

            case ShieldChargerState.Prepare:
                HandlePrepare();
                break;

            case ShieldChargerState.Dash:
                HandleDash();
                break;

            case ShieldChargerState.Recover:
                HandleRecover();
                break;

            case ShieldChargerState.Shoot:
                HandleShoot();
                break;

            case ShieldChargerState.Cooldown:
                HandleCooldown();
                break;
        }
    }

    private void ChangeState(ShieldChargerState newState)
    {
        shieldChargerState = newState;
        ApplyAnimatorState(shieldChargerState);

        switch (shieldChargerState)
        {
            case ShieldChargerState.Chase:
                EndShootState();
                stateTimer = 0f;
                break;

            case ShieldChargerState.Prepare:
                EndShootState();
                stateTimer = prepareDuration;
                dashDirection = GetDirectionToTarget();
                break;

            case ShieldChargerState.Dash:
                EndShootState();
                stateTimer = dashDuration;
                hasHitPlayerThisDash = false;
                PlaySfxAtSelf(dashStartSfx, dashStartSfxVolume);
                break;

            case ShieldChargerState.Recover:
                EndShootState();
                stateTimer = recoverDuration;
                break;

            case ShieldChargerState.Shoot:
                BeginShootState(shootDuration, shootStartDelay);
                break;

            case ShieldChargerState.Cooldown:
                EndShootState();
                stateTimer = cooldownDuration;
                break;
        }
    }

    private void ApplyAnimatorState(ShieldChargerState state)
    {
        if (animator == null)
            return;

        switch (state)
        {
            case ShieldChargerState.Chase:
                animator.SetInteger(BaseStateHash, 0);
                animator.SetInteger(LeftArmStateHash, 0);
                break;

            case ShieldChargerState.Prepare:
                animator.SetInteger(BaseStateHash, 1);
                animator.SetInteger(LeftArmStateHash, 0);
                break;

            case ShieldChargerState.Dash:
                animator.SetInteger(BaseStateHash, 2);
                animator.SetInteger(LeftArmStateHash, 0);
                break;

            case ShieldChargerState.Recover:
                animator.SetInteger(BaseStateHash, 3);
                animator.SetInteger(LeftArmStateHash, 1);
                break;

            case ShieldChargerState.Shoot:
                animator.SetInteger(BaseStateHash, 3);
                animator.SetInteger(LeftArmStateHash, 1);
                break;

            case ShieldChargerState.Cooldown:
                animator.SetInteger(BaseStateHash, 1);
                animator.SetInteger(LeftArmStateHash, 1);
                break;
        }
    }

    private void HandleChase()
    {
        SetAimMode(AimMode.BodyFaceTarget);

        float distance = DistanceToTarget();
        if (distance > dashTriggerRange)
        {
            RotateBodyTowardsTarget(bodyTurnSpeed);
            MoveTowardsTarget(bodyMoveSpeed);
        }
        else
        {
            ChangeState(ShieldChargerState.Prepare);
        }
    }

    private void HandlePrepare()
    {
        SetAimMode(AimMode.BodyFaceTarget);

        dashDirection = GetDirectionToTarget();

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChangeState(ShieldChargerState.Dash);
        }
    }

    private void HandleDash()
    {
        SetAimMode(AimMode.BodyFaceTarget);

        RotateBodyTowardsDirection(dashDirection, bodyTurnSpeed);
        MoveInDirection(dashDirection, dashSpeed);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChangeState(ShieldChargerState.Recover);
        }
    }

    private void HandleRecover()
    {
        SetAimMode(AimMode.BodyFaceTarget);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChangeState(ShieldChargerState.Shoot);
        }
    }

    private void HandleShoot()
    {
        if (UpdateShootState())
        {
            ChangeState(ShieldChargerState.Cooldown);
        }
    }

    private void HandleCooldown()
    {
        SetAimMode(AimMode.BodyFaceTarget);

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            ChangeState(ShieldChargerState.Chase);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (shieldChargerState != ShieldChargerState.Dash)
            return;

        if (hasHitPlayerThisDash)
            return;

        if (!other.CompareTag(playerTag))
            return;

        if (TryDamageAndKnockbackPlayer(other))
        {
            hasHitPlayerThisDash = true;
        }
    }

    private bool TryDamageAndKnockbackPlayer(Collider other)
    {
        if (other == null)
            return false;

        PlayerStateController playerStateController = other.GetComponent<PlayerStateController>();
        if (playerStateController == null)
        {
            playerStateController = other.GetComponentInParent<PlayerStateController>();
        }

        if (playerStateController == null)
            return false;

        playerStateController.TakeDamage(dashCollisionDamage);
        PlayDashHitPlayerSfx(other);
        TriggerPlayerHitFeedback(other);

        CharacterController characterController = other.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = other.GetComponentInParent<CharacterController>();
        }

        if (characterController == null)
            return true;

        Vector3 knockbackDirection = dashDirection;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude <= 0.0001f)
            return true;

        knockbackDirection.Normalize();

        PlayerController playerController = characterController.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = characterController.GetComponentInParent<PlayerController>();
        }

        StartCoroutine(ApplyAirKnockback(
            characterController,
            playerController,
            knockbackDirection,
            dashKnockbackDistance,
            dashKnockbackDuration,
            dashKnockbackHeight));

        return true;
    }

    private void PlayDashHitPlayerSfx(Collider other)
    {
        if (dashHitPlayerSfx == null || AudioManager.Instance == null)
            return;

        Vector3 hitPoint = transform.position;

        if (other != null)
        {
            hitPoint = other.ClosestPoint(transform.position);

            if (hitPoint == Vector3.zero)
            {
                hitPoint = other.transform.position;
            }
        }

        AudioManager.Instance.PlaySFXAtPoint(dashHitPlayerSfx, hitPoint, dashHitPlayerSfxVolume);
    }

    private void TriggerPlayerHitFeedback(Collider other)
    {
        if (other == null)
            return;

        PlayerHitFeedback feedback = other.GetComponent<PlayerHitFeedback>();
        if (feedback == null)
        {
            feedback = other.GetComponentInParent<PlayerHitFeedback>();
        }

        if (feedback != null)
        {
            feedback.SendMessage("OnDamaged", SendMessageOptions.DontRequireReceiver);
        }
    }

    private System.Collections.IEnumerator ApplyAirKnockback(
        CharacterController characterController,
        PlayerController playerController,
        Vector3 direction,
        float distance,
        float duration,
        float height)
    {
        if (characterController == null)
            yield break;

        if (duration <= 0f)
        {
            characterController.Move(direction * distance);
            yield break;
        }

        bool hadPlayerController = playerController != null;
        if (hadPlayerController)
        {
            playerController.enabled = false;
        }

        Vector3 startPosition = characterController.transform.position;
        Vector3 flatDirection = direction;
        flatDirection.y = 0f;
        flatDirection.Normalize();

        float elapsed = 0f;
        Vector3 previousTargetPosition = startPosition;

        try
        {
            while (elapsed < duration)
            {
                if (characterController == null)
                    yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 horizontalOffset = flatDirection * (distance * t);
                float verticalOffset = 4f * height * t * (1f - t);

                Vector3 targetPosition = startPosition + horizontalOffset + Vector3.up * verticalOffset;
                Vector3 frameMotion = targetPosition - previousTargetPosition;

                characterController.Move(frameMotion);
                previousTargetPosition = targetPosition;

                yield return null;
            }

            Vector3 finalTargetPosition = startPosition + flatDirection * distance;
            Vector3 finalMotion = finalTargetPosition - previousTargetPosition;
            characterController.Move(finalMotion);
        }
        finally
        {
            if (hadPlayerController && playerController != null)
            {
                playerController.enabled = true;
            }
        }
    }

    public override void Hit(Bullet bullet)
    {
        if (IsBlockingState())
        {
            PlaySfxAtSelf(blockedBulletSfx, blockedBulletSfxVolume);
            return;
        }

        base.Hit(bullet);
    }

    private bool IsBlockingState()
    {
        switch (shieldChargerState)
        {
            case ShieldChargerState.Chase:
            case ShieldChargerState.Prepare:
            case ShieldChargerState.Dash:
                return true;

            case ShieldChargerState.Recover:
            case ShieldChargerState.Shoot:
            case ShieldChargerState.Cooldown:
            default:
                return false;
        }
    }

    private void PlaySfxAtSelf(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySFXAtPoint(clip, transform.position, volumeScale);
    }
}