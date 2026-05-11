using System;
using System.Collections;

using UnityEngine;

/// <summary>
/// 분신 1~3명을 순차적으로 소환하는 패턴.
/// 분신은 BossClone 컴포넌트를 가진 프리팹이어야 한다.
/// 분신은 자체적으로 반복 사격하며, 반사탄 1발에 소멸한다.
/// 모든 분신이 소멸하거나 lifetime이 끝나면 패턴이 완료된다.
/// </summary>
public class CloneSummonPattern : BossPattern
{
    [Header("Clone Settings")]
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private int minClones = 1;
    [SerializeField] private int maxClones = 3;
    [SerializeField] private int maxConcurrentClones = 3;
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float cloneLifetime = 6f;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private float afterDelay = 3f;

    [Header("Clone Attack")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 18f;

    [Header("Sound")]
    [SerializeField] private AudioClip spawnSound;

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(SummonClones(boss, onComplete));
    }

    private IEnumerator SummonClones(BossEnemy boss, Action onComplete)
    {
        Transform target = boss.Target;
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        int cloneCount = UnityEngine.Random.Range(minClones, maxClones + 1);
        WaitForSeconds spawnWait = new WaitForSeconds(spawnInterval);

        Stage2Boss stage2Boss = boss as Stage2Boss;
        stage2Boss?.PauseMovement();

        for (int i = 0; i < cloneCount; i++)
        {
            // 현재 살아있는 분신 수가 최대치면 소환 중단
            int currentCount = FindObjectsByType<BossClone>(FindObjectsSortMode.None).Length;
            if (currentCount >= maxConcurrentClones) break;

            // 보스 주변 랜덤 위치에 소환
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = boss.transform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);
            GameObject cloneObj = Instantiate(clonePrefab, spawnPos, Quaternion.identity);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFXAtPoint(spawnSound, spawnPos);

            // BossClone 초기화
            BossClone clone = cloneObj.GetComponent<BossClone>();
            if (clone != null)
            {
                clone.Initialize(target, boss.gameObject, bulletPrefab, bulletSpeed);
            }
            // 클론 색상 변경 (보스와 구분)
            Renderer[] cloneRenderers = cloneObj.GetComponentsInChildren<Renderer>();
            for (int j = 0; j < cloneRenderers.Length; j++)
            {
                cloneRenderers[j].material.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.1f));
            }
            // 자동 소멸
            Destroy(cloneObj, cloneLifetime);

            // 분신 간 소환 간격
            if (i < cloneCount - 1)
            {
                yield return spawnWait;
            }
        }

        // 분신이 활동할 시간 확보(보스 정지 유지), 그 후 이동 재개
        yield return new WaitForSeconds(afterDelay);
        stage2Boss?.ResumeMovement();
        onComplete?.Invoke();
    }
}
