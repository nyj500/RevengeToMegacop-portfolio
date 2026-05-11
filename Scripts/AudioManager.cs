using System.Collections;

using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGM과 SFX 재생을 담당하는 싱글톤 매니저.
/// MainMixer의 BGM/SFX 그룹을 통해 AudioMixer 볼륨 설정과 자동으로 연동된다.
/// 씬에 별도 GameObject로 배치하고 audioMixer 필드에 MainMixer를 할당한다.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private int sfxPoolSize = 8;
    [SerializeField] private float defaultCrossfadeDuration = 1f;

    private AudioMixerGroup bgmGroup;
    private AudioMixerGroup sfxGroup;

    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private AudioSource activeBgmSource;
    private Coroutine crossfadeCoroutine;

    private AudioSource[] sfxSources;
    private int nextSfxIndex;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeMixerGroups();
        InitializeBgmSources();
        InitializeSfxPool();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void InitializeMixerGroups()
    {
        AudioMixerGroup[] bgmGroups = audioMixer.FindMatchingGroups("BGM");
        AudioMixerGroup[] sfxGroups = audioMixer.FindMatchingGroups("SFX");

        bgmGroup = bgmGroups.Length > 0 ? bgmGroups[0] : null;
        sfxGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;
    }

    private void InitializeBgmSources()
    {
        bgmSourceA = CreateAudioSource("BGM_A", bgmGroup, loop: true);
        bgmSourceB = CreateAudioSource("BGM_B", bgmGroup, loop: true);
        activeBgmSource = bgmSourceA;
    }

    private void InitializeSfxPool()
    {
        sfxSources = new AudioSource[sfxPoolSize];
        for (int index = 0; index < sfxPoolSize; index++)
            sfxSources[index] = CreateAudioSource($"SFX_{index}", sfxGroup, loop: false);
    }

    private AudioSource CreateAudioSource(string sourceName, AudioMixerGroup mixerGroup, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);

        AudioSource audioSource = sourceObject.AddComponent<AudioSource>();
        audioSource.outputAudioMixerGroup = mixerGroup;
        audioSource.loop = loop;
        audioSource.playOnAwake = false;
        return audioSource;
    }

    /// <summary>
    /// BGM을 기본 크로스페이드 시간으로 재생한다.
    /// 현재 재생 중인 클립과 동일하면 무시된다.
    /// </summary>
    /// <param name="clip">재생할 BGM 클립</param>
    public void PlayBGM(AudioClip clip)
    {
        PlayBGM(clip, defaultCrossfadeDuration);
    }

    /// <summary>
    /// BGM을 지정한 크로스페이드 시간으로 재생한다.
    /// </summary>
    /// <param name="clip">재생할 BGM 클립</param>
    /// <param name="fadeDuration">크로스페이드 시간(초). 0이면 즉시 전환.</param>
    public void PlayBGM(AudioClip clip, float fadeDuration)
    {
        if (clip == null)
            return;

        if (activeBgmSource.clip == clip && activeBgmSource.isPlaying)
            return;

        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        if (fadeDuration <= 0f)
        {
            activeBgmSource.Stop();
            activeBgmSource.clip = clip;
            activeBgmSource.volume = 1f;
            activeBgmSource.Play();
            return;
        }

        AudioSource incomingSource = activeBgmSource == bgmSourceA ? bgmSourceB : bgmSourceA;
        crossfadeCoroutine = StartCoroutine(CrossfadeRoutine(activeBgmSource, incomingSource, clip, fadeDuration));
        activeBgmSource = incomingSource;
    }

    /// <summary>
    /// 현재 재생 중인 BGM을 서서히 정지한다.
    /// </summary>
    /// <param name="fadeDuration">페이드아웃 시간(초)</param>
    public void StopBGM(float fadeDuration = 1f)
    {
        if (!activeBgmSource.isPlaying)
            return;

        if (crossfadeCoroutine != null)
            StopCoroutine(crossfadeCoroutine);

        crossfadeCoroutine = StartCoroutine(FadeOutRoutine(activeBgmSource, fadeDuration));
    }

    /// <summary>
    /// SFX 클립을 원샷으로 재생한다. 여러 소리가 동시에 겹쳐 재생된다.
    /// </summary>
    /// <param name="clip">재생할 SFX 클립</param>
    /// <param name="volumeScale">볼륨 배율 [0, 1]</param>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        AudioSource audioSource = sfxSources[nextSfxIndex];
        nextSfxIndex = (nextSfxIndex + 1) % sfxSources.Length;
        audioSource.PlayOneShot(clip, volumeScale);
    }

    /// <summary>
    /// 지정한 월드 위치에서 SFX 클립을 재생한다.
    /// 임시 GameObject를 생성하여 3D 공간음으로 재생한 뒤 자동 소멸한다.
    /// </summary>
    /// <param name="clip">재생할 SFX 클립</param>
    /// <param name="position">재생 위치 (월드 좌표)</param>
    /// <param name="volumeScale">볼륨 배율 [0, 1]</param>
    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        StartCoroutine(PlayAtPointRoutine(clip, position, volumeScale));
    }

    private IEnumerator CrossfadeRoutine(AudioSource outgoingSource, AudioSource incomingSource, AudioClip clip, float fadeDuration)
    {
        incomingSource.clip = clip;
        incomingSource.volume = 0f;
        incomingSource.Play();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / fadeDuration;
            incomingSource.volume = Mathf.Clamp01(progress);
            outgoingSource.volume = Mathf.Clamp01(1f - progress);
            yield return null;
        }

        incomingSource.volume = 1f;
        outgoingSource.volume = 0f;
        outgoingSource.Stop();
        outgoingSource.clip = null;
        crossfadeCoroutine = null;
    }

    private IEnumerator FadeOutRoutine(AudioSource audioSource, float fadeDuration)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Clamp01(startVolume * (1f - elapsed / fadeDuration));
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
        audioSource.clip = null;
        crossfadeCoroutine = null;
    }

    private IEnumerator PlayAtPointRoutine(AudioClip clip, Vector3 position, float volumeScale)
    {
        GameObject pointObject = new GameObject("SFX_Point");
        pointObject.transform.position = position;

        AudioSource pointSource = pointObject.AddComponent<AudioSource>();
        pointSource.outputAudioMixerGroup = sfxGroup;
        pointSource.spatialBlend = 1f;
        pointSource.loop = false;
        pointSource.playOnAwake = false;
        pointSource.volume = volumeScale;
        pointSource.clip = clip;
        pointSource.Play();

        yield return new WaitForSecondsRealtime(clip.length + 0.1f);

        if (pointObject != null)
            Destroy(pointObject);
    }
}
