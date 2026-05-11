using System;
using System.Collections;
using UnityEngine;

public class WavePattern : BossPattern
{
    [SerializeField] private GameObject wavePrefab;
    [SerializeField] private float maxRange = 10f;
    [SerializeField] private float holdDuration = 3f;
    [SerializeField] private AudioClip waveCastSound;
    [SerializeField] private AudioClip waveLoopSound;

    private AudioSource waveLoopSource;

    void Awake()
    {
        waveLoopSource = gameObject.AddComponent<AudioSource>();
        waveLoopSource.loop = true;
        waveLoopSource.playOnAwake = false;
        waveLoopSource.spatialBlend = 1f;
    }

    protected override void ExecutePattern(BossEnemy boss, Action onComplete)
    {
        StartCoroutine(EmitCircularWave(boss, onComplete));
    }

    private IEnumerator EmitCircularWave(BossEnemy boss, Action onComplete)
    {
        if (boss.Target == null || Vector3.Distance(boss.transform.position, boss.Target.position) > maxRange)
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
        stage1Boss?.LockLooking();
        stage1Boss?.BossAnimator?.SetTrigger("Wave");

        yield return new WaitUntil(() => fireReady);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPoint(waveCastSound, boss.transform.position);

        if (wavePrefab != null)
            Instantiate(wavePrefab, boss.transform.position, Quaternion.identity);

        if (waveLoopSound != null)
        {
            waveLoopSource.clip = waveLoopSound;
            waveLoopSource.Play();
        }

        yield return new WaitForSeconds(holdDuration);

        waveLoopSource.Stop();
        stage1Boss?.UnlockLooking();
        stage1Boss?.NotifyPatternEnd();
        onComplete?.Invoke();
    }
}