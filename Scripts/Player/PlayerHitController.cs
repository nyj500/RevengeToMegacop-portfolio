using System;

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStateController))]
public class PlayerHitController : MonoBehaviour, IDamageable
{
    public event Action OnParry;
    public event Action OnGuard;
    public event Action OnDamaged;

    [Range(-1f, 1f)]
    [SerializeField]
    private float parryThreshold = 0.5f;

    [SerializeField]
    private float parryDuration = 0.5f;

    [SerializeField]
    private GameObject parryVfxPrefab;

    [SerializeField]
    private float parryVfxDistance = 1.5f;

    [SerializeField]
    private AudioClip parrySound;

    private PlayerStateController playerStateController;

    private ParryController parryController = new ParryController();

    public bool IsGuarding => isGuarding;

    private bool isGuarding = false;

    private InputAction parryAction;

    void Awake()
    {
        playerStateController = GetComponent<PlayerStateController>();
    }

    public void Initialize(InputAction parryAction)
    {
        this.parryAction = parryAction;
    }

    public void UpdateParries()
    {
        parryController.RemoveTooEarlyParries(parryDuration);
    }

    public void HandleHit()
    {
        if (parryAction.WasPressedThisFrame())
        {
            parryController.StackParry();
            isGuarding = true;
        }

        if (parryAction.WasReleasedThisFrame())
        {
            isGuarding = false;
        }
    }

    public void Hit(Bullet bullet)
    {
        if (bullet == null) return;
        if (CanParry(bullet))
        {
            Parry(bullet);
            return;
        }

        if (CanGuard(bullet))
        {
            Guard(bullet);
            return;
        }

        TakeDamage(bullet);
    }

    private bool CanParry(Bullet bullet)
    {
        return IsBulletInFront(bullet) && parryController.CanParry() && playerStateController.CanParry();
    }

    private bool IsBulletInFront(Bullet bullet)
    {
        Vector3 directionToBullet = bullet.transform.forward.normalized * -1f;

        directionToBullet.y = 0f;
        Vector3 playerForward = transform.forward.normalized;
        playerForward.y = 0f;

        float dot = Vector3.Dot(directionToBullet, playerForward);

        return parryThreshold < dot;
    }

    private void Parry(Bullet bullet)
    {
        parryController.Parry();
        SpawnParryVfx();
        bullet.Reflect(gameObject, true);
        playerStateController.OnSuccessfulParry();
        PlayParrySound();
        OnParry?.Invoke();
    }

    private void SpawnParryVfx()
    {
        if (parryVfxPrefab == null) return;
        Vector3 position = transform.position + transform.forward * parryVfxDistance;
        GameObject vfx = Instantiate(parryVfxPrefab, position, transform.rotation);
        Destroy(vfx, 2f);
    }

    private bool CanGuard(Bullet bullet)
    {
        return IsBulletInFront(bullet) && isGuarding && playerStateController.CanGuard();
    }

    private void Guard(Bullet bullet)
    {
        bullet.Reflect(gameObject, false);
        playerStateController.OnSuccessfulGuard();
        PlayParrySound();
        OnGuard?.Invoke();
    }

    private void PlayParrySound()
    {
        if (parrySound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(parrySound);
    }

    private void TakeDamage(Bullet bullet)
    {
        playerStateController.TakeDamage(bullet.Damage);
        OnDamaged?.Invoke();
        bullet.Remove();
    }

#if UNITY_EDITOR
    [ContextMenu("[Debug] Invoke OnParry")]
    private void DebugInvokeOnParry() => OnParry?.Invoke();

    [ContextMenu("[Debug] Invoke OnDamaged")]
    private void DebugInvokeOnDamaged() => OnDamaged?.Invoke();

    public void DebugTriggerParry() => OnParry?.Invoke();
#endif

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        float angle = Mathf.Acos(parryThreshold) * Mathf.Rad2Deg;

        Vector3 leftDirection = Quaternion.Euler(0, -angle, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDirection * 2f);

        Vector3 rightDirection = Quaternion.Euler(0, angle, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, rightDirection * 2f);
    }
}
