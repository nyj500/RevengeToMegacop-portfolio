using System.Collections;

using UnityEngine;

/// <summary>
/// 보스 적의 베이스 클래스. Enemy를 상속하며 페이즈/패턴 시스템을 제공한다.
///
/// <para><b>상속 방법:</b></para>
/// <list type="number">
///   <item>이 클래스를 상속한 구체 보스 클래스를 만든다.</item>
///   <item>abstract 메서드 2개를 반드시 구현한다:
///     <see cref="OnPhaseChanged"/> — 페이즈 전환 시 연출/상태 변경,
///     <see cref="GetPatternsForPhase"/> — 해당 페이즈에서 사용할 패턴 배열 반환.</item>
///   <item>필요하면 virtual 메서드를 override한다:
///     <see cref="OnBossIntro"/> — 등장 연출,
///     <see cref="OnBossDeath"/> — 사망 연출.</item>
/// </list>
///
/// <para><b>사용 흐름:</b></para>
/// <code>
/// ActivateBoss(playerTransform)
///   → OnBossIntro() (인트로 코루틴)
///   → Active 상태: 패턴 가중치 기반 실행
///   → HP 감소 시 페이즈 전환 → OnPhaseChanged() + GetPatternsForPhase()
///   → HP 0 → OnBossDeath() (사망 연출) → Die()
/// </code>
/// </summary>
public abstract class BossEnemy : Enemy
{
    [Header("Boss Settings")]
    [SerializeField] private BossPhaseData[] phases;
    [SerializeField] private float interPatternDelay = 0.5f;

    private int currentPhaseIndex = -1;
    private BossPattern[] currentPatterns;
    private float nextPatternTime;

    private enum BossState { Inactive, Intro, Active, PatternExecuting, Death }
    private BossState bossState = BossState.Inactive;

    /// <summary>
    /// Enemy.Hit()을 override하여 보스 전용 로직을 적용한다.
    /// Inactive/Death 상태에서는 피격을 무시하며, HP 변경 후 페이즈 전환을 체크한다.
    /// 부모의 Hit()과 달리 SetHp()를 통해 HP를 변경하고 Die() 대신 DeathSequence를 사용한다.
    /// </summary>
    public override void Hit(Bullet bullet)
    {
        if (bullet == null) return;
        if (bossState == BossState.Death || bossState == BossState.Inactive) return;

        float newHp = Hp - bullet.Damage;
        if (newHp < 0f) newHp = 0f;
        SetHp(newHp);

        CheckPhaseTransition();

        if (Hp <= 0f)
        {
            TriggerDeathSequence();
        }
        else
        {
            InvokeOnHit();
        }
    }

    /// <summary>
    /// 외부에서 보스 전투를 시작할 때 호출한다.
    /// target을 설정하고 HP를 최대치로 초기화한 뒤, 첫 번째 페이즈 진입 및 인트로 연출을 실행한다.
    /// </summary>
    public void ActivateBoss(Transform target)
    {
        SetTarget(target);
        SetHp(MaxHp);
        EnterPhase(0);
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        bossState = BossState.Intro;
        yield return StartCoroutine(OnBossIntro());
        bossState = BossState.Active;
    }

    /// <summary>
    /// 보스 사망 시퀀스를 시작한다. OnBossDeath() 연출 후 Die()를 호출한다.
    /// 이미 Death 상태이면 무시한다. 자식 클래스에서 처형 등으로 HP가 0이 될 때 사용한다.
    /// </summary>
    protected void TriggerDeathSequence()
    {
        if (bossState == BossState.Death) return;
        bossState = BossState.Death;
        StopAllPatterns();
        StartCoroutine(DeathSequence());
    }

    /// <summary>
    /// 사망 시 현재 실행 중인 패턴 코루틴을 모두 중단한다.
    /// 자식 클래스에서 override하여 추가 패턴 배열도 중단할 수 있다.
    /// </summary>
    protected virtual void StopAllPatterns()
    {
        if (currentPatterns == null) return;
        for (int i = 0; i < currentPatterns.Length; i++)
        {
            if (currentPatterns[i] != null)
                currentPatterns[i].StopAllCoroutines();
        }
    }

