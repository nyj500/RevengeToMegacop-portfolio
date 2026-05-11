using System;
using System.Collections;

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float executionDashSpeed = 80f;
    [SerializeField] private float executionOvershootDistance = 5f;
    [SerializeField] private float groundY = 1f;
    [SerializeField] private float executionArrivalThreshold = 0.1f;

    private CharacterController controller;

    private bool isMoving;
    private bool isExecutionDashing;
    private Vector3 localMoveDirection;

    public bool IsExecutionDashing => isExecutionDashing;
    public bool IsMoving => isMoving;
    public float NormalizedSpeed => isMoving ? 1f : 0f;
    public float LocalMoveX => localMoveDirection.x;
    public float LocalMoveZ => localMoveDirection.z;

    private InputAction moveAction;

    private const float gravity = -9.81f;
    private Vector3 velocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void Initialize(InputAction moveAction)
    {
        this.moveAction = moveAction;
    }

    public void Teleport(Vector3 targetPosition)
    {
        if (controller == null) return;
        if (float.IsNaN(targetPosition.x) || float.IsNaN(targetPosition.y) || float.IsNaN(targetPosition.z))
        {
            Debug.LogWarning("PlayerMovementController: Teleport targetPosition is invalid.");
            return;
        }
        targetPosition.y = groundY;
        controller.enabled = false;
        transform.position = targetPosition;
        controller.enabled = true;
    }

    public void UpdateGravity()
    {
        if (isExecutionDashing) return;

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void HandleMovement()
    {
        HandleMove();
        HandleRotation();
    }

    private void HandleMove()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        isMoving = input.sqrMagnitude > 0f;
        Vector3 dir = (Vector3.right * input.x + Vector3.forward * input.y).normalized;
        localMoveDirection = isMoving ? transform.InverseTransformDirection(dir) : Vector3.zero;
        controller.Move(dir * (speed * Time.deltaTime));
    }

    private void HandleRotation()
    {
        transform.LookAt(MousePositionGetter.GetMousePositionInWorld(transform.position));
    }

    public void ExecutionDash(Vector3 target, Action onComplete)
        => ExecutionDash(target, executionOvershootDistance, onComplete);

    public void ExecutionDash(Vector3 target, float overshootDistance, Action onComplete)
    {
        StartCoroutine(ExecutionDashCoroutine(target, overshootDistance, onComplete));
    }

    private IEnumerator ExecutionDashCoroutine(Vector3 target, float overshootDistance, Action onComplete)
    {
        isExecutionDashing = true;
        controller.enabled = false;
        target.y = groundY;

        Vector3 toTarget = target - transform.position;
        toTarget.y = 0f;
        Vector3 dashDir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : transform.forward;
        Vector3 finalDestination = target + dashDir * overshootDistance;
        finalDestination.y = groundY;

        try
        {
            while (Vector3.Distance(transform.position, finalDestination) > executionArrivalThreshold)
            {
                transform.position = Vector3.MoveTowards(transform.position, finalDestination, executionDashSpeed * Time.unscaledDeltaTime);
                yield return null;
            }

            transform.position = finalDestination;
        }
        finally
        {
            controller.enabled = true;
            isExecutionDashing = false;
            onComplete?.Invoke();
        }
    }
}