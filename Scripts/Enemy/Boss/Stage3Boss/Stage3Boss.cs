using UnityEngine;

namespace Boss3
{
    public class Stage3Boss : BossEnemy
{
    [Header("기본 패턴")]
    [SerializeField] private ScopePattern _scopePattern;
    [SerializeField] private SmokeBomb _smokeBombPattern;
    [SerializeField] private OscillatingBulletPattern _OscillatingBulletPattern;
    [SerializeField] private MovePattern _movePattern;

    [Header("강화 패턴")]
    [SerializeField] private OscillatingBulletPattern _StrongOscillatingBulletPattern;
    [SerializeField] private SmokeTeleportPattern _smokeTeleportPattern;

    [Header("보스 설정")]
    [SerializeField] private float _damagePerSlash;

        

    protected override BossPattern[] GetPatternsForPhase(int phaseIndex)
    {
        if (phaseIndex == 0)
        {
            return new BossPattern[] 
            {
               
                _OscillatingBulletPattern,
                _movePattern,
                _scopePattern,
                _smokeBombPattern
            };
        }
        if (phaseIndex == 1)
        {
            return new BossPattern[]
            {
                _StrongOscillatingBulletPattern,
                _smokeTeleportPattern,
                _scopePattern,
                
            };
        }

        return new BossPattern[0];
    }

    protected override void OnPhaseChanged(int phaseIndex, BossPhaseData data)
    {
        Debug.Log($"페이즈 {phaseIndex + 1} 진입!");

        if (phaseIndex == 1)
            {
                transform.position = Vector3.zero;
                //TODO 특정 모션

                
            }
    }

    public override void Hit(Bullet bullet)
    {
            

            base.Hit(bullet);
            Debug.Log("hit" + bullet);
            
    }

    public override ExecutionResult HandleExecution(ExecutionContext context)
    {
        
        if(context.SlashVfx != null)
        context.SlashVfx.Play(context.SlicePosition,context.SlashDirection);

        float damageAmount = MaxHp * _damagePerSlash;
        float newHp = Mathf.Max(0f,Hp-damageAmount);
        SetHp(newHp);

        CheckPhaseTransition();
        

        if(Hp <= 0f)
            {
                TriggerDeathSequence();
            }else
            {
                Debug.Log($"남은 보스의 Hp : {Hp}");
            }

        return new ExecutionResult
        {
        Target = this,
        Position = context.SlicePosition
        };
    }

    // AnimationEvent receiver: Robot_black_run* 클립에 박힌 Step 이벤트 수신용 (발소리 SFX 미연결 상태라 no-op)
    public void Step() { }

}
}