using System;
using System.Collections;
using UnityEngine;

public class MovePattern : BossPattern
{
    [SerializeField]private float _moveSpeed = 15f;

    [Header("arive")]
    [SerializeField] private float _arriveDistance = 1f;
    

    [Header("Target")]
    [SerializeField] private Transform _player;

    [Header("Distance Range From Player")]
    [SerializeField] private float _minDistance = 4f;
    [SerializeField] private float _maxDistance = 8f;

    [Header("Validation")]
    [SerializeField] private int _maxTryCount = 20;
    [SerializeField] private float _checkRadius = 0.5f;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private LayerMask _groundLayer;

    [Header("Result")]
    [SerializeField] private Vector3 _nextMovePosition;

    [Header("RotateDuration")]
    [SerializeField] private float rotateDuration = 1;

    public Vector3 NextMovePosition => _nextMovePosition;

    private Animator _anim;

    void Awake() {
        _player = FindAnyObjectByType<PlayerController>().transform;
        _anim = GetComponentInParent<Animator>();
    }

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        Debug.Log("movePattern");
        StartCoroutine(MoveBoss(boss,onComplete));
    }

    IEnumerator MoveBoss(BossEnemy boss, Action onComplete)
    {
        bool found = PickRandomPositionAroundPlayer();

        if (!found)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (found)
        {
            _anim.SetBool("Run" , true);

            Vector3 moveTarget = _nextMovePosition;


            while (Vector3.Distance(boss.transform.position, _nextMovePosition) > _arriveDistance)
        {
            Vector3 direction = (_nextMovePosition - boss.transform.position).normalized;
            direction.y = 0;
            boss.transform.rotation = Quaternion.LookRotation(direction);
            boss.transform.position += direction * _moveSpeed * Time.deltaTime;

            yield return null;
        }
        }

        Debug.Log(_nextMovePosition);

        
        _anim.SetBool("Run" , false);


        yield return StartCoroutine(LoockTarget(boss));

        onComplete?.Invoke();
    }

    IEnumerator LoockTarget(BossEnemy boss)
    {
        if (_player == null) yield break;

        Quaternion startRotation = boss.transform.rotation;

        Vector3 direction = (_player.position - boss.transform.position).normalized;

        // Y축만 회전하게 하고 싶으면 direction.y = 0;
        direction.y = 0f;

        if (direction == Vector3.zero) yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        float time = 0f;

        while (time < rotateDuration)
        {

            Debug.Log("Rotaion");
            time += Time.deltaTime;
            float t = time / rotateDuration;

            boss.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;

        }
    }

    private bool PickRandomPositionAroundPlayer()
    {
         for (int i = 0; i < _maxTryCount; i++)
        {
            // 1. 랜덤 방향 뽑기 (XZ 평면 기준)
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized;
            Vector3 randomDir = new Vector3(randomCircle.x, 0f, randomCircle.y);

            // 2. 랜덤 거리 뽑기
            float randomDistance = UnityEngine.Random.Range(_minDistance, _maxDistance);

            // 3. 후보 위치 생성
            Vector3 candidatePos = _player.position + randomDir * randomDistance;

            // 4. 바닥 체크 (위에서 아래로 레이캐스트)
            if (Physics.Raycast(candidatePos + Vector3.up * 5f, Vector3.down, out RaycastHit groundHit, 10f, _groundLayer))
            {
                candidatePos.y = transform.position.y;

                // 5. 장애물 겹침 검사
                bool isBlocked = Physics.CheckSphere(candidatePos, _checkRadius, _obstacleLayer);

                if (!isBlocked)
                {
                    _nextMovePosition = candidatePos;
                    return true;
                }
            }
        }

        Debug.LogWarning("유효한 이동 위치를 찾지 못했습니다.");
        return false;
    }
    private void OnDrawGizmosSelected()
    {
        // 플레이어 기준 거리 범위 표시
        if (_player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_player.position, _minDistance);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_player.position, _maxDistance);
        }

        // 선택된 목표 위치 표시
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_nextMovePosition, 0.3f);
    }
}
