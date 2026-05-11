using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// 개별 스킬의 메타데이터를 정의하는 데이터 에셋.
/// 스킬트리 UI에서 이름, 설명, 아이콘, 선행 조건을 표시하는 데 사용한다.
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "Skill/SkillData")]
public class SkillData : ScriptableObject
{
    [SerializeField] private SkillId skillId;
    [SerializeField] private string skillName;
    [TextArea][SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private SkillData[] prerequisites;

    public SkillId SkillId => skillId;
    public string SkillName => skillName;
    public string Description => description;
    public Sprite Icon => icon;

    /// <summary>
    /// 이 스킬을 해금하기 위해 먼저 해금되어야 하는 선행 스킬 목록.
    /// 비어 있으면 즉시 해금 가능하다.
    /// </summary>
    public IReadOnlyList<SkillData> Prerequisites => prerequisites;
}
