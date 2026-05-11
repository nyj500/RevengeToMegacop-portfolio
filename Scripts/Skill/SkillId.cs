/// <summary>
/// 게임 내 모든 스킬의 고유 식별자.
/// [필수 구현] 새 스킬 추가 시 이 enum에 항목을 추가하고 int 값을 명시적으로 지정한다.
/// int 값은 세이브 데이터 호환성을 위해 변경하지 않는다.
/// </summary>
public enum SkillId
{
    None = 0,
    Dash = 1,
    SwordThrow = 2,
    Blink = 3,
}
