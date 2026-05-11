using UnityEngine;

[CreateAssetMenu(fileName = "BossPhaseData", menuName = "Boss/PhaseData")]
public class BossPhaseData : ScriptableObject
{
    [Tooltip("HP ratio (0-1) at which this phase activates")]
    [Range(0f, 1f)]
    [SerializeField] private float hpThreshold = 1f;

    [SerializeField] private float moveSpeedMultiplier = 1f;
    [SerializeField] private float attackSpeedMultiplier = 1f;

    public float HpThreshold => hpThreshold;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public float AttackSpeedMultiplier => attackSpeedMultiplier;
}
