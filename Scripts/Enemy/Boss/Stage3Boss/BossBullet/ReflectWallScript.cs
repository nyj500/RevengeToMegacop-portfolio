using UnityEngine;

public class ReflectWallScript : MonoBehaviour
{
    [Header("Bounce")]
    [SerializeField] private int _bounceLimit = 3;
    [SerializeField] private float _bounceCooldown = 0.05f;
    [SerializeField] private float _pushOutDistance = 0.1f;
    [SerializeField] private Vector3 _moveDirection;


    private Bullet _bullet;
    private float _timer;
    private int _bounceCount;
    private float _lastBounceTime;
    
    private void Awake()
    {
        _bullet = GetComponent<Bullet>();
    }
    void OnEnable()
        {
            
            _bounceCount = 0;
            _lastBounceTime = -999f;
            
        }

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // "벽"인지 확인
        WallReflect wall = other.GetComponent<WallReflect>();
        
        if (wall == null) return;

        if (Time.time - _lastBounceTime < _bounceCooldown) return;

        ReflectWall(other);
    }
    private void ReflectWall(Collider other)
    {
        Vector3 normal = GetWallNormal(other);

        // 현재 진행 방향 기준으로 반사
        Vector3 reflectedDir = Vector3.Reflect(transform.forward, normal).normalized;

        // 방향 반영
        transform.forward = new Vector3(reflectedDir.x, 0, reflectedDir.z);

        // 벽에 박히는 현상 방지
        transform.position += normal * _pushOutDistance;

        _bounceCount++;
        _lastBounceTime = Time.time;

        if (_bounceCount >= _bounceLimit)
        {
            if (_bullet != null)
            {
                _bullet.Remove();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

        private Vector3 GetWallNormal(Collider other)
        {
            Vector3 closestPoint = other.ClosestPoint(transform.position);
            Vector3 normal = (transform.position - closestPoint).normalized;

            // 혹시 이상한 값 나오면 안전장치
            if (normal == Vector3.zero)
            {
                normal = -other.transform.forward;
            }

            return normal;
        }
        
    
}
