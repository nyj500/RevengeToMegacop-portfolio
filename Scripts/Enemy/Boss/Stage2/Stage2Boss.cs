using System.Collections;

using UnityEngine;

/// <summary>
/// 2스테이지 보스
/// 원거리 궁수 보스. 반사탄과 처형(최대 HP 15%)으로만 데미지를 줄 수 있다
/// Phase1(HP 100%~30%): 단발 조준, 3연발 리듬, 화살비, 순간이동
/// Phase2(HP 30%~0%): 5연발 가속, 유도 화살, 분신 소환, 순간이동
/// </summary>
public class Stage2Boss : BossEnemy
{
    [Header("Stage2 Boss Settings")]
    [SerializeField] private float maxDamagePerHitRatio = 0.15f;
    [SerializeField] private Stage2BossAnimator bossAnimator;
    [SerializeField] private Transform weaponPoint;
    [Header("Knockback")]
    [SerializeField] private float knockbackDistance = 1.5f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("Sound")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;

    private bool isTeleporting = false;

    /// <summary>순간이동 시작/종료 시 TeleportPattern에서 호출. 텔레포트 중 넉백을 차단한다.</summary>
    public void SetTeleporting(bool value) { isTeleporting = value; }

    /// <summary>화살 발사 기준점. 미설정 시 보스 루트 위치를 사용한다.</summary>
    public Transform WeaponPoint => weaponPoint != null ? weaponPoint : transform;

    [Header("Arrow Rain (Background Hazard)")]
    [SerializeField] private ArrowRainPattern arrowRainPattern;

    [Header("Phase1 Patterns")]
    [SerializeField] private BossPattern[] phase1Patterns;

    [Header("Phase2 Patterns")]
    [SerializeField] private BossPattern[] phase2Patterns;

    [SerializeField] private BossStrafeMovement strafeMovement;

    /// <summary>공격 패턴 시작 시 호출. 스트레이핑 이동을 일시정지한다.</summary>
    public void PauseMovement()
    {
        if (strafeMovement != null) strafeMovement.Pause();
    }

    /// <summary>공격 패턴 종료 시 호출. 스트레이핑 이동을 재개한다.</summary>
    public void ResumeMovement()
    {
        if (strafeMovement != null) strafeMovement.Resume();
    }


    /// <summary>
    /// 반사탄/처형만 데미지 허용. 1회 최대 데미지는 MaxHp의 15%.
    /// base.Hit() 호출 후 HP가 너무 많이 깎였으면 보정한다.
    /// </summary>
    public override void Hit(Bullet bullet)
    {
        if (bullet == null) return;

        float hpBefore = Hp;
        float maxDamage = MaxHp * maxDamagePerHitRatio;
        float minHpAfterHit = hpBefore - maxDamage;
        if (minHpAfterHit < 0f) minHpAfterHit = 0f;

        base.Hit(bullet);

        // HP가 0이 됐으면 TriggerDeathSequence가 이미 호출됨 — 복구 금지
        if (Hp > 0f && Hp < minHpAfterHit)
        {
            SetHp(minHpAfterHit);
        }

        // 사망 처리 중에는 Hit 애니메이션 생략 (Die 트리거 방해 방지)
        if (Hp > 0f)
        {
            bossAnimator?.PlayHit();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(hitSound);
        }
        // 반사탄 방향으로 넉백
        Vector3 knockbackDirection = bullet.transform.forward;
        knockbackDirection.y = 0f;
        knockbackDirection.Normalize();
        StartCoroutine(KnockbackCoroutine(knockbackDirection));


        Debug.Log($"[Stage2Boss] HP: {Hp}/{MaxHp} ({Mathf.RoundToInt(HpRatio * 100)}%)");
    }

