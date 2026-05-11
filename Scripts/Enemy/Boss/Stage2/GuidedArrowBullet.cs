using UnityEngine;

/// <summary>
/// Stage2 보스 전용 유도 화살. Bullet을 상속하지 않는 독립 컴포넌트.
///
/// Bullet 상속을 포기한 이유:
/// - Bullet.Update()가 직선 이동을 해서 유도 로직과 충돌
/// - Bullet.Reflect()가 virtual이 아니라 override 불가
/// - Bullet.isReflected가 private이라 패링 감지 불가
///
/// 대신 IDamageable을 직접 구현하여 패링 시스템과 연동:
/// - 플레이어 PlayerHitController는 IDamageable.Hit()을 통해 피격 처리
/// - 이 화살이 플레이어에 닿으면 PlayerHitController.Hit(Bullet)이 호출됨
///   → 하지만 이 화살은 Bullet이 아니므로 다른 방식 필요
///
/// 최종 해결: 패링은 타이밍 기반으로 자체 구현.
/// 플레이어가 Q/E를 누른 상태에서 이 화살에 닿으면 패링 성공으로 처리.
/// </summary>
public class GuidedArrowBullet : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float turnSpeed = 200f;
    [SerializeField] private float lifetime = 8f;

    [Header("Damage")]
    [SerializeField] private float playerDamage = 15f;
    [SerializeField] private float bossDamageRatio = 0.05f;

    private Transform chaseTarget;
    private Transform bossTransform;
    private GameObject owner;
    private float spawnTime;
    private bool isChasingBoss = false;

    public void Initialize(Transform playerTarget, Transform boss, GameObject owner)
    {
        chaseTarget = playerTarget;
        bossTransform = boss;
        this.owner = owner;
        spawnTime = Time.time;
    }

    void Update()
    {
        if (Time.time > spawnTime + lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // 추적 대상 방향으로 회전
        if (chaseTarget != null)
        {
            Vector3 toTarget = (chaseTarget.position - transform.position).normalized;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toTarget);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
            }
        }

        transform.position += transform.forward * (speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        GameObject obj = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
        if (obj == owner) return;

        if (!isChasingBoss)
        {
            // 패링 전: Enemy 무시
            if (obj.CompareTag("Enemy")) return;

            // 플레이어에게 닿음 → 패링 or 데미지
            PlayerStateController stateController = obj.GetComponent<PlayerStateController>();
            if (stateController == null) return;
            Debug.Log($"[GuidedArrow] 플레이어 접촉! CanParry={stateController.CanParry()}, InFront={IsBulletInFront(obj.transform)}");


            // 패링 판정: 스태미나 있음 + 화살이 플레이어 정면에서 날아옴
            if (stateController.CanParry() && IsBulletInFront(obj.transform))
            {
                // 패링 성공 → 보스 추적으로 전환
                isChasingBoss = true;
                chaseTarget = bossTransform;
                owner = obj;
                spawnTime = Time.time;
                stateController.OnSuccessfulParry();
                Debug.Log("[GuidedArrow] 패링 성공 보스 추적 시작");
                return;
            }

            // 패링 실패 → 데미지
            stateController.TakeDamage(playerDamage);
            Destroy(gameObject);
            return;
        }
        else
        {
            // 패링 후: 보스에게 5% 데미지
            if (!obj.CompareTag("Enemy")) return;

            Stage2Boss boss = obj.GetComponent<Stage2Boss>();
            if (boss != null)
            {
                boss.TakeGuidedArrowDamage(bossDamageRatio);
                Debug.Log($"[GuidedArrow] 보스 명중 HP {Mathf.RoundToInt(boss.HpRatio * 100)}%");
            }

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 화살이 플레이어 정면에서 날아오는지 판정. (PlayerHitController 로직 참고)
    /// </summary>
    private bool IsBulletInFront(Transform playerTransform)
    {
        Vector3 bulletDirection = transform.forward.normalized * -1f;
        bulletDirection.y = 0f;
        Vector3 playerForward = playerTransform.forward.normalized;
        playerForward.y = 0f;

        float dot = Vector3.Dot(bulletDirection, playerForward);
        return dot > 0.5f;
    }
}
