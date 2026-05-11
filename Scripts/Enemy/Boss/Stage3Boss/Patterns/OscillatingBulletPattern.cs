using System; // Action 델리게이트를 사용하기 위해 추가
using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어를 바라보는 방향을 기준으로 -45도에서 +45도 범위로 Y축을 왕복하며 총알을 발사하는 보스 패턴입니다.
/// BossPattern을 상속받아 패턴 실행 로직을 코루틴으로 처리합니다.
/// </summary>
public class OscillatingBulletPattern : BossPattern
{
    [Header("진동 탄막 패턴 설정")]
    
    [SerializeField] GameObject bulletPrefab ;// 총알 프리펩

    [SerializeField]
    private float bulletSpeed = 10f; // 생성될 총알의 속도

    [SerializeField]
    private int bulletsPerShot = 1; // 한 번에 발사되는 총알 수

    [SerializeField]
    private float timeBetweenShots = 0.1f; // 각 총알 발사 간 지연 시간
    
    [SerializeField]
    private float oscillationDuration = 2f; // -45도에서 +45도 또는 그 반대로 한 번 왕복하는 데 걸리는 시간 (초)

    [SerializeField]
    private float minAngle = -45f; // 보스의 전방 방향을 기준으로 한 최소 발사 각도
    
    [SerializeField]
    private float maxAngle = 45f; // 보스의 전방 방향을 기준으로 한 최대 발사 각도

    [SerializeField]
    private float patternDuration = 5f; // 이 패턴이 총 몇 초 동안 실행될지 (onComplete 호출을 위해 필요)

    [SerializeField]
    private Transform firePoint; // 총알이 발사될 위치와 방향을 나타내는 Transform

    [Header("총 사운드")]
    [SerializeField] AudioClip _audioiclip;


    private Transform playerTransform; // 플레이어의 Transform 참조
    private bool movingForward = true; // 현재 진동 방향 (minAngle -> maxAngle이 true, maxAngle -> minAngle이 false)
    private float oscillationTimer = 0f; // 진동 주기 내 타이머

    private Animator _anim;

    /// <summary>
    /// 스크립트 인스턴스가 로드될 때 호출됩니다.
    /// 플레이어 Transform을 찾아 참조를 설정합니다.
    /// </summary>
    protected void Awake()
    {
        

        // 총알 발사 지점을 이 게임 오브젝트의 Transform으로 설정합니다.
        // 이를 통해 보스 자체의 위치에서 총알이 발사됩니다.
        if(firePoint ==null)
        firePoint = this.transform; 
        
        // "Player" 태그를 가진 게임 오브젝트를 찾아 플레이어 Transform을 설정합니다.
        // 더 견고한 플레이어 참조를 위해 의존성 주입을 사용하는 것이 좋습니다.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("OscillatingBulletPattern: 'Player' 태그를 가진 플레이어를 찾을 수 없습니다! 총알이 올바르게 플레이어를 향하지 않을 수 있습니다.");
        }

