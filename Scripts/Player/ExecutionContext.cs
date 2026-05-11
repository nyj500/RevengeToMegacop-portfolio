using UnityEngine;

/// <summary>
/// 처형 시 적에게 전달되는 컨텍스트 데이터.
/// 적은 이 데이터를 사용하여 자신의 처형 반응(즉사, 퍼센트 데미지, VFX 여부 등)을 결정한다.
/// </summary>
public struct ExecutionContext
{
    public Vector3 SlicePosition;
    public Vector3 SliceNormal;
    public Vector3 SlashDirection;
    public ExecutionSliceEffect SliceEffect;
    public ExecutionSlashVfx SlashVfx;
}
