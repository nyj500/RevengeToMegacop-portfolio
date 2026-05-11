using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerAnimationController))]
[RequireComponent(typeof(PlayerExecutionController))]
[RequireComponent(typeof(PlayerHitController))]
[RequireComponent(typeof(PlayerMovementController))]
[RequireComponent(typeof(PlayerStateController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;

    private PlayerAnimationController playerAnimationController;
    private PlayerExecutionController playerExecutionController;
    private PlayerHitController playerHitController;
    private PlayerMovementController playerMovementController;
    private PlayerStateController playerStateController;

    private PlayerSkillController[] skillControllers;

    private bool isInitialized = false;

    void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError("PlayerController: inputActions is not assigned.");
            return;
        }

        playerAnimationController = GetComponent<PlayerAnimationController>();
        playerExecutionController = GetComponent<PlayerExecutionController>();
        playerHitController = GetComponent<PlayerHitController>();
        playerMovementController = GetComponent<PlayerMovementController>();
        playerStateController = GetComponent<PlayerStateController>();

        var playerMap = inputActions.FindActionMap("Player", throwIfNotFound: true);
        playerMovementController.Initialize(
            playerMap.FindAction("Move", throwIfNotFound: true));
        playerHitController.Initialize(
            playerMap.FindAction("Parry", throwIfNotFound: true));
        playerExecutionController.Initialize(
            playerMap.FindAction("Attack", throwIfNotFound: true));

        skillControllers = GetComponents<PlayerSkillController>();
        foreach (var skillController in skillControllers)
            skillController.InitializeSkill(playerMap);

        var playerSwordController = GetComponent<PlayerSwordController>();
        playerAnimationController.Initialize(
            playerMovementController,
            playerHitController,
            playerStateController,
            playerSwordController);

        isInitialized = true;
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        if (!isInitialized) return;

        playerAnimationController.UpdateAnimation();
        playerHitController.UpdateParries();
        playerMovementController.UpdateGravity();
        playerStateController.UpdateStamina();

        foreach (var skillController in skillControllers)
            skillController.Tick();

        if (playerMovementController.IsExecutionDashing) return;

        playerHitController.HandleHit();
        playerMovementController.HandleMovement();

        foreach (var skillController in skillControllers)
        {
            if (SkillManager.Instance != null && SkillManager.Instance.IsUnlocked(skillController.SkillId))
                skillController.Handle();
        }

        playerExecutionController.HandleExecution();
    }
}