        _anim = GetComponentInParent<Animator>();
    }
       
    

    /// <summary>
    /// 보스 패턴을 실행하는 추상 메서드의 오버라이드입니다.
    /// 이 메서드는 실제 탄막 발사 로직을 코루틴으로 시작하고, 패턴이 끝나면 onComplete 액션을 호출합니다.
    /// </summary>
    /// <param name="boss">현재 패턴을 실행하는 BossEnemy 인스턴스</param>
    /// <param name="onComplete">패턴 실행 완료 시 호출될 콜백 액션</param>
    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        
       
        // 발사 지점(firePoint)이 할당되지 않았다면 오류를 기록하고 패턴 실행을 중지합니다.
        if (firePoint == null)
        {
            Debug.LogError("OscillatingBulletPattern: firePoint가 할당되지 않았습니다! 패턴을 실행할 수 없습니다.");
            onComplete?.Invoke(); // 패턴 실패로 간주하고 완료 콜백 호출
            return;
        }

        // 플레이어 Transform이 없으면 경고를 기록하고 패턴 실행을 중지합니다.
        if (playerTransform == null)
        {
            Debug.LogWarning("OscillatingBulletPattern: 플레이어 Transform을 찾을 수 없습니다. 패턴을 실행할 수 없습니다.");
            onComplete?.Invoke(); // 패턴 실패로 간주하고 완료 콜백 호출
            return;
        }
        
        // 실제 탄막 발사 로직을 코루틴으로 실행합니다.
        StartCoroutine(FireOscillatingBullets(boss, onComplete));
    }

    /// <summary>
    /// 플레이어를 향해 진동하는 각도로 총알을 발사하는 코루틴 로직입니다.
    /// 이 코루틴은 patternDuration 동안 실행된 후 완료됩니다.
    /// </summary>
    /// <param name="boss">패턴을 실행하는 BossEnemy 인스턴스 (현재 이 패턴에서는 직접 사용되지 않지만 전달됨)</param>
    /// <param name="onComplete">패턴 완료 시 호출될 콜백 액션</param>
    /// <returns>총알 발사 및 진동을 위한 IEnumerator</returns>
    private IEnumerator FireOscillatingBullets(BossEnemy boss, Action onComplete)
    {
        Debug.Log("ShotPattern");
        float currentPatternTime = 0f; // 현재 패턴이 실행된 시간

        // 패턴 지속 시간 동안 반복합니다.
        while (currentPatternTime < patternDuration)
        {
            _anim.SetBool("Soot",true);
            // 보스 위치에서 플레이어를 향하는 정규화된 벡터를 계산합니다.
            Vector3 directionToPlayer = (playerTransform.position - boss.transform.position).normalized;
            
            // 플레이어를 향하는 회전에서 Y축 회전만 추출합니다.
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            Quaternion yOnlyRotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);


            // 현재 진동 타이머와 방향에 따라 발사 각도를 계산합니다.
            float currentRelativeAngle;
            if (movingForward)
            {
                currentRelativeAngle = Mathf.Lerp(minAngle, maxAngle, oscillationTimer / oscillationDuration);
            }
            else
            {
                currentRelativeAngle = Mathf.Lerp(maxAngle, minAngle, oscillationTimer / oscillationDuration);
            }

            // 플레이어를 향하는 기본 회전에 현재 진동 각도를 적용하여 최종 발사 회전을 결정합니다.
            Quaternion fireRotation = yOnlyRotation * Quaternion.Euler(0, currentRelativeAngle, 0);

            boss.transform.rotation = fireRotation;


            // 설정된 총알 수만큼 총알을 발사합니다.
            for (int i = 0; i < bulletsPerShot; i++)
            {
                // BulletPool에서 총알을 가져와 발사 위치와 방향으로 초기화합니다.  
                // BulletPool.Instance는 싱글톤 패턴으로 구현되어 있어야 합니다.    
                if(bulletPrefab != null)
                {
                    Bullet bullet1 = BulletPool.Instance.Get(bulletPrefab,firePoint.position,fireRotation);
                    bullet1.Speed = bulletSpeed;
                    bullet1.SetOwner(boss.gameObject);
                    AudioManager.Instance.PlaySFXAtPoint(_audioiclip,boss.transform.position);
                }

                
            }

            // 다음 총알 발사까지 대기합니다.
            yield return new WaitForSeconds(timeBetweenShots);

            // 진동 타이머를 업데이트합니다.
            oscillationTimer += timeBetweenShots;
            // 진동 주기가 끝나면 방향을 반전하고 타이머를 초기화합니다.
            if (oscillationTimer >= oscillationDuration)
            {
                oscillationTimer = 0f;
                movingForward = !movingForward; // 진동 방향 반전 (예: -45 -> +45 후 +45 -> -45)
            }

            // 전체 패턴 실행 시간을 업데이트합니다.
            currentPatternTime += timeBetweenShots;
            
            _anim.SetBool("Soot",false);

        }


        // 패턴 실행이 완료되면 onComplete 콜백을 호출합니다.
        onComplete?.Invoke();
    }
}