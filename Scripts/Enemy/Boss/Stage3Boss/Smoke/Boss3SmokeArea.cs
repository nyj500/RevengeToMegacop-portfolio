using System.Collections;
using System.Collections.Generic;
using Boss3;
using UnityEngine;

public class Boss3SmokeArea : MonoBehaviour
{
    [Header("SmokeSetting")]
    [SerializeField] ParticleSystem _smokeAreaParticle;
    [SerializeField] float _smokeLifeTime = 5f;


    [Header("연막효과를 관리하는 컨트롤러 오브젝트")]
    [Tooltip("플레이어의 카메라와 적의 메터리얼을 관리하는 오브젝트")]
    [SerializeField] ViewController _viewController;
    
    [Header ("연막 효과음")]
    [Tooltip("연막 에리어가 펼쳐질때 나는 효과음")]
    [SerializeField] AudioClip _SmokeClip;

    
    

    private List<GameObject> _hiddenEnemys = new List<GameObject>();
    private bool _isPlayerInside = false;


    void Awake()
    {
        _viewController = FindFirstObjectByType<ViewController>();
        
        
    }
    

    void OnEnable()
    {
        CreateArea();
        
        
    }

    public void CreateArea()
    {
        StartCoroutine(PlaySmokeArea());

    }

    void OnTriggerEnter(Collider other) {
        Debug.Log(other.name + "연막 안에 들어옴");

        if (other.CompareTag("Enemy"))
        {
           GameObject enemyRoot = other.transform.root.gameObject;

            if (!_hiddenEnemys.Contains(enemyRoot))
            {
                _hiddenEnemys.Add(enemyRoot);
                _viewController.HideEnemy(true,enemyRoot);
            }
            
        }
        if (other.CompareTag("Player"))
        {
            if(_isPlayerInside) return;
            _isPlayerInside = true;
            _viewController.LimitPlayerView(true);
            
        }
    }
    private void OnTriggerExit(Collider other) {
        
        if (other.CompareTag("Player"))
        {
            if(!_isPlayerInside) return;
            _isPlayerInside = false;
            _viewController.LimitPlayerView(false);
        }

        if (other.CompareTag("Enemy"))
        {
            GameObject enemyRoot = other.transform.root.gameObject;

            if (_hiddenEnemys.Contains(enemyRoot))
            {
                
                _viewController.HideEnemy(false,enemyRoot);
                _hiddenEnemys.Remove(enemyRoot);
            }
        }
    }

    IEnumerator PlaySmokeArea()
    {
        _smokeAreaParticle.Play();
        AudioManager.Instance.PlaySFXAtPoint(_SmokeClip,transform.position);
        yield return new WaitForSeconds(_smokeLifeTime);

        _smokeAreaParticle.Stop();

        foreach(GameObject enemy in _hiddenEnemys)
        {
            if(enemy == null) continue;
            _viewController.HideEnemy(false,enemy); 
        }
        _hiddenEnemys.Clear();

        if (_isPlayerInside)
        {
            _viewController.LimitPlayerView(false);
            _isPlayerInside = false;
        }

        yield return null;
        Destroy(gameObject);
    }


    
}
