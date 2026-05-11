using System;

using UnityEngine;

/// <summary>
/// 보스 패턴 기반 클래스. cooldown과 weight로 패턴 선택 빈도를 제어한다.
/// BossEnemy가 Execute()를 호출하여 패턴을 실행하고, 패턴 완료 시 onComplete 콜백을 받는다.
/// [필수 구현] ExecutePattern() — 완료 시 onComplete 반드시 호출
/// </summary>
public abstract class BossPattern : MonoBehaviour
{
    [SerializeField] private float cooldown = 2f;
    [SerializeField] private float weight = 1f;

    private float lastExecuteTime = float.NegativeInfinity;

    public float Weight => weight;

    /// <summary>
    /// 쿨타임이 지났으면 true를 반환한다.
    /// </summary>
    public bool CanExecute()
    {
        return Time.time >= lastExecuteTime + cooldown;
    }

    /// <summary>
    /// 패턴을 실행한다. 쿨타임을 갱신하고 ExecutePattern()을 호출한다.
    /// </summary>
    public void Execute(BossEnemy boss, Action onComplete)
    {
        lastExecuteTime = Time.time;
        ExecutePattern(boss, onComplete);
    }

    /// <summary>
    /// 패턴 실제 로직. [필수 구현]
    /// 패턴이 완료되면 onComplete를 반드시 호출해야 BossEnemy가 다음 패턴을 실행할 수 있다.
    /// </summary>
    protected abstract void ExecutePattern(BossEnemy boss, Action onComplete);
}
