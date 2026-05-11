using System;
using UnityEngine;

namespace Boss3
{
    

    public class Boss3SmokeBomb : MonoBehaviour
{
    [Header("연막탄 설정")]
    [SerializeField] private float _travelTime = 1.2f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float _arcHeight = 2f;
    [SerializeField] private GameObject smokeEffectPrefab; // 나중에 연막 생성용

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private bool _isMoving;
    private float _timer;
    private bool _hasLanded = false;
    private Action _onExplode;


    private void Awake()
    {
        
    }

    private void OnEnable()
    {
        _timer = 0f;
        _isMoving = false;
        _onExplode = null;
        _hasLanded = false;

        
    }

    public void Throw(Vector3 startPos, Vector3 targetPos, Action onExplode = null)
    {
        
        _startPos = startPos;
        _targetPos = targetPos;
        transform.position = startPos;
        transform.rotation = Quaternion.identity;

        _timer = 0f;
        _isMoving = true;
        _onExplode = onExplode;
        _hasLanded = false;

        CancelInvoke();
        Invoke(nameof(ForceExplode), lifeTime);
        
    }
        void Update()
        {
            if(!_isMoving) return;
            _timer += Time.deltaTime;
            float t = _timer / _travelTime;

            if (t >= 1f)
            {
            transform.position = _targetPos;
            Explode();
            return;
            }
            

            Vector3 pos = Vector3.Lerp(_startPos, _targetPos, t);

            // 포물선 느낌
            pos.y += Mathf.Sin(t * Mathf.PI) * _arcHeight;

            transform.position = pos;
        }


        void OnTriggerEnter(Collider other)
        {
            
            if (_hasLanded) return;
            if (!other.CompareTag("Ground") && !other.CompareTag("Wall"))
            return;

            // 바닥이나 벽에 닿으면 착지로 처리
            _isMoving = false;
            _hasLanded = true;
            Explode();
        }

        private void ForceExplode()
        {
        if (_hasLanded) return;
        _isMoving = false;
        _hasLanded = true;
        Explode();
        }

    private void Explode()
    {
        
        Debug.Log("연막탄 착지 / 폭발");
        _isMoving = false;
        _hasLanded = true;
        // 나중에 연막 생성
        if (smokeEffectPrefab != null)
        {
            Instantiate(smokeEffectPrefab, transform.position, Quaternion.identity);
            
        }

        _onExplode?.Invoke();
        Destroy(gameObject);
    }
}
}