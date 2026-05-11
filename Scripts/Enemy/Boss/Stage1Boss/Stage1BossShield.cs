using System;
using FXV;
using UnityEngine;

public class Stage1BossShield : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxShieldGauge = 100f;
    [SerializeField] private Shield shield;
    [SerializeField] private float maxDamagePerHitRatio = 0.3f;
    [SerializeField] private float maxExecutionDamageRatio = 0.15f;
    [SerializeField] private float destructionThreshold = 0.3f;
    [SerializeField] private float reflectSpeedMultiplier = 0.7f;
    [SerializeField] private float minReflectSpeed = 5f;
    [SerializeField] private AudioClip reflectSound;
    [SerializeField] private Transform followTarget; // 보스 루트 할당

    private float shieldGauge;
    private Collider shieldCollider;

    public float ShieldRatio => maxShieldGauge > 0f ? shieldGauge / maxShieldGauge : 0f;
    public bool IsActive => shieldGauge >= 0.01f;

    public event Action<float> OnShieldChanged; // 실드 게이지가 변할 때 비율(0~1)을 전달하는 이벤트

    private Transform _target;

    void Awake()
    {
        shieldCollider = GetComponent<Collider>();
    }

    void LateUpdate()
    {
        transform.position = followTarget.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Bullet>(out var bullet))
        {
            Vector3 hitPos = other.ClosestPoint(transform.position);
            Vector3 hitNormal = (hitPos - transform.position).normalized;
            shield.OnHit(hitPos, hitNormal, 1.5f, 0.8f);
        }
    }

    public void Initialize(Transform target)
    {
        this._target = target;
        shieldGauge = maxShieldGauge;
    }

    public void Hit(Bullet bullet)
    {
        if (bullet == null) return;
        if (shieldGauge < 0.01f) return;

        float maxDamage = maxShieldGauge * maxDamagePerHitRatio;
        float minFromDamageRatio = Mathf.Max(shieldGauge - maxDamage, 0f);
        float minFromThreshold = GetMinAllowedGauge();

        shieldGauge = Mathf.Max(shieldGauge - bullet.Damage, 0f);
        shieldGauge = Mathf.Max(shieldGauge, minFromDamageRatio, minFromThreshold);

        OnShieldChanged?.Invoke(ShieldRatio);
        Debug.Log($"Shield hit! Remaining Shield: {shieldGauge}/{maxShieldGauge}");
        ReflectToPlayer(bullet);

        TryDeactivate();
    }

    public void TakeExecutionDamage()
    {
        if (shieldGauge < 0.01f) return;

        float damage = maxShieldGauge * maxExecutionDamageRatio;
        shieldGauge = Mathf.Max(shieldGauge - damage, GetMinAllowedGauge());

        OnShieldChanged?.Invoke(ShieldRatio);
        Debug.Log($"Shield execution hit! Remaining Shield: {shieldGauge}/{maxShieldGauge}");

        TryDeactivate();
    }

    public void Regenerate()
    {
        shieldGauge = maxShieldGauge;
        shield.SetShieldActive(true, true);
        if (shieldCollider != null) shieldCollider.enabled = true;
        OnShieldChanged?.Invoke(ShieldRatio);
    }

    private float GetMinAllowedGauge()
    {
        return ShieldRatio > destructionThreshold ? maxShieldGauge * destructionThreshold : 0f;
    }

    private void TryDeactivate()
    {
        if (shieldGauge < 0.01f)
        {
            shieldGauge = 0f;
            shield.SetShieldActive(false, true);
            if (shieldCollider != null) shieldCollider.enabled = false;
        }
    }

    private void ReflectToPlayer(Bullet bullet)
    {
        if (_target == null) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPoint(reflectSound, transform.position);

        bullet.Reflect(gameObject, true);
        bullet.Speed = Mathf.Max(bullet.Speed * reflectSpeedMultiplier, minReflectSpeed);

        Vector3 direction = _target.position - bullet.transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
            bullet.transform.forward = direction.normalized;
    }
}
