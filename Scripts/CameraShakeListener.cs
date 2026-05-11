using UnityEngine;

/// <summary>
/// 전투 이벤트(피격/처형/적 사망)를 구독하여 CameraShake.Shake()를 호출하는 컴포넌트.
/// EnemySpawner에서 RegisterEnemy()를 통해 동적으로 생성된 적도 등록한다.
/// </summary>
public class CameraShakeListener : MonoBehaviour
{
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private PlayerHitController playerHitController;
    [SerializeField] private PlayerExecutionController playerExecutionController;

    [Header("피격")]
    [SerializeField] private float damageIntensity = 0.5f;
    [SerializeField] private float damageDuration = 0.25f;

    [Header("처형")]
    [SerializeField] private float executionIntensity = 0.5f;
    [SerializeField] private float executionDuration = 0.3f;

    [Header("적 사망")]
    [SerializeField] private float enemyDeathIntensity = 0.1f;
    [SerializeField] private float enemyDeathDuration = 0.1f;

    void Start()
    {
        if (playerHitController != null)
        {
            playerHitController.OnDamaged += OnDamaged;
        }

        if (playerExecutionController != null)
        {
            playerExecutionController.OnExecutionComplete += OnExecutionComplete;
        }
    }

    void OnDestroy()
    {
        if (playerHitController != null)
        {
            playerHitController.OnDamaged -= OnDamaged;
        }

        if (playerExecutionController != null)
        {
            playerExecutionController.OnExecutionComplete -= OnExecutionComplete;
        }

    }

    /// <summary>
    /// EnemySpawner가 적을 생성할 때 호출하여 적 사망 셰이크를 등록한다.
    /// </summary>
    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        enemy.OnDeath += OnEnemyDeath;
    }

    private void OnDamaged() => cameraShake?.Shake(damageIntensity, damageDuration);
    private void OnExecutionComplete(ExecutionResult result) => cameraShake?.Shake(executionIntensity, executionDuration);
    private void OnEnemyDeath(Enemy enemy) => cameraShake?.Shake(enemyDeathIntensity, enemyDeathDuration);
}
