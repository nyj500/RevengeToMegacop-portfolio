# Revenge to Megacop

> Unity 6 3D 탑다운 액션 게임 | 팀 프로젝트 (4인)

> 팀 프로젝트 특성상 유료 에셋이 포함되어 있어 스크립트 코드만 공개합니다.

## 게임 소개

총·투척 검·텔레포트 수리검을 조합해 적 웨이브를 돌파하는 3D 탑다운 슈터.
날아오는 총알을 타이밍에 맞춰 패리하거나 가드로 튕겨낼 수 있으며, 적을 약화시키면 처형으로 즉사시킬 수 있다.
스테이지마다 고유한 패턴을 가진 보스가 등장하며, 보스 공격을 역이용하는 전투 구조가 핵심이다.

## 담당 역할

**Stage 1 보스 전담 — 패턴·애니메이션·VFX·SFX**

- 4종 공격 패턴 코루틴 구현 (일반 사격·가이드 미사일·폭탄·웨이브)
- 패턴 실행 중 NavMesh 키팅 이동 병행
- 실드 반사 메커니즘 설계 (패링한 투사체를 실드가 다시 플레이어에게 반사)
- HP 50% 도달 시 실드 재생성 및 스턴 진입 로직
- 스턴 중 보조 몹 소환, 스턴 해제 후 복귀 애니메이션 연동
- 패턴별 파티클 이펙트 및 공간음향 SFX 연동
- 사망 시 서서히 가라앉는 Death 연출 구현

## 주요 구현

### Animation Event 기반 타이밍 동기화

패턴 코루틴이 Animator 내부 상태를 직접 폴링하면 결합도가 높아진다. `RegisterFireCallback()` / `RegisterAnimationCompleteCallback()`으로 콜백을 등록하고, Animation Event 발생 시점에 실행되는 구조로 분리. 패턴 코루틴은 `WaitUntil`로 대기하며, 발사 타이밍은 애니메이션이 결정한다.

```csharp
stage1Boss.RegisterFireCallback(() => fireReady = true);
stage1Boss.BossAnimator.SetTrigger("StartFire");
yield return new WaitUntil(() => fireReady);
// ...투사체 연속 발사...
yield return new WaitUntil(() => animComplete);
```

### 실드 반사 메커니즘 — 투사체 유형별 반사 처리 분기

실드를 `IDamageable`로 구현해 기존 피격 시스템에 통합. 반사 시 총알의 `owner`를 실드로 교체해 재충돌을 방지하고, 방향을 플레이어 쪽으로 재설정한다.

투사체 유형마다 반사 후 동작이 달랐기 때문에 베이스 클래스에 `OnReflected()` 콜백 확장 지점을 설계하고 각 투사체에서 오버라이드:

```csharp
// 가이드 미사일 — 호밍 대상 전환
protected override void OnReflected(bool isParry)
{
    target = (target == playerTarget) ? bossTransform : playerTarget;
}
```

- **가이드 미사일**: 호밍 대상을 보스로 전환
- **폭탄**: 포물선 궤도 전체 재계산, 실드 경유 시 보스 스턴 중복 방지

## 기술 스택

Unity 6 · C# · URP · NavMesh · Particle System · Animation Rigging
