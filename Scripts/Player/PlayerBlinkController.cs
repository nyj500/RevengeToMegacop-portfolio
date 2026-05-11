using System;

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovementController))]
public class PlayerBlinkController : PlayerSkillController, IPlayerCooldownSource
{
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private float maxDashDistance = 15f;

    public event Action OnCooldownStarted;

    private PlayerMovementController movementController;

    private float currentCooldown;

    private InputAction blinkAction;

    public override SkillId SkillId => SkillId.Blink;

    public float CooldownDuration => cooldown;
    public float CooldownRemaining => Mathf.Max(0f, currentCooldown);
    public float CooldownProgress01 => cooldown <= 0f
        ? 0f
        : Mathf.Clamp01(currentCooldown / cooldown);
    public bool IsOnCooldown => 0f < currentCooldown;

    void Awake()
    {
        movementController = GetComponent<PlayerMovementController>();
    }

    public override void InitializeSkill(InputActionMap playerMap)
    {
        blinkAction = playerMap.FindAction("Blink", throwIfNotFound: true);
    }

    public override void Tick()
    {
        if (0f < currentCooldown) currentCooldown -= Time.deltaTime;
    }

    public override void Handle()
    {
        if (!blinkAction.WasPressedThisFrame()) return;
        if (0f < currentCooldown) return;

        Vector3 mousePos = MousePositionGetter.GetMousePositionInWorld(transform.position);
        Vector3 toMouse = mousePos - transform.position;
        toMouse.y = 0f;

        Vector3 dashDir = toMouse.sqrMagnitude > 0.0001f ? toMouse.normalized : transform.forward;
        float distance = Mathf.Min(toMouse.magnitude, maxDashDistance);
        Vector3 target = transform.position + dashDir * distance;

        Time.timeScale = 0f;
        movementController.ExecutionDash(target, 0f, OnBlinkComplete);

        currentCooldown = cooldown;
        OnCooldownStarted?.Invoke();
    }

    private void OnBlinkComplete()
    {
        Time.timeScale = 1f;
    }
}