    private IEnumerator DeathSequence()
    {
        yield return StartCoroutine(OnBossDeath());
        Die();
    }

    /// <summary>
    /// Enemy.Update()를 override하여 부모의 FSM 대신 보스 전용 패턴 실행 루프를 사용한다.
    /// Active 상태일 때만 다음 패턴을 실행한다.
    /// </summary>
    protected override void Update()
    {
        if (bossState == BossState.Active && Time.time >= nextPatternTime)
        {
            ExecuteNextPattern();
        }
    }

    private void ExecuteNextPattern()
    {
        if (currentPatterns == null || currentPatterns.Length == 0) return;

        float totalWeight = 0f;
        int candidateCount = 0;

        for (int i = 0; i < currentPatterns.Length; i++)
        {
            if (currentPatterns[i].CanExecute())
            {
                totalWeight += currentPatterns[i].Weight;
                candidateCount++;
            }
        }

        if (candidateCount == 0)
        {
            nextPatternTime = Time.time + 0.1f;
            return;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        int lastCandidate = -1;

        for (int i = 0; i < currentPatterns.Length; i++)
        {
            if (!currentPatterns[i].CanExecute()) continue;

            cumulative += currentPatterns[i].Weight;
            lastCandidate = i;
            if (roll <= cumulative)
            {
                bossState = BossState.PatternExecuting;
                currentPatterns[i].Execute(this, OnPatternComplete);
                return;
            }
        }

        // 부동소수점 오차로 선택이 안 된 경우 마지막 유효 패턴 실행
        if (lastCandidate >= 0)
        {
            bossState = BossState.PatternExecuting;
            currentPatterns[lastCandidate].Execute(this, OnPatternComplete);
        }
    }

    private void OnPatternComplete()
    {
        if (bossState == BossState.Death) return;
        bossState = BossState.Active;
        nextPatternTime = Time.time + interPatternDelay;
    }

    protected void CheckPhaseTransition()
    {
        if (phases == null) return;

        for (int i = phases.Length - 1; i >= 0; i--)
        {
            if (i <= currentPhaseIndex) break;

            if (HpRatio <= phases[i].HpThreshold)
            {
                EnterPhase(i);
                break;
            }
        }
    }

    private void EnterPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phases == null || phaseIndex >= phases.Length) return;
        currentPhaseIndex = phaseIndex;
        currentPatterns = GetPatternsForPhase(phaseIndex);

        // 패턴 실행 중에 페이즈 전환이 발생하면 즉시 Active로 전환하여 다음 패턴부터 새 페이즈 적용
        if (bossState == BossState.PatternExecuting)
            bossState = BossState.Active;

        OnPhaseChanged(phaseIndex, phases[phaseIndex]);
    }

    /// <summary>
    /// [필수 구현] 페이즈가 전환될 때 호출된다.
    /// 이동 속도 변경, 외형 변화 등 페이즈별 상태 전환 로직을 구현한다.
    /// </summary>
    protected abstract void OnPhaseChanged(int phaseIndex, BossPhaseData data);

    /// <summary>
    /// [필수 구현] 해당 페이즈에서 사용할 BossPattern 배열을 반환한다.
    /// 반환된 패턴들은 Weight 기반 가중치 랜덤으로 선택·실행된다.
    /// </summary>
    protected abstract BossPattern[] GetPatternsForPhase(int phaseIndex);

    /// <summary>
    /// [선택 override] 보스 등장 연출 코루틴. 기본 구현은 즉시 완료된다.
    /// 카메라 연출, 등장 애니메이션 등을 구현할 수 있다.
    /// </summary>
    protected virtual IEnumerator OnBossIntro()
    {
        yield break;
    }

    /// <summary>
    /// [선택 override] 보스 사망 연출 코루틴. 기본 구현은 즉시 완료된다.
    /// 폭발 이펙트, 슬로우모션 등을 구현할 수 있다. 완료 후 Die()가 자동 호출된다.
    /// </summary>
    protected virtual IEnumerator OnBossDeath()
    {
        yield break;
    }
}
