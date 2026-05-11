using System;
using System.Collections;
using UnityEngine;

public class ScopePattern : BossPattern
{
    [SerializeField]private ScopeScript _scope;
    [SerializeField] GameObject _scopePrefab;
    [SerializeField] Vector3 _scopeSpawnOffset;

    void Start()
    {
        _scope = FindFirstObjectByType<ScopeScript>();
        if(_scope == null)
        {
            _scope = Instantiate(_scopePrefab,transform.position,Quaternion.identity).GetComponent<ScopeScript>();
            _scope.gameObject.SetActive (false);
        }
    }
    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(SpwanScope(boss,onComplete));
    }

    IEnumerator SpwanScope(BossEnemy boss, Action onComplete)
    {
        Debug.Log("scopePattern");

        if (_scope == null)
        {
            Debug.LogWarning("ScopeScript가 없습니다.");
            onComplete?.Invoke();
            yield break;
        }
        //TODO : 플레이어를 천천히 따라가는 스코프를 생성
        if (!_scope._isActive)
        {
            _scope.gameObject.SetActive(true);
            _scope.transform.position = boss.transform.position + _scopeSpawnOffset;
            _scope.StartTrace();

        }
        Debug.Log(_scope._isActive);


        yield return null;
        onComplete?.Invoke();

    }
}
