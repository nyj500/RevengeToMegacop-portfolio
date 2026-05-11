using System;

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovementController))]
[RequireComponent(typeof(PlayerStateController))]
public class PlayerExecutionController : MonoBehaviour
{
    public event Action<ExecutionResult> OnExecutionComplete;

    private PlayerMovementController playerMovementController;
    private PlayerStateController playerStateController;

    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float executionRange = 50f;
    [SerializeField] private ExecutionSliceEffect executionSliceEffect;
    [SerializeField] private ExecutionSlashVfx executionSlashVfx;
    [SerializeField, Range(0f, 1f)] private float executionHealRatio = 0.5f;

    private InputAction attackAction;
    private Camera mainCamera;

    private Enemy executionTarget;

    void Awake()
    {
        playerMovementController = GetComponent<PlayerMovementController>();
        playerStateController = GetComponent<PlayerStateController>();
        mainCamera = Camera.main;
    }

    public void Initialize(InputAction attackAction)
    {
        this.attackAction = attackAction;
    }

    public void HandleExecution()
    {
        if (attackAction.WasPressedThisFrame() && playerStateController.CanExecute())
        {
            TryExecute();
        }
    }

    private void TryExecute()
    {
        if (Mouse.current == null) return;
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, executionRange, enemyLayerMask))
        {
            Execute(hit.collider.attachedRigidbody != null ? hit.collider.attachedRigidbody.gameObject : hit.collider.gameObject);
        }
    }

    private void Execute(GameObject enemy)
    {
        if (enemy == null) return;
        Vector3 enemyPosition = enemy.transform.position;

        executionTarget = enemy.TryGetComponent<Enemy>(out var enemyComponent) ? enemyComponent : null;

        Time.timeScale = 0f;
        playerMovementController.ExecutionDash(enemyPosition, OnExecutionDashComplete);
        playerStateController.Executed();
    }

    private void OnExecutionDashComplete()
    {
        Time.timeScale = 1f;
        if (executionTarget != null)
        {
            ExecutionContext context = new ExecutionContext
            {
                SlicePosition = executionTarget.transform.position,
                SliceNormal = transform.right,
                SlashDirection = transform.forward,
                SliceEffect = executionSliceEffect,
                SlashVfx = executionSlashVfx
            };

            ExecutionResult result = executionTarget.HandleExecution(context);
            executionTarget = null;
            OnExecutionComplete?.Invoke(result);
            playerStateController.Heal(playerStateController.MaxHp * executionHealRatio);
        }
    }
}
