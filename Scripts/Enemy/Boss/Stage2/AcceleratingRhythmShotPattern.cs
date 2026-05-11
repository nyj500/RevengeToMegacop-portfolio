using System;
using System.Collections;

using UnityEngine;

/// <summary>
/// 산탄 버스트를 점점 빠른 간격으로 쏘는 페이즈2 패턴.
/// 매 버스트마다 좌/정면/우 방향으로 동시 발사하고, 버스트 사이 간격이 점점 짧아진다.
/// </summary>
public class AcceleratingRhythmShotPattern : BossPattern
{
    [Header("Burst Settings")]
    [SerializeField] private int burstCount = 4;
    [SerializeField] private float firstBurstDelay = 0.8f;
    [SerializeField] private float lastBurstDelay = 0.1f;

    [Header("Spread Settings")]
    [SerializeField] private int shotsPerBurst = 3;
    [SerializeField] private float spreadAngle = 20f;

    [Header("Shot Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 22f;
    [SerializeField] private float bowReleaseDelay = 0.3f;
    [SerializeField] private float afterDelay = 1.5f;
    [SerializeField] private GameObject muzzleEffectPrefab;
    [SerializeField] private AudioClip shootSound;

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(AcceleratingBurstShot(boss, onComplete));
    }

    private IEnumerator AcceleratingBurstShot(BossEnemy boss, Action onComplete)
    {
        Transform target = boss.Target;
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (BulletPool.Instance == null)
        {
            Debug.LogError("BulletPool.Instance is null. AcceleratingRhythmShotPattern cannot fire.");
            onComplete?.Invoke();
            yield break;
        }

        Stage2Boss stage2Boss = boss as Stage2Boss;
        stage2Boss?.PauseMovement();

        Stage2BossAnimator bossAnimator = boss.GetComponent<Stage2BossAnimator>();
        Transform firePoint = stage2Boss != null ? stage2Boss.WeaponPoint : boss.transform;

        for (int burstIndex = 0; burstIndex < burstCount; burstIndex++)
        {
            if (bossAnimator != null) bossAnimator.PlayAttack();
            yield return new WaitForSeconds(bowReleaseDelay);

            // 발사 시점의 플레이어 방향으로 조준
            Vector3 baseDirection = (target.position - firePoint.position).normalized;
            baseDirection.y = 0f;
            Quaternion baseRotation = Quaternion.LookRotation(baseDirection);

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(shootSound);
            FireBurst(boss, firePoint, baseRotation);

            // 버스트 간격: 점점 짧아짐 (마지막 버스트 이후는 대기 없음)
            if (burstIndex < burstCount - 1)
            {
                float t = (float)burstIndex / (burstCount - 1);
                float burstDelay = Mathf.Lerp(firstBurstDelay, lastBurstDelay, t);
                yield return new WaitForSeconds(burstDelay);
            }
        }

        yield return new WaitForSeconds(afterDelay);
        stage2Boss?.ResumeMovement();
        onComplete?.Invoke();
    }

    private void FireBurst(BossEnemy boss, Transform firePoint, Quaternion baseRotation)
    {
        for (int shotIndex = 0; shotIndex < shotsPerBurst; shotIndex++)
        {
            // shotsPerBurst=3 기준: -spreadAngle, 0, +spreadAngle
            float angleOffset = spreadAngle * (shotIndex - (shotsPerBurst - 1) / 2f);
            Quaternion spreadRotation = Quaternion.Euler(0, angleOffset, 0) * baseRotation;

            if (muzzleEffectPrefab != null)
            {
                GameObject effect = Instantiate(muzzleEffectPrefab, firePoint.position, spreadRotation);
                Destroy(effect, 2f);
            }

            Bullet bullet = BulletPool.Instance.Get(bulletPrefab, firePoint.position, spreadRotation);
            bullet.Speed = bulletSpeed;
            bullet.SetOwner(boss.gameObject);
        }
    }
}
