using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScopeScript : MonoBehaviour
{
    [SerializeField] private Transform _targetTF;


    public bool _isActive = false;
    private bool _lockOn = false;
    Camera _mainCamera;

    [Header("scope 셋팅")]
    [SerializeField] private Image _scopeImage;
    [SerializeField] private float _waitTime = 2f;
    [SerializeField] private float _scopeMoveSpeed = 5f;
    [SerializeField] private float _rayDistance = 20f;
    [SerializeField] private float _scopeDamage = 50f;

    [Header("스코프 패턴 사격 효과음")]
    [SerializeField]AudioClip _scopClip;

    

    void Awake()
    {
        _mainCamera = Camera.main;
        _targetTF = FindAnyObjectByType<PlayerController>().transform;
        
    }
    void Start()
    {

        
    }

    void OnEnable()
    {
        
    }

    public void StartTrace()
    {
        if (!_isActive)
        {
            _isActive=true;
            _lockOn = false;
            _scopeImage.gameObject.SetActive(true);

            StartCoroutine(MoveOnTargetAndShot());
        }
        
    }

    IEnumerator MoveOnTargetAndShot()
    {
        while(!_lockOn)
        {
            Vector3 targetDistace = _targetTF.position - transform.position;
            Vector3 nomalTargetDistance = targetDistace.normalized;

            transform.position += nomalTargetDistance * Time.deltaTime *_scopeMoveSpeed;
            _scopeImage.rectTransform.position = _mainCamera.WorldToScreenPoint(transform.position);

            if (Vector3.Distance(transform.position, _targetTF.position) < 0.2f)
            {
                _lockOn = true;
            }
            
            yield return null;

        }

        


        yield return StartCoroutine(ChangeColor());


        Vector2 screenPoint = _scopeImage.rectTransform.position;
        Ray ray = _mainCamera.ScreenPointToRay(screenPoint);
        if (Physics.Raycast(ray, out RaycastHit hit , _rayDistance))
        {
            Debug.Log("맞은 대상: " + hit.collider.name);

            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log("플레이어 명중!");
                GameObject currentplayer = hit.collider.gameObject;
                PlayerStateController playerStateController = GetComponent<PlayerStateController>();
                playerStateController.TakeDamage(_scopeDamage);
            }
        }
        AudioManager.Instance.PlaySFXAtPoint(_scopClip,transform.position);

        ResetScope();
        yield return null;
    }

    

    IEnumerator ChangeColor()
    {
        Color minColor = new Color(1,1,1,0.25f);
        Color maxColor = new Color(1,0,0,0.25f);
        
        float elapsed = 0f;

        while (elapsed < _waitTime)
        {
        elapsed += Time.deltaTime;

        float t = elapsed / _waitTime;
        
        _scopeImage.color = Color.Lerp(minColor,maxColor, t);

        _scopeImage.rectTransform.position = _mainCamera.WorldToScreenPoint(transform.position);
        yield return null;
        }

        _scopeImage.color = maxColor;
        yield return null;
    }


    private void ResetScope()
    {
        _scopeImage.color = new Color(1,1,1,0.25f);;
        transform.position = new Vector3(0,0,0); // TODO 시작 위치 리팩토링 할 것 
        _isActive = false;
        _scopeImage.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
}
