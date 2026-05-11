using UnityEngine;

/// <summary>
/// 보스가 플레이어 주변을 원형으로 돌며 거리를 유지하는 이동 컴포넌트.
/// 보스전 시작 시 StartStrafe, 보스 사망 시 StopStrafe를 호출한다.
/// 주기적으로 회전 방향(시계/반시계)을 반전한다.
/// </summary>
public class BossStrafeMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float preferredDistance = 15f;

    [Header("Direction Change")]
    [SerializeField] private float directionChangeInterval = 3f;

    [Header("Animation")]
    [SerializeField] private Stage2BossAnimator bossAnimator;

    private Transform target;
    private bool isActive = false;
    private int direction = 1;
    private float lastDirectionChangeTime;

    public void StartStrafe(Transform target)
    {
        this.target = target;
        isActive = true;
        direction = Random.value < 0.5f ? 1 : -1;
        lastDirectionChangeTime = Time.time;
        if (bossAnimator != null)
        {
            bossAnimator.SetMoving(true);
            bossAnimator.SetMoveDirection(direction);
        }
    }

    public void StopStrafe()
    {
        isActive = false;
        if (bossAnimator != null) bossAnimator.SetMoving(false);
    }

    void Update()
    {
        if (target == null) return;

        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;
        if (distance < 0.01f) return;

        Vector3 toPlayerDirection = toPlayer / distance;

        // 공격 중이든 이동 중이든 항상 플레이어를 바라봄
        transform.forward = toPlayerDirection;

        if (!isActive) return;

        if (Time.time > lastDirectionChangeTime + directionChangeInterval)
        {
            direction *= -1;
            lastDirectionChangeTime = Time.time;
            if (bossAnimator != null) bossAnimator.SetMoveDirection(direction);
        }

        // 접선 방향 (원형 이동)
        Vector3 tangent = Vector3.Cross(Vector3.up, toPlayerDirection) * direction;

        // 거리 유지: 멀면 접근, 가까우면 후퇴
        float distanceError = (distance - preferredDistance) / preferredDistance;
        Vector3 radial = toPlayerDirection * Mathf.Clamp(distanceError, -1f, 1f);

        Vector3 moveDirection = (tangent + radial).normalized;

        transform.position += moveDirection * (moveSpeed * Time.deltaTime);
    }
    public void Pause()
    {
        isActive = false;
        if (bossAnimator != null) bossAnimator.SetMoving(false);
    }

    public void Resume()
    {
        isActive = true;
        if (bossAnimator != null) bossAnimator.SetMoving(true);
    }
}