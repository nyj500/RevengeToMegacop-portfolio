using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 모든 SkillData를 하나로 모아 관리하는 데이터베이스 에셋.
/// SkillManager가 이 에셋 하나만 참조하면 전체 스킬 정보에 접근할 수 있다.
/// </summary>
[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Skill/SkillDatabase")]
public class SkillDatabase : ScriptableObject
{
    [SerializeField] private SkillData[] skills;

    private Dictionary<SkillId, SkillData> lookup;

    /// <summary>
    /// ID로 스킬 데이터를 조회한다. 존재하지 않으면 null을 반환한다.
    /// </summary>
    public SkillData GetSkill(SkillId id)
    {
        if (lookup == null) BuildLookup();
        return lookup.TryGetValue(id, out var data) ? data : null;
    }

    /// <summary>
    /// 데이터베이스에 등록된 모든 스킬 목록을 반환한다.
    /// </summary>
    public IReadOnlyList<SkillData> GetAllSkills() => skills;

    private void BuildLookup()
    {
        lookup = new Dictionary<SkillId, SkillData>(skills.Length);
        foreach (var skill in skills)
        {
            if (skill != null)
                lookup[skill.SkillId] = skill;
        }
    }
}
