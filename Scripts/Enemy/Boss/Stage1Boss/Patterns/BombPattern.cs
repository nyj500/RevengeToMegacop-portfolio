using System;
using System.Collections;
using UnityEngine;

public class BombPattern : BossPattern
{
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float holdDuration = 1f;
    [SerializeField] private AudioClip bombLaunchSound;

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(ThrowBomb(boss, onComplete));
    }

    private IEnumerator ThrowBomb(BossEnemy boss, Action onComplete)
    {
        if (bombPrefab == null || boss.Target == null)
        {
            yield return null;
            onComplete?.Invoke();
            yield break;
        }

        Transform origin = firePoint != null ? firePoint : boss.transform;
        Vector3 toPlayer = new Vector3(boss.Target.position.x - origin.position.x, 0f, boss.Target.position.z - origin.position.z);
        Quaternion spawnRotation = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer.normalized) : Quaternion.identity;
        Bullet bullet = BulletPool.Instance.Get(bombPrefab, origin.position, spawnRotation);
        Stage1BossBomb bomb = bullet as Stage1BossBomb;

        if (bomb == null)
        {
            yield return null;
            onComplete?.Invoke();
            yield break;
        }

        Stage1Boss stage1Boss = boss as Stage1Boss;
        bool fireReady = false;
        stage1Boss?.RegisterPatternCompleteCallback(onComplete);
        stage1Boss?.RegisterFireCallback(() => fireReady = true);
        stage1Boss?.NotifyPatternStart();
        stage1Boss?.BossAnimator?.SetTrigger("Bomb");

        yield return new WaitUntil(() => fireReady);

        float bossToPlayerDist = Vector3.Distance(
            new Vector3(boss.transform.position.x, 0f, boss.transform.position.z),
            new Vector3(boss.Target.position.x, 0f, boss.Target.position.z));
        bomb.SetOwner(boss.gameObject);
        bomb.Launch(origin.position, boss.Target.position, bossToPlayerDist);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPoint(bombLaunchSound, origin.position);

        yield return new WaitForSeconds(holdDuration);
        (boss as Stage1Boss)?.NotifyPatternEnd();
        onComplete?.Invoke();
    }
}