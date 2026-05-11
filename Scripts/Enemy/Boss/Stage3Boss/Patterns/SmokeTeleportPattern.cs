using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Boss3;

/// <summary>
/// SmokeTeleportPattern (연막 이동 패턴)
/// 
/// 플레이어 근처의 유효한 위치 3곳에 연막탄을 던집니다.
/// 그 중 한 곳으로 보스가 빠르게 이동하여 기습적인 위치 선정을 합니다.
/// MovePattern의 이동 로직과 SmokeBomb의 투척 로직을 결합한 연습용 패턴입니다.
/// 
/// [사용법]
/// 1. 유니티 에디터에서 보스(BossEnemy) 오브젝트의 자식으로 빈 게임 오브젝트를 만듭니다.
/// 2. 생성한 오브젝트에 이 'SmokeTeleportPattern' 스크립트를 추가합니다.
/// 3. Inspector 창에서 다음 항목들을 연결합니다:
///    - Smoke Bomb Prefab: 'Boss3SmokeBomb' 프리팹을 연결합니다. 
///      (경로: Assets/_Project/Prefabs/Stage3BossPatternPrefab/SmokeBomb.prefab)
///    - Throw Point: 연막탄이 발사될 보스의 손이나 무기 위치 Transform을 연결합니다.
///    - Ground Layer: 바닥 체크를 위한 레이어를 선택합니다. (보통 'Ground')
///    - Obstacle Layer: 장애물 체크를 위한 레이어를 선택합니다. (보통 'Obstacle')
/// 4. BossEnemy 컴포넌트의 'Patterns' 리스트에 이 오브젝트를 등록합니다.
/// </summary>
public class SmokeTeleportPattern : BossPattern
{
    [Header("연막탄 설정")]
    [SerializeField] private Boss3SmokeBomb _smokeBombPrefab;
    [SerializeField] private Transform _throwPoint;
    [SerializeField] private int _smokeCount = 3;
    [SerializeField] private float _throwRadius = 6f;

    [Header("이동 설정")]
    [SerializeField] private float _moveSpeed = 25f;
    [SerializeField] private float _arriveDistance = 0.8f;
    
    [Header("위치 검증 설정 (MovePattern 참고)")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private float _checkRadius = 0.5f;
    [SerializeField] private int _maxTryCount = 30;

    private Transform _playerTransform;
    private Animator _anim;

    private void Awake()
    {
        // 발사 지점이 설정되지 않았다면 자신을 사용
        if (_throwPoint == null)
            _throwPoint = transform;

        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _playerTransform = player.transform;

        // 부모로부터 애니메이터 가져오기
        _anim = GetComponentInParent<Animator>();
    }

    /// <summary>
    /// 패턴 실행의 시작점입니다. BossEnemy에 의해 호출됩니다.
    /// </summary>
    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        if (_smokeBombPrefab == null || _playerTransform == null)
        {
            Debug.LogWarning("SmokeTeleportPattern: 프리팹이나 플레이어 참조가 누락되었습니다.");
            onComplete?.Invoke();
            return;
        }

        Debug.Log("SmokeTeleportPattern 실행 중...");
        StartCoroutine(PerformSmokeAndMove(boss, onComplete));
    }

    private IEnumerator PerformSmokeAndMove(BossEnemy boss, Action onComplete)
    {
        List<Vector3> targetPositions = new List<Vector3>();

        // 1. 플레이어 주변에 연막탄을 던질 유효한 위치 3곳 확보
        for (int i = 0; i < _smokeCount; i++)
        {
            Vector3 pos;
            if (TryGetValidPositionNearPlayer(out pos))
            {
                targetPositions.Add(pos);
            }
        }

        if (targetPositions.Count == 0)
        {
            Debug.LogWarning("SmokeTeleportPattern: 유효한 위치를 찾지 못해 패턴을 종료합니다.");
            onComplete?.Invoke();
            yield break;
        }

        // 2. 연막탄 투척 애니메이션 실행
        if (_anim != null)
            _anim.SetTrigger("Granade");

        // 애니메이션 프레임에 맞춰 투척 시점 지연
        yield return new WaitForSeconds(0.3f);

        // 3. 연막탄 투척 및 착지 확인
        int arrivedCount = 0;
        foreach (var pos in targetPositions)
        {
            Boss3SmokeBomb bomb = Instantiate(_smokeBombPrefab, _throwPoint.position, Quaternion.identity);
            // bomb.Throw는 내부적으로 포물선 이동 후 onExplode 콜백을 호출함
            bomb.Throw(_throwPoint.position, pos, () => arrivedCount++);
        }

        // 모든 연막탄이 도착할 때까지 최대 2초 대기
        float timeout = 2.0f;
        while (arrivedCount < targetPositions.Count && timeout > 0)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // 4. 목표 지점 결정 (랜덤하게 한 곳 선택)
        Vector3 finalDestination = targetPositions[UnityEngine.Random.Range(0, targetPositions.Count)];

        // 이동 애니메이션 시작
        if (_anim != null)
            _anim.SetBool("Run", true);

        // 5. 목표 지점으로 빠르게 이동 (MovePattern 로직 활용)
        while (Vector3.Distance(boss.transform.position, finalDestination) > _arriveDistance)
        {
            Vector3 direction = (finalDestination - boss.transform.position).normalized;
            direction.y = 0; // 평면 이동
            
            if (direction != Vector3.zero)
            {
                // 부드럽게 방향 전환
                boss.transform.rotation = Quaternion.Slerp(boss.transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 15f);
                boss.transform.position += direction * _moveSpeed * Time.deltaTime;
            }
            yield return null;
        }

        // 이동 완료 후 애니메이션 정지
        if (_anim != null)
            _anim.SetBool("Run", false);

        // 6. 이동 후 플레이어를 바라보며 마무리
        yield return StartCoroutine(LookAtTarget(boss));

        Debug.Log("SmokeTeleportPattern 완료");
        onComplete?.Invoke();
    }

    /// <summary>
    /// 플레이어 주변의 랜덤한 위치 중 바닥이 있고 장애물이 없는 곳을 찾습니다.
    /// </summary>
    private bool TryGetValidPositionNearPlayer(out Vector3 position)
    {
        position = Vector3.zero;
        for (int i = 0; i < _maxTryCount; i++)
        {
            // 플레이어 주변 반지름 내 랜덤 좌표 계산
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * _throwRadius;
            Vector3 candidatePos = _playerTransform.position + new Vector3(randomOffset.x, 0f, randomOffset.y);

            // 위에서 아래로 레이를 쏴서 지형 높이 체크
            if (Physics.Raycast(candidatePos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, _groundLayer))
            {
                candidatePos.y = hit.point.y;

                // 해당 위치에 장애물이 있는지 구체 체크
                if (!Physics.CheckSphere(candidatePos, _checkRadius, _obstacleLayer))
                {
                    position = candidatePos;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 패턴 종료 전 보스가 플레이어를 향하도록 회전시킵니다.
    /// </summary>
    private IEnumerator LookAtTarget(BossEnemy boss)
    {
        if (_playerTransform == null) yield break;

        float rotateTime = 0.5f;
        float elapsed = 0f;
        Quaternion startRotation = boss.transform.rotation;
        
        while (elapsed < rotateTime)
        {
            elapsed += Time.deltaTime;
            Vector3 dir = (_playerTransform.position - boss.transform.position).normalized;
            dir.y = 0;
            
            if (dir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dir);
                boss.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / rotateTime);
            }
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 인스펙터에서 기즈모로 투척 범위 시각화
        if (_playerTransform != null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(_playerTransform.position, _throwRadius);
        }
    }
}
