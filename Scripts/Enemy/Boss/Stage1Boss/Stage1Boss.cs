using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Stage1Boss : BossEnemy
{
    [SerializeField] private Transform player;
    [SerializeField] private Stage1BossShield shield;
    [SerializeField] private Collider shieldHitbox;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float bossMoveSpeed = 3f;
    
    [SerializeField] private Animator bossAnimator;

    [SerializeField] private float kiteDistance = 8f;
    [SerializeField] private float kiteSwitchInterval = 1.5f;
    [SerializeField] private float shieldRegenHpThreshold = 0.5f;
    [SerializeField] private float maxExecutionDamageRatio = 0.15f;
    [SerializeField] private float stunDuration = 3f;
    [SerializeField] private GameObject mobPrefab;
    [SerializeField] private float spawnTriangleHeight = 5f;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;

    private BasicShotPattern basicShotPattern;
    private GuidedMissilePattern guidedMissilePattern;
    private BombPattern bombPattern;
    private WavePattern wavePattern;

    private NavMeshAgent bossAgent;
    private bool shieldRegenDone;
    private bool isPatternExecuting;
    private bool isKiting;
    private bool isStunned;
    private bool isLookingLocked;
    private float kiteDirection = 1f;
    private float kiteSwitchTimer;
    private Action pendingFireCallback;
    private Action pendingAnimationCompleteCallback;
    private Action pendingPatternCompleteCallback;
    private float deathSinkAmount = 0.7f;
    private float deathSinkDuration = 1.5f;

    public Animator BossAnimator => bossAnimator;

    public void NotifyPatternStart() => isPatternExecuting = true;
    public void NotifyPatternEnd()
    {
        isPatternExecuting = false;
        pendingPatternCompleteCallback = null;
    }

    public void StartKiting()
    {
        isKiting = true;
        kiteSwitchTimer = kiteSwitchInterval;
    }

    public void StopKiting()
    {
        isKiting = false;
        if (bossAgent != null) bossAgent.ResetPath();
    }

    public void LockLooking() => isLookingLocked = true;
    public void UnlockLooking() => isLookingLocked = false;

    public void RegisterFireCallback(Action callback) => pendingFireCallback = callback;
    public void RegisterAnimationCompleteCallback(Action callback) => pendingAnimationCompleteCallback = callback;
    public void RegisterPatternCompleteCallback(Action callback) => pendingPatternCompleteCallback = callback;

    // Animation Event에서 호출 — 발사 시작 신호
    public void OnFireAnimationEvent()
    {
        pendingFireCallback?.Invoke();
        pendingFireCallback = null;
    }

    // Animation Event에서 호출 — 애니메이션 완료 신호
    public void OnAnimationCompleteEvent()
    {
        pendingAnimationCompleteCallback?.Invoke();
        pendingAnimationCompleteCallback = null;
    }

    void Awake()
    {
        basicShotPattern = GetComponent<BasicShotPattern>();
        guidedMissilePattern = GetComponent<GuidedMissilePattern>();
        bombPattern = GetComponent<BombPattern>();
        wavePattern = GetComponent<WavePattern>();
    }

    protected override void Start()
    {
        base.Start();
        bossAgent = GetComponent<NavMeshAgent>();
        if (player != null) ActivateBoss(player);
        if (shield != null)
        {
            shield.Initialize(player);
            shield.OnShieldChanged += OnShieldChanged;
        }
        bool shieldActive = shield != null && shield.IsActive;
        bossAnimator?.SetBool("HasShield", shieldActive);
        if (shieldHitbox != null) shieldHitbox.enabled = shieldActive;
    }

    private void OnShieldChanged(float ratio)
    {
        bool shieldActive = ratio > 0f;
        bossAnimator?.SetBool("HasShield", shieldActive);
        if (shieldHitbox != null) shieldHitbox.enabled = shieldActive;
    }

    public void EnterStun()
    {
        if (Hp <= 0f) return;
        if (shield != null && shield.IsActive) return;
        isStunned = true;
        StopKiting();
        pendingPatternCompleteCallback?.Invoke();
        pendingPatternCompleteCallback = null;
        StopAllCoroutines();
        basicShotPattern?.StopAllCoroutines();
        guidedMissilePattern?.StopAllCoroutines();
        bombPattern?.StopAllCoroutines();
        wavePattern?.StopAllCoroutines();
        isPatternExecuting = false;
        isLookingLocked = false;
        pendingFireCallback = null;
        pendingAnimationCompleteCallback = null;
        bossAnimator?.SetBool("IsStunned", true);
        SpawnMob();
        StartCoroutine(StunRoutine());
    }

    private void SpawnMob()
    {
        if (mobPrefab == null || Target == null) return;

        Vector3 bossPos = transform.position;
        Vector3 playerPos = Target.position;
        Vector3 midpoint = (bossPos + playerPos) * 0.5f;

        Vector3 forward = playerPos - bossPos;
        forward.y = 0f;
        if (forward == Vector3.zero) return;

        Vector3 right = Vector3.Cross(Vector3.up, forward.normalized);
        float side = UnityEngine.Random.value > 0.5f ? 1f : -1f;

        Vector3 spawnPos = midpoint + right * side * spawnTriangleHeight;
        spawnPos.y = 0f;

        GameObject mobObj = Instantiate(mobPrefab, spawnPos, Quaternion.identity);
        if (mobObj.TryGetComponent<Stage1StunMob>(out Stage1StunMob mob))
            mob.SetTarget(Target);
    }

    private IEnumerator StunRoutine()
    {
        yield return new WaitForSeconds(stunDuration);
        if (Hp <= 0f) yield break;
        bossAnimator?.SetBool("IsStunned", false);
        bossAnimator?.SetTrigger("StunRecover");
        bool recoverComplete = false;
        RegisterAnimationCompleteCallback(() => recoverComplete = true);
        yield return new WaitUntil(() => recoverComplete);
        isStunned = false;
    }

    protected override void Update()
    {
        if (Target == null) return;
        if (isStunned) return;

        float distance = Vector3.Distance(transform.position, Target.position);
        if (distance <= attackRange)
            base.Update(); // 사정거리 안에서만 패턴 실행

        MoveTowardTarget(distance);

        if (!isLookingLocked)
        {
            Vector3 direction = Target.position - transform.position;
            direction.y = 0f;
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void MoveTowardTarget(float distance)
    {
        if (isKiting)
        {
            UpdateKiteMovement(distance);
            return;
        }

        if (isPatternExecuting) return; // 패턴 실행 중에는 이동 안 함

        if (distance <= attackRange)
        {
            if (bossAgent != null) bossAgent.ResetPath();
            return;
        }

        if (bossAgent != null)
        {
            bossAgent.SetDestination(Target.position);
        }
        else
        {
            Vector3 dir = (Target.position - transform.position).normalized;
            dir.y = 0f;
            transform.position += bossMoveSpeed * Time.deltaTime * dir;
        }
    }

    private void UpdateKiteMovement(float distanceToTarget)
    {
        if (Target == null) return;

        kiteSwitchTimer -= Time.deltaTime;
        if (kiteSwitchTimer <= 0f)
        {
            kiteDirection = -kiteDirection;
            kiteSwitchTimer = kiteSwitchInterval;
        }

        Vector3 toPlayer = Target.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer == Vector3.zero) return;

        Vector3 forward = toPlayer.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 moveDir = right * kiteDirection;

        if (distanceToTarget < kiteDistance - 1f)
            moveDir -= forward; 
        else if (distanceToTarget > kiteDistance + 1f)
            moveDir += forward; 

        Vector3 targetPos = transform.position + moveDir.normalized * bossMoveSpeed * Time.deltaTime;

        if (bossAgent != null)
            bossAgent.SetDestination(targetPos);
        else
            transform.position = targetPos;
    }

    public override void Hit(Bullet bullet)
    {
        if (shield != null && shield.IsActive)
        {
            if (bullet is Stage1BossBomb)
                shield.Hit(bullet);
            return;
        }

        base.Hit(bullet);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPoint(hitSound, transform.position);
        // bossAnimator?.SetTrigger("Hit");

        if (bullet is not Stage1BossBomb bomb)
            bullet.Remove();
        Debug.Log($"Boss hit! Remaining HP: {Hp}");

        if (Hp > 0f && !shieldRegenDone && shield != null && HpRatio <= shieldRegenHpThreshold)
        {
            shieldRegenDone = true;
            if (bullet is Stage1BossBomb)
                EnterStun();
            shield.Regenerate();
        }
    }

    protected override BossPattern[] GetPatternsForPhase(int phaseIndex)
    {
        return new BossPattern[]
        {
            basicShotPattern,
            guidedMissilePattern,
            bombPattern,
            wavePattern
        };
    }

    public override ExecutionResult HandleExecution(ExecutionContext context)
    {
        if (context.SlashVfx != null)
            context.SlashVfx.Play(context.SlicePosition, context.SlashDirection);

        if (shield != null && shield.IsActive)
        {
            Debug.Log($"[Execution] 실드 처형. 현재 게이지: {shield.ShieldRatio:P0}");
            shield.TakeExecutionDamage();
        }
        else
        {
            float damage = MaxHp * maxExecutionDamageRatio;
            float newHp = Mathf.Max(0f, Hp - damage);
            SetHp(newHp);
            Debug.Log($"Boss execution hit! Remaining HP: {Hp}");

            if (newHp <= 0f)
                TriggerDeathSequence();
        }

        return new ExecutionResult
        {
            Target = this,
            Position = context.SlicePosition
        };
    }

    protected override void OnPhaseChanged(int phaseIndex, BossPhaseData data) { }

    protected override IEnumerator OnBossDeath()
    {
        SetTarget(null);
        if (bossAgent != null) bossAgent.ResetPath();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPoint(deathSound, transform.position);
        isStunned = false;
        bossAnimator?.SetBool("IsStunned", false);
        pendingAnimationCompleteCallback = null;
        pendingFireCallback = null;
        bool deathAnimComplete = false;
        RegisterAnimationCompleteCallback(() => deathAnimComplete = true);
        bossAnimator?.SetTrigger("Die");
        StartCoroutine(SinkDown());
        yield return new WaitUntil(() => deathAnimComplete);
    }

    private IEnumerator SinkDown()
    {
        yield return new WaitForSeconds(3.5f); // 애니메이션과 싱크 맞추기 위한 딜레이
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos - new Vector3(0f, deathSinkAmount, 0f);
        float elapsed = 0f;
        while (elapsed < deathSinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / deathSinkDuration);
            yield return null;
        }
        transform.position = endPos;
    }
}
