using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PixelArtGUI ActionBar 형태의 스킬 쿨타임 HUD.
/// GridLayoutGroup + ToggleGroup이 부착된 컨테이너에 같이 붙어,
/// displayedSkills 순서대로 SkillToggle 인스턴스를 런타임 생성한다.
/// </summary>
[RequireComponent(typeof(ToggleGroup))]
public class SkillCooldownActionBar : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private SkillDatabase skillDatabase;
    [SerializeField] private GameObject skillTogglePrefab;
    [SerializeField] private SkillId[] displayedSkills = { SkillId.SwordThrow, SkillId.Blink };

    private ToggleGroup toggleGroup;

    void Awake()
    {
        toggleGroup = GetComponent<ToggleGroup>();
    }

    void Start()
    {
        BuildEntries();
    }

    private void BuildEntries()
    {
        if (playerController == null || skillDatabase == null || skillTogglePrefab == null)
        {
            Debug.LogError("SkillCooldownActionBar: Required reference is missing.", this);
            return;
        }

        var skillControllers = playerController.GetComponents<PlayerSkillController>();

        foreach (var skillId in displayedSkills)
        {
            var skillData = skillDatabase.GetSkill(skillId);
            var cooldownSource = ResolveSource(skillControllers, skillId);
            if (skillData == null || cooldownSource == null)
            {
                Debug.LogWarning(
                    $"SkillCooldownActionBar: Skill {skillId} skipped (data={skillData}, source={cooldownSource}).",
                    this);
                continue;
            }

            var toggleInstance = Instantiate(skillTogglePrefab, transform, false);
            toggleInstance.name = $"SkillToggle_{skillId}";
            var entry = toggleInstance.AddComponent<SkillCooldownEntry>();
            entry.Bind(skillData, cooldownSource, toggleGroup);
        }
    }

    private static IPlayerCooldownSource ResolveSource(PlayerSkillController[] controllers, SkillId skillId)
    {
        foreach (var controller in controllers)
        {
            if (controller.SkillId == skillId && controller is IPlayerCooldownSource cooldownSource)
                return cooldownSource;
        }
        return null;
    }
}
