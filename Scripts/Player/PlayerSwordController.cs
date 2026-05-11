using System;

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSwordController : PlayerSkillController, IPlayerCooldownSource
{
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private float throwCooldown = 3f;

    public event Action OnSwordThrown;
    public event Action OnCooldownStarted;

    private float currentCooldown;

    private InputAction throwSwordAction;

    public override SkillId SkillId => SkillId.SwordThrow;

    public float CooldownDuration => throwCooldown;
    public float CooldownRemaining => Mathf.Max(0f, currentCooldown);
    public float CooldownProgress01 => throwCooldown <= 0f
        ? 0f
        : Mathf.Clamp01(currentCooldown / throwCooldown);
    public bool IsOnCooldown => 0f < currentCooldown;

    public override void InitializeSkill(InputActionMap playerMap)
    {
        throwSwordAction = playerMap.FindAction("ThrowSword", throwIfNotFound: true);
    }

    public override void Tick()
    {
        if (0 < currentCooldown) currentCooldown -= Time.deltaTime;
    }

    public override void Handle()
    {
        if (throwSwordAction.WasPressedThisFrame())
        {
            if (InCooldown()) return;
            if (swordPrefab == null)
            {
                Debug.LogWarning("PlayerSwordController: swordPrefab is not assigned.");
                return;
            }

            currentCooldown = throwCooldown;
            OnCooldownStarted?.Invoke();

            GameObject swordObj = Instantiate(swordPrefab, transform.position, Quaternion.identity);
            if (!swordObj.TryGetComponent<SwordController>(out var swordController))
            {
                Destroy(swordObj);
                return;
            }

            Vector3 mousePos = MousePositionGetter.GetMousePositionInWorld(swordController.transform.position);
            swordController.Throw(mousePos);
            OnSwordThrown?.Invoke();
        }
    }

    private bool InCooldown()
    {
        return 0 < currentCooldown;
    }
}
