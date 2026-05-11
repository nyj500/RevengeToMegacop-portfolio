using System;
using System.Collections;

using UnityEngine;

public class CircularBulletPattern : BossPattern
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int bulletCount = 12;
    [SerializeField] private int waves = 3;
    [SerializeField] private float waveCooldown = 0.5f;
    [SerializeField] private float bulletSpeed = 10f;

    private WaitForSeconds waitForWave;

    void Awake()
    {
        waitForWave = new WaitForSeconds(waveCooldown);
    }

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(FireCircularBullets(boss, onComplete));
    }

    private IEnumerator FireCircularBullets(BossEnemy boss, Action onComplete)
    {
        if (BulletPool.Instance == null)
        {
            Debug.LogError("BulletPool.Instance is null. CircularBulletPattern cannot fire.");
            onComplete?.Invoke();
            yield break;
        }

        float angleStep = 360f / bulletCount;

        for (int w = 0; w < waves; w++)
        {
            if (boss == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            for (int i = 0; i < bulletCount; i++)
            {
                float angle = i * angleStep;
                Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                Vector3 position = boss.transform.position;

                Bullet bullet = BulletPool.Instance.Get(bulletPrefab, position, rotation);
                if (bullet == null) continue;
                bullet.Speed = bulletSpeed;
                bullet.SetOwner(boss.gameObject);
            }

            if (w < waves - 1)
            {
                yield return waitForWave;
            }
        }

        onComplete?.Invoke();
    }
}
