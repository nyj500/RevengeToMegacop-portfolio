using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스킬 기반 서브컨트롤러의 베이스 클래스.
/// PlayerController가 GetComponents로 자동 수집하여 Tick/Handle을 호출한다.
/// [필수 구현] SkillId, InitializeSkill
/// [선택 override] Tick — 매 프레임 (쿨타임 등 해금 여부 무관한 업데이트)
/// [선택 override] Handle — 스킬 해금 시에만 호출 (입력 처리 등 핵심 로직)
/// </summary>
public abstract class PlayerSkillController : MonoBehaviour
{
    /// <summary>이 컨트롤러가 담당하는 스킬의 식별자.</summary>
    public abstract SkillId SkillId { get; }

    /// <summary>
    /// PlayerController가 Awake에서 호출한다.
    /// InputActionMap에서 필요한 InputAction을 직접 조회하여 저장한다.
    /// </summary>
    public abstract void InitializeSkill(InputActionMap playerMap);

    /// <summary>매 프레임 호출. 스킬 해금 여부와 무관하게 실행된다.</summary>
    public virtual void Tick() { }

    /// <summary>스킬이 해금된 경우에만 호출된다.</summary>
    public virtual void Handle() { }
}
