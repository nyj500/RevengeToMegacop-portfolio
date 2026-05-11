using System;
using System.Collections;

using UnityEngine;

/// <summary>
/// 1초간 조준선(LineRenderer)을 표시한 후 플레이어를 향해 단발 발사하는 패턴.
/// LineRenderer는 이 스크립트와 같은 GameObject에 붙여야 한다.
/// </summary>
public class AimedShotPattern : BossPattern
{
    [Header("Aim Settings")]
    [SerializeField] private float aimDuration = 1f;
    [SerializeField] private Color aimLineColor = Color.red;
    [SerializeField] private float aimLineWidth = 0.05f;
    [SerializeField] private float aimLineLength = 50f;

    [Header("Shot Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 25f;
    [SerializeField] private float bowReleaseDelay = 0.5f;
    [SerializeField] private float afterDelay = 1.5f;
    [SerializeField] private GameObject muzzleEffectPrefab;
    [SerializeField] private AudioClip shootSound;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = aimLineWidth;
        lineRenderer.endWidth = aimLineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = aimLineColor;
        lineRenderer.endColor = aimLineColor;
        lineRenderer.enabled = false;
    }

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(AimAndShoot(boss, onComplete));
    }


    private IEnumerator AimAndShoot(BossEnemy boss, Action onComplete)
    {
        Transform target = boss.Target;
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        // 조준 중 이동 정지
        Stage2Boss stage2Boss = boss as Stage2Boss;
        stage2Boss?.PauseMovement();

        Transform firePoint = (boss as Stage2Boss)?.WeaponPoint ?? boss.transform;
        Vector3 aimDirection = Vector3.forward;

        // 조준선 표시 — 매 프레임 방향 재계산 (순간이동 후에도 플레이어 방향 추적)
        lineRenderer.enabled = true;
        float elapsed = 0f;

        while (elapsed < aimDuration)
        {
            Vector3 startPos = firePoint.position;
            Vector3 toTarget = target.position - startPos;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                aimDirection = toTarget.normalized;

            Vector3 endPos = startPos + aimDirection * aimLineLength;
            lineRenderer.SetPosition(0, startPos);
            lineRenderer.SetPosition(1, endPos);

            elapsed += Time.deltaTime;
            yield return null;
        }

        lineRenderer.enabled = false;

        boss.GetComponent<Stage2BossAnimator>()?.PlayAttack();
        yield return new WaitForSeconds(bowReleaseDelay);

        // 발사
        if (BulletPool.Instance == null)
        {
            Debug.LogError("BulletPool.Instance is null. AimedShotPattern cannot fire.");
            stage2Boss?.ResumeMovement();
            onComplete?.Invoke();
            yield break;
        }

        Vector3 firePos = firePoint.position;
        Quaternion fireRotation = Quaternion.LookRotation(aimDirection);

        if (muzzleEffectPrefab != null)
        {
            GameObject effect = Instantiate(muzzleEffectPrefab, firePos, fireRotation);
            Destroy(effect, 2f);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(shootSound);

        Bullet bullet = BulletPool.Instance.Get(bulletPrefab, firePos, fireRotation);
        bullet.Speed = bulletSpeed;
        bullet.SetOwner(boss.gameObject);

        // 공격 후 여유 시간(후딜) 동안 정지 유지, 그 후 이동 재개
        yield return new WaitForSeconds(afterDelay);
        stage2Boss?.ResumeMovement();
        onComplete?.Invoke();
    }
}
