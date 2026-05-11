using System;

/// <summary>
/// 쿨타임을 가진 스킬 컨트롤러가 UI 표시용으로 노출하는 인터페이스.
/// PlayerSwordController, PlayerBlinkController 등이 구현한다.
/// </summary>
public interface IPlayerCooldownSource
{
    SkillId SkillId { get; }
    float CooldownDuration { get; }
    float CooldownRemaining { get; }
    /// <summary>1 = 방금 시작, 0 = 사용 가능. UI fillAmount에 직접 매핑된다.</summary>
    float CooldownProgress01 { get; }
    bool IsOnCooldown { get; }
    /// <summary>쿨타임이 새로 시작될 때 발행. UI가 갱신 루프 진입 트리거로 구독.</summary>
    event Action OnCooldownStarted;
}
