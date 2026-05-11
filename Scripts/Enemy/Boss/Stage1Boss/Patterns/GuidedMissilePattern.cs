using System;
using System.Collections;

using UnityEngine;

public class GuidedMissilePattern : BossPattern
{
    [SerializeField] private GameObject missilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float missileSpeed = 10f;
    [SerializeField] private int missileCount = 3;
    [SerializeField] private float interval = 0.5f;
    [SerializeField] private AudioClip missileSound;

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(LaunchGuidedMissiles(boss, onComplete));
    }

    private IEnumerator LaunchGuidedMissiles(BossEnemy boss, Action onComplete)
    {
        if (missilePrefab == null || boss.Target == null)
        {
            yield return null;
            onComplete?.Invoke();
            yield break;
        }

        Stage1Boss stage1Boss = boss as Stage1Boss;
        bool fireReady = false;
        bool animComplete = false;
        stage1Boss?.RegisterPatternCompleteCallback(onComplete);
        stage1Boss?.RegisterFireCallback(() => fireReady = true);
        stage1Boss?.RegisterAnimationCompleteCallback(() => animComplete = true);
        stage1Boss?.NotifyPatternStart();
        stage1Boss?.BossAnimator?.SetTrigger("StartMissile");

        yield return new WaitUntil(() => fireReady);

        for (int i = 0; i < missileCount; i++)
        {
            LaunchMissile(boss);
            yield return new WaitForSeconds(interval);
        }

        stage1Boss?.BossAnimator?.SetTrigger("EndMissile");

        yield return new WaitUntil(() => animComplete);

        stage1Boss?.NotifyPatternEnd();
        onComplete?.Invoke();
    }

    private void LaunchMissile(BossEnemy boss)
    {
        Transform origin = firePoint != null ? firePoint : boss.transform;
        Bullet bullet = BulletPool.Instance.Get(missilePrefab, origin.position, boss.transform.rotation);
        Stage1BossMissile missile = bullet as Stage1BossMissile;
        if (missile == null) return;

        missile.SetOwner(boss.gameObject);
        missile.Speed = missileSpeed;
        missile.Launch(boss.Target, boss.transform);
        missile.GetComponentInChildren<BulletVFX>()?.PlayMuzzle();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPoint(missileSound, origin.position);
    }
}
