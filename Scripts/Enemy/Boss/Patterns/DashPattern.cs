using System;
using System.Collections;

using UnityEngine;

public class DashPattern : BossPattern
{
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.5f;

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(DashTowardsTarget(boss, onComplete));
    }

    private IEnumerator DashTowardsTarget(BossEnemy boss, Action onComplete)
    {
        if (boss == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Transform target = boss.Target;
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector3 direction = (target.position - boss.transform.position).normalized;
        direction.y = 0f;

        var agent = boss.NavAgent;
        if (agent != null) agent.ResetPath();

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            if (boss == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 delta = direction * (dashSpeed * Time.deltaTime);
            if (agent != null)
                agent.Move(delta);
            else
                boss.transform.position += delta;
            elapsed += Time.deltaTime;
            yield return null;
        }

        onComplete?.Invoke();
    }
}
