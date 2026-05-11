using UnityEngine;

/// <summary>
/// 적 일반 사망 위치에 파티클 버스트를 재생하는 컴포넌트.
/// EnemyDeathEffectListener가 Play()를 호출하여 효과를 시작한다.
/// </summary>
public class EnemyDeathEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem deathVfxPrefab;
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0.5f, 0f);

    /// <summary>
    /// 지정 위치에 사망 파티클을 재생한다. 재생 완료 후 자동 파괴된다.
    /// </summary>
    /// <param name="position">적의 월드 위치</param>
    public void Play(Vector3 position)
    {
        if (deathVfxPrefab == null) return;

        Vector3 spawnPosition = position + positionOffset;
        ParticleSystem instance = Instantiate(deathVfxPrefab, spawnPosition, Quaternion.identity);
        instance.Play(true);

        ParticleSystem[] childSystems = instance.GetComponentsInChildren<ParticleSystem>();
        Destroy(instance.gameObject, GetTotalDuration(childSystems));
    }

    private float GetTotalDuration(ParticleSystem[] systems)
    {
        float maxDuration = 0f;
        foreach (ParticleSystem system in systems)
        {
            ParticleSystem.MainModule main = system.main;
            float systemDuration = main.duration + main.startLifetime.constantMax;
            if (systemDuration > maxDuration)
            {
                maxDuration = systemDuration;
            }
        }
        return maxDuration;
    }
}
