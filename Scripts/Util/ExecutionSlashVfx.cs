using UnityEngine;

/// <summary>
/// 처형 도착 순간 칼날 궤적을 시각적으로 연출하는 슬래시 VFX.
/// ParticleSystem 기반 프리팹을 인스턴스화하고 재생 완료 후 자동 파괴한다.
///
/// [필수 구현] slashVfxPrefab에 Free Slash VFX 프리팹을 인스펙터에서 할당할 것.
/// </summary>
public class ExecutionSlashVfx : MonoBehaviour
{
    [SerializeField] private ParticleSystem slashVfxPrefab;
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 rotationOffset;
    [SerializeField] private float vfxScale = 1f;
    [SerializeField] private float simulationSpeed = 1f;

    /// <summary>
    /// 지정 위치에서 슬래시 VFX를 재생한다.
    /// </summary>
    /// <param name="position">적의 월드 위치</param>
    /// <param name="slashDirection">베는 방향 (플레이어 forward)</param>
    public void Play(Vector3 position, Vector3 slashDirection)
    {
        if (slashVfxPrefab == null) return;

        Quaternion baseRotation = Quaternion.LookRotation(slashDirection, Vector3.up);
        Quaternion finalRotation = baseRotation * Quaternion.Euler(rotationOffset);
        Vector3 spawnPosition = position + positionOffset;

        ParticleSystem instance = Instantiate(slashVfxPrefab, spawnPosition, finalRotation);
        instance.transform.localScale = Vector3.one * vfxScale;

        ParticleSystem[] childSystems = instance.GetComponentsInChildren<ParticleSystem>();

        foreach (ParticleSystem childSystem in childSystems)
        {
            ParticleSystem.MainModule main = childSystem.main;
            main.simulationSpeed = simulationSpeed;
        }

        instance.Play(true);

        Destroy(instance.gameObject, GetTotalDuration(childSystems) / simulationSpeed);
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
