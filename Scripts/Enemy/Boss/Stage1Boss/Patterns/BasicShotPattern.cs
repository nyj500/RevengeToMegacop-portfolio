using System;
using System.Collections;
using UnityEngine;

public class BasicShotPattern : BossPattern
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 12f;
    [SerializeField] private float shotInterval = 0.3f;
    [SerializeField] private float minShotDuration = 1f;
    [SerializeField] private float maxShotDuration = 3f;
    [SerializeField] private AudioClip fireSound;

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(FireSequence(boss, onComplete));
    }

    private IEnumerator FireSequence(BossEnemy boss, Action onComplete)
    {
        Stage1Boss stage1Boss = boss as Stage1Boss;

        bool fireReady = false;
        bool animComplete = false;
        stage1Boss?.RegisterPatternCompleteCallback(onComplete);
        stage1Boss?.RegisterFireCallback(() => fireReady = true);
        stage1Boss?.RegisterAnimationCompleteCallback(() => animComplete = true);

        stage1Boss?.NotifyPatternStart();
        stage1Boss?.BossAnimator?.SetTrigger("StartFire");

        yield return new WaitUntil(() => fireReady);

        stage1Boss?.StartKiting();

        float shotDuration = UnityEngine.Random.Range(minShotDuration, maxShotDuration);
        float elapsed = 0f;
        while (elapsed < shotDuration)
        {
            Fire(boss);
            yield return new WaitForSeconds(shotInterval);
            elapsed += shotInterval;
        }

        stage1Boss?.StopKiting();
        stage1Boss?.BossAnimator?.SetTrigger("EndFire");

        yield return new WaitUntil(() => animComplete);

        stage1Boss?.NotifyPatternEnd();
        onComplete?.Invoke();
    }

    private void Fire(BossEnemy boss)
    {
        if (bulletPrefab == null || boss.Target == null) return;

        Transform origin = firePoint != null ? firePoint : boss.transform;
        Vector3 direction = boss.Target.position - origin.position;
        direction.y = 0f;

        if (direction == Vector3.zero) return;

        Quaternion rotation = Quaternion.LookRotation(direction.normalized);
        Bullet bullet = BulletPool.Instance.Get(bulletPrefab, origin.position, rotation);
        if (bullet == null) return;

        bullet.SetOwner(boss.gameObject);
        bullet.Speed = bulletSpeed;
        bullet.GetComponentInChildren<BulletVFX>()?.PlayMuzzle();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPoint(fireSound, origin.position);
    }
}