    /// <summary>
    /// 처형 시 즉사 대신 최대 HP의 15% 데미지를 준다. 슬라이스 없이 슬래시 VFX만 재생한다.
    /// HP가 0 이하가 되면 base.Die()로 진짜 사망 처리한다.
    /// </summary>
    public override ExecutionResult HandleExecution(ExecutionContext context)
    {
        if (context.SlashVfx != null)
            context.SlashVfx.Play(context.SlicePosition, context.SlashDirection);

        float damage = MaxHp * maxDamagePerHitRatio;
        float newHp = Hp - damage;
        if (newHp < 0f) newHp = 0f;
        SetHp(newHp);
        CheckPhaseTransition();

        // 사망하지 않을 때만 피격 애니메이션 재생 (Die 트리거 방해 방지)
        if (newHp > 0f)
        {
            bossAnimator?.PlayHit();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(hitSound);
        }

        if (newHp <= 0f)
            TriggerDeathSequence();
        return new ExecutionResult
        {
            Target = this,
            Position = context.SlicePosition
        };
    }

    /// <summary>
    /// 유도 화살 명중 시 호출. MaxHp × damageRatio 만큼 HP를 깎는다.
    /// GuidedArrowBullet에서 사용. SetHp()가 protected이므로 이 메서드로 중개한다.
    /// </summary>
    public void TakeGuidedArrowDamage(float damageRatio)
    {
        float damage = MaxHp * damageRatio;
        float newHp = Hp - damage;
        if (newHp < 0f) newHp = 0f;
        SetHp(newHp);
    }

    protected override void StopAllPatterns()
    {
        base.StopAllPatterns();
        StopPatternArray(phase1Patterns);
        StopPatternArray(phase2Patterns);
    }

    private void StopPatternArray(BossPattern[] patterns)
    {
        if (patterns == null) return;
        for (int i = 0; i < patterns.Length; i++)
        {
            if (patterns[i] != null)
                patterns[i].StopAllCoroutines();
        }
    }

    protected override BossPattern[] GetPatternsForPhase(int phaseIndex)
    {
        return phaseIndex switch
        {
            0 => phase1Patterns,
            1 => phase2Patterns,
            _ => phase1Patterns
        };
    }

    protected override void OnPhaseChanged(int phaseIndex, BossPhaseData data)
    {
        // Phase2 진입 시 색상 변경 (임시 시각 피드백)
        if (phaseIndex == 1)
        {
            // 패턴 실행 중 강제 전환된 경우 이동 정지 상태를 복구
            ResumeMovement();

            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }

            Debug.Log("[Stage2Boss] Phase2 진입");
        }
    }

    protected override IEnumerator OnBossIntro()
    {

        // 보스전 시작 시 화살비 가동
        if (arrowRainPattern != null)
        {
            arrowRainPattern.StartRain(Target, gameObject);
        }
        if (strafeMovement != null) strafeMovement.StartStrafe(Target);

        yield break;
    }

    protected override IEnumerator OnBossDeath()
    {
        // 이동 및 화살비 즉시 정지
        if (strafeMovement != null) strafeMovement.StopStrafe();
        if (arrowRainPattern != null) arrowRainPattern.StopRain();

        // 분신 사망 트리거 — 보스 애니메이션과 동시에 클론도 사망 연출 시작
        BossClone[] clones = FindObjectsByType<BossClone>(FindObjectsSortMode.None);
        for (int i = 0; i < clones.Length; i++)
        {
            if (clones[i] != null) clones[i].TriggerDeath();
        }

        // 사망 애니메이션 재생 및 완료 대기
        if (bossAnimator != null)
        {
            bossAnimator.PlayDie();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(deathSound);
            yield return StartCoroutine(bossAnimator.WaitForDieAnimation());
        }
    }
    private IEnumerator KnockbackCoroutine(Vector3 direction)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + direction * knockbackDistance;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            // 순간이동이 시작됐으면 넉백 취소 (텔레포트 위치를 덮어쓰지 않도록)
            if (isTeleporting) yield break;

            elapsed += Time.deltaTime;
            float ratio = elapsed / knockbackDuration;
            transform.position = Vector3.Lerp(startPosition, endPosition, ratio);
            yield return null;
        }

        if (!isTeleporting)
            transform.position = endPosition;
    }
}
