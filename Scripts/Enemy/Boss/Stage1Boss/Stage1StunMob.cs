using System;
using System.Collections;
using UnityEngine;

public class Stage1StunMob : Enemy
{
    [SerializeField] private Animator mobAnimator;
    [SerializeField] private GameObject weaponPrefab;
    [SerializeField] private float fireDuration = 1.5f;
    [SerializeField] private float shootCooldown = 1f;

    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private float attackSoundVolume = 1f;

    private GunWeapon gunWeapon;
    private Action pendingFireCallback;
    private Action pendingShootCycleCompleteCallback;

    void Awake()
    {
        gameObject.tag = "Enemy";
        if (weaponPrefab != null)
        {
            GameObject weaponObj = Instantiate(weaponPrefab);
            Weapon weapon = weaponObj.GetComponent<Weapon>();
            if (weapon != null)
            {
                EquipWeapon(weapon);
                gunWeapon = weapon as GunWeapon;
            }
        }
    }

    protected override void Start()
    {
        if (spawnEffectPrefab != null)
            Instantiate(spawnEffectPrefab, transform, false);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPoint(spawnSound, transform.position);
        base.Start();
        StartCoroutine(ShootRoutine());
    }

    protected override void Update()
    {
        if (Target == null) return;
        LookTarget();
    }

    private IEnumerator ShootRoutine()
    {
        while (Target != null)
        {
            bool fireReady = false;
            pendingFireCallback = () => fireReady = true;
            mobAnimator?.SetTrigger("StartFire");
            yield return new WaitUntil(() => fireReady);

            float fireElapsed = 0f;
            while (fireElapsed < fireDuration && Target != null)
            {
                if (gunWeapon != null)
                {
                    if (gunWeapon.CanFire())
                    {
                        int ammoBefore = gunWeapon.Ammo;
                        gunWeapon.TryUse();
                        if (gunWeapon.Ammo < ammoBefore && AudioManager.Instance != null)
                            AudioManager.Instance.PlaySFXAtPoint(attackSound, transform.position, attackSoundVolume);
                    }
                    else
                        gunWeapon.Reload();
                }
                fireElapsed += Time.deltaTime;
                yield return null;
            }

            bool cycleComplete = false;
            pendingShootCycleCompleteCallback = () => cycleComplete = true;
            mobAnimator?.SetTrigger("EndFire");
            yield return new WaitUntil(() => cycleComplete);

            yield return new WaitForSeconds(shootCooldown);
        }
    }

    public override void Hit(Bullet bullet)
    {
        base.Hit(bullet);
        bullet?.Remove();
    }

    // Animation Event에서 호출 — 발사 신호
    public void OnFireAnimationEvent()
    {
        pendingFireCallback?.Invoke();
        pendingFireCallback = null;
    }

    // Animation Event에서 호출 — 애니메이션 완료 신호
    public void OnAnimationCompleteEvent()
    {
        pendingShootCycleCompleteCallback?.Invoke();
        pendingShootCycleCompleteCallback = null;
    }
}
