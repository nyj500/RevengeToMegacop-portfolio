using System.Collections;

using UnityEngine;

/// <summary>
/// 보스전 동안 백그라운드로 계속 작동하는 화살비 환경 위협.
/// 패턴 시스템(BossPattern)과 독립적으로 동작한다.
///
/// [구역 방식]
/// 경고 구역 1개가 플레이어를 trackDuration초 동안 실시간 추적한다.
/// 시간이 끝나면 그 자리에 화살이 낙하하고, 즉시 다음 구역이 추적을 시작한다.
/// 이를 burstCount회 반복한다.
/// 플레이어는 계속 움직여도 구역이 따라오고, 멈추면 바로 맞으므로 움직임이 강제된다.
/// </summary>
public class ArrowRainPattern : MonoBehaviour
{
    [Header("Warning Settings")]
    [SerializeField] private GameObject warningPrefab;
    [SerializeField] private float warningRadius = 2.5f;
    [SerializeField] private float warningDuration = 0.4f;   // 경고 표시 후 화살 낙하까지 대기 시간

    [Header("Burst Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int burstCount = 5;
    [SerializeField] private int bulletsPerBurst = 8;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private float spawnHeight = 15f;
    [SerializeField] private float rainDamage = 10f;

    [Header("Repeat Settings")]
    [SerializeField] private float repeatInterval = 5f;

    [Header("Sound")]
    [SerializeField] private AudioClip warningSound;

    private Transform target;
    private GameObject owner;
    private Coroutine rainLoop;

    /// <summary>화살비 반복 시작. 보스전 시작 시 호출한다.</summary>
    public void StartRain(Transform target, GameObject owner)
    {
        this.target = target;
        this.owner = owner;

        if (rainLoop != null) StopCoroutine(rainLoop);
        rainLoop = StartCoroutine(RainLoop());
    }

    /// <summary>화살비 중단. 보스 사망 시 호출한다.</summary>
    public void StopRain()
    {
        StopAllCoroutines();
        rainLoop = null;
    }

    private IEnumerator RainLoop()
    {
        WaitForSeconds repeatWait = new WaitForSeconds(repeatInterval);
        while (true)
        {
            yield return StartCoroutine(BurstSequence());
            yield return repeatWait;
        }
    }

    /// <summary>
    /// burstCount회 순차 낙하.
    /// 각 회차: 현재 플레이어 위치에 경고 고정 → warningDuration 대기 → 화살 낙하 → 즉시 다음 회차.
    /// </summary>
    private IEnumerator BurstSequence()
    {
        WaitForSeconds warningWait = new WaitForSeconds(warningDuration);

        for (int i = 0; i < burstCount; i++)
        {
            if (target == null || BulletPool.Instance == null) yield break;

            // 이 순간 플레이어 위치 스냅샷 → 경고 고정
            Vector3 lockedPos = new Vector3(target.position.x, 0f, target.position.z);

            GameObject warning = null;
            if (warningPrefab != null)
            {
                Vector3 warningPos = new Vector3(lockedPos.x, 0.01f, lockedPos.z);
                warning = Instantiate(warningPrefab, warningPos, Quaternion.identity);
                warning.transform.localScale = Vector3.one * warningRadius;
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySFXAtPoint(warningSound, warningPos);
            }

            // 경고 표시 중 플레이어는 이동 가능 (경고는 고정)
            yield return warningWait;

            if (warning != null) Destroy(warning);

            // 화살 낙하
            yield return StartCoroutine(DropArrows(lockedPos));

            // 데미지 판정
            if (target != null)
            {
                Vector3 playerFlat = new Vector3(target.position.x, 0f, target.position.z);
                if (Vector3.Distance(playerFlat, lockedPos) <= warningRadius)
                    target.GetComponent<PlayerStateController>()?.TakeDamage(rainDamage);
            }
        }
    }

    private IEnumerator DropArrows(Vector3 center)
    {
        if (BulletPool.Instance == null) yield break;

        float interval = 0.05f;
        for (int i = 0; i < bulletsPerBurst; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * warningRadius;
            Vector3 spawnPos = new Vector3(center.x + offset.x, spawnHeight, center.z + offset.y);

            Bullet bullet = BulletPool.Instance.Get(bulletPrefab, spawnPos, Quaternion.LookRotation(Vector3.down));
            if (bullet != null)
            {
                bullet.Speed = bulletSpeed;
                bullet.SetOwner(owner);
            }

            yield return new WaitForSeconds(interval);
        }
    }
}
