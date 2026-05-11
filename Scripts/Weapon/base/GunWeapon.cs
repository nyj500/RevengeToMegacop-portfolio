using System.Collections;

using UnityEngine;

/// <summary>
/// 총기 계열 무기 기반 클래스. 탄약 관리와 리로드 코루틴을 담당한다.
/// BulletPool을 통해 bulletPrefab을 발사하며, 탄약 소진 시 Reload()를 호출해야 한다.
/// [선택 override] Awake() — base.Awake() 반드시 호출
/// </summary>
public abstract class GunWeapon : Weapon
{
    [field: SerializeField] public int MaxAmmo { get; private set; }

    public int Ammo { get; private set; }

    [field: SerializeField] public float BulletSpeed { get; private set; }

    [SerializeField] protected GameObject bulletPrefab;

    [SerializeField] private Transform firePoint;

    [SerializeField] private Vector3 muzzleLocalOffset = new Vector3(0f, 1.2f, 0f);

    [SerializeField] private float reloadTime = 2f;

    [SerializeField] private AudioClip fireSound;

    private Transform shooter;

    private bool isReloading = false;
    private Coroutine reloadCoroutine;
    private WaitForSeconds waitForReload;

    /// <summary>
    /// 현재 탄약이 남아 있으면 true를 반환한다.
    /// </summary>
    public bool CanFire()
    {
        return Ammo > 0;
    }

    /// <summary>
    /// 리로드를 시작한다. 이미 리로드 중이면 무시한다.
    /// </summary>
    public void Reload()
    {
        if (isReloading) return;
        isReloading = true;
        reloadCoroutine = StartCoroutine(ReloadCoroutine());
    }

    void OnDestroy()
    {
        if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
    }

    private IEnumerator ReloadCoroutine()
    {
        yield return waitForReload;
        Ammo = MaxAmmo;
        isReloading = false;
    }

    /// <summary>
    /// 총알 발사 기준 Transform을 설정한다. Enemy.EquipWeapon에서 호출된다.
    /// </summary>
    public void SetShooter(Transform shooterTransform)
    {
        shooter = shooterTransform;
    }

    protected override void Awake()
    {
        base.Awake();
        waitForReload = new WaitForSeconds(reloadTime);
        Ammo = MaxAmmo;
        if (bulletPrefab == null)
        {
            Debug.LogWarning("GunWeapon: bulletPrefab is not assigned.");
        }
    }

    protected override void Use()
    {
        if (CanFire()) Fire();
    }

    private void Fire()
    {
        if (bulletPrefab == null || shooter == null)
        {
            Debug.LogWarning("GunWeapon: Cannot fire due to missing bulletPrefab or shooter.");
            return;
        }
        Ammo--;
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        if (firePoint != null)
        {
            spawnPosition = firePoint.position;
            Vector3 flatForward = firePoint.forward;
            flatForward.y = 0f;
            spawnRotation = flatForward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(flatForward.normalized)
                : firePoint.rotation;
        }
        else
        {
            spawnPosition = shooter.TransformPoint(muzzleLocalOffset);
            spawnRotation = shooter.rotation;
        }
        Bullet bullet = BulletPool.Instance.Get(bulletPrefab, spawnPosition, spawnRotation);
        if (bullet == null) return;
        bullet.SetOwner(gameObject);
        bullet.Speed = BulletSpeed;
        if (fireSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(fireSound);
    }
}
