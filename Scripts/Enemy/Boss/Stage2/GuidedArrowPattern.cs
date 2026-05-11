using System;
using System.Collections;

using UnityEngine;

/// <summary>
/// 유도 화살을 발사하는 패턴.
/// GuidedArrowBullet 프리팹을 생성하고 Initialize()로 추적 대상을 설정한다.
/// 화살은 독립적으로 플레이어를 추적하며, 패링 시 보스를 추적한다.
/// </summary>
public class GuidedArrowPattern : BossPattern
{
    [Header("Guided Arrow Settings")]
    [SerializeField] private GameObject guidedArrowPrefab;
    [SerializeField] private float bowReleaseDelay = 0.5f;
    [SerializeField] private float afterDelay = 1.5f;
    [SerializeField] private GameObject muzzleEffectPrefab;

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(FireGuidedArrow(boss, onComplete));
    }

    private IEnumerator FireGuidedArrow(BossEnemy boss, Action onComplete)
    {
        Transform target = boss.Target;
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (guidedArrowPrefab == null)
        {
            Debug.LogError("[GuidedArrowPattern] guidedArrowPrefab이 할당되지 않았습니다!");
            onComplete?.Invoke();
            yield break;
        }

        Stage2Boss stage2Boss = boss as Stage2Boss;
        stage2Boss?.PauseMovement();

        boss.GetComponent<Stage2BossAnimator>()?.PlayAttack();
        yield return new WaitForSeconds(bowReleaseDelay);

        // 발사 방향
        Vector3 direction = (target.position - boss.transform.position).normalized;
        direction.y = 0f;
        Quaternion rotation = Quaternion.LookRotation(direction);

        if (muzzleEffectPrefab != null)
        {
            GameObject effect = Instantiate(muzzleEffectPrefab, boss.transform.position, rotation);
            Destroy(effect, 2f);
        }

        // 유도 화살 생성
        GameObject arrowObj = Instantiate(guidedArrowPrefab, boss.transform.position, rotation);
        GuidedArrowBullet guidedBullet = arrowObj.GetComponent<GuidedArrowBullet>();

        if (guidedBullet == null)
        {
            Debug.LogError("[GuidedArrowPattern] 프리팹에 GuidedArrowBullet 컴포넌트가 없습니다!");
            Destroy(arrowObj);
            stage2Boss?.ResumeMovement();
            onComplete?.Invoke();
            yield break;
        }

        guidedBullet.Initialize(target, boss.transform, boss.gameObject);
        Debug.Log("[GuidedArrowPattern] 유도 화살 발사!");

        // 공격 후 여유 시간(후딜) 동안 정지 유지, 그 후 이동 재개
        yield return new WaitForSeconds(afterDelay);
        stage2Boss?.ResumeMovement();
        onComplete?.Invoke();
    }
}
