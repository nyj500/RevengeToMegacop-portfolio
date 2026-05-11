using UnityEngine;

/// <summary>
/// Enemy.OnDeath 이벤트를 구독하여 EnemyDeathEffect.Play()를 호출하는 컴포넌트.
/// EnemySpawner가 적을 생성할 때 RegisterEnemy()를 호출하여 등록한다.
/// 처형 사망은 Enemy.WasExecuted 플래그로 감지하여 제외된다.
/// </summary>
public class EnemyDeathEffectListener : MonoBehaviour
{
    [SerializeField] private EnemyDeathEffect enemyDeathEffect;

    /// <summary>
    /// EnemySpawner가 적을 생성할 때 호출하여 사망 이펙트를 등록한다.
    /// </summary>
    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        enemy.OnDeath += OnEnemyDeath;
    }

    private void OnEnemyDeath(Enemy enemy)
    {
        if (enemy == null) return;

        // 처형 사망은 스킵 (HandleExecution이 처형 전용 VFX를 담당)
        if (enemy.WasExecuted) return;

        if (enemyDeathEffect != null)
            enemyDeathEffect.Play(enemy.transform.position);
    }
}
