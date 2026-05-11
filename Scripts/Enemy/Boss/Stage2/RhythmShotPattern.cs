using System;
using System.Collections;

using UnityEngine;

/// <summary>
/// 지정한 발 수만큼 쉬지 않고 몰아치는 집중 사격 패턴.
/// 매 발마다 공격 애니메이션을 재생하고 bowReleaseDelay 후 발사한다.
/// </summary>
public class RhythmShotPattern : BossPattern
{
    [Header("Barrage Settings")]
    [SerializeField] private int shotCount = 15;
    [SerializeField] private float shotDelay = 0f;

    [Header("Shot Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 20f;
    [SerializeField] private float bowReleaseDelay = 0.3f;
    [SerializeField] private float afterDelay = 1.5f;
    [SerializeField] private GameObject muzzleEffectPrefab;
    [SerializeField] private AudioClip shootSound;

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(BarrageShot(boss, onComplete));
    }

    private IEnumerator BarrageShot(BossEnemy boss, Action onComplete)
    {
        Transform target = boss.Target;
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (BulletPool.Instance == null)
        {
            Debug.LogError("BulletPool.Instance is null. RhythmShotPattern cannot fire.");
            onComplete?.Invoke();
            yield break;
        }

        Stage2Boss stage2Boss = boss as Stage2Boss;
        stage2Boss?.PauseMovement();

        Stage2BossAnimator bossAnimator = boss.GetComponent<Stage2BossAnimator>();

        for (int i = 0; i < shotCount; i++)
        {
            if (bossAnimator != null) bossAnimator.PlayAttack();
            yield return new WaitForSeconds(bowReleaseDelay);

            FireAtTarget(boss, target);

            if (i < shotCount - 1)
                yield return new WaitForSeconds(shotDelay);
        }

        yield return new WaitForSeconds(afterDelay);
        stage2Boss?.ResumeMovement();
        onComplete?.Invoke();
    }

    private void FireAtTarget(BossEnemy boss, Transform target)
    {
        Transform firePoint = (boss as Stage2Boss)?.WeaponPoint ?? boss.transform;
        Vector3 direction = (target.position - firePoint.position).normalized;
        direction.y = 0f;

        Quaternion rotation = Quaternion.LookRotation(direction);

        if (muzzleEffectPrefab != null)
        {
            GameObject effect = Instantiate(muzzleEffectPrefab, firePoint.position, rotation);
            Destroy(effect, 2f);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(shootSound);

        Bullet bullet = BulletPool.Instance.Get(bulletPrefab, firePoint.position, rotation);
        bullet.Speed = bulletSpeed;
        bullet.SetOwner(boss.gameObject);
    }
}
