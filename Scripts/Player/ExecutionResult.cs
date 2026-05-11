using UnityEngine;

/// <summary>
/// 처형 완료 후 OnExecutionComplete 이벤트에 전달되는 결과 데이터.
/// </summary>
public struct ExecutionResult
{
    /// <summary>처형 대상 적. 킬캠, UI 표시, 콤보 카운트 등에서 사용한다.</summary>
    public Enemy Target;
    /// <summary>처형이 발생한 월드 좌표. 이펙트 스폰, 카메라 포커스 등에서 사용한다.</summary>
    public Vector3 Position;
}
