using System;

/// <summary>스킬 해금 상태의 세이브/로드용 직렬화 구조체.</summary>
[Serializable]
public struct SkillSaveData
{
    public SkillId[] unlockedIds;
}
