using System;
using System.Collections;
using UnityEngine;

namespace Boss3
{
    

public class SmokeBomb : BossPattern
{
    [Header("연막탄 패턴")]
    [SerializeField] private Boss3SmokeBomb _SmokeBombPrefab;
    [SerializeField] private Transform _throwPoint;
    

    private Transform _PlayerTransform;

    private Animator _anim;

    void Awake()
    {
        if(_throwPoint == null)
        _throwPoint = transform; 

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if(player != null)
        _PlayerTransform = player.transform;

        if(_anim ==null)
        _anim = GetComponentInParent<Animator>();
    }

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        if (_SmokeBombPrefab == null)
        {
            Debug.LogError("SmokeBombPattern: smokeBombPrefab이 비어 있음");
            onComplete?.Invoke();
            return;
        }

        if (_PlayerTransform == null)
        {
            Debug.LogWarning("SmokeBombPattern: 플레이어를 찾지 못함");
            onComplete?.Invoke();
            return;
        }



        StartCoroutine(ThrowSmokeBomb(boss , onComplete));
    }

    IEnumerator ThrowSmokeBomb(BossEnemy boss, Action onComplete)
    {

        
        Debug.Log("SmokeBombPattern Start");
        
        _anim.SetTrigger("Granade");


        yield return new WaitForSeconds(0.3f);

        Vector3 startPos = _throwPoint.position;
        Vector3 targetPos = _PlayerTransform.position;


        Boss3SmokeBomb bomb = Instantiate(_SmokeBombPrefab, startPos, Quaternion.identity);
        bomb.Throw(startPos, targetPos, () =>
        {
            Debug.Log("연막탄 패턴 완료");
            onComplete?.Invoke();
        });

        yield return null;
        
    }

}

}
