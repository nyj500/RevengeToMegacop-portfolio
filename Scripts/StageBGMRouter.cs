using System;

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 활성 씬에 따라 매칭되는 BGM을 AudioManager로 재생 요청한다.
/// AudioManager와 동일한 GameObject(DontDestroyOnLoad)에 배치한다.
/// </summary>
[RequireComponent(typeof(AudioManager))]
public class StageBGMRouter : MonoBehaviour
{
    [Serializable]
    public struct Entry
    {
        public string sceneName;
        public AudioClip clip;
    }

    [SerializeField] private Entry[] entries;
    [SerializeField] private float crossfadeDuration = 1f;
    [SerializeField] private bool stopOnUnmatchedScene = false;

    private AudioManager audioManager;

    void Awake()
    {
        audioManager = GetComponent<AudioManager>();
    }

    void OnEnable()
    {
        SceneManager.activeSceneChanged += HandleSceneChanged;
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleSceneChanged;
    }

    void Start()
    {
        ApplyForScene(SceneManager.GetActiveScene().name);
    }

    private void HandleSceneChanged(Scene previous, Scene next)
    {
        ApplyForScene(next.name);
    }

    private void ApplyForScene(string sceneName)
    {
        if (entries == null)
            return;

        for (int index = 0; index < entries.Length; index++)
        {
            if (entries[index].sceneName == sceneName)
            {
                if (entries[index].clip != null)
                    audioManager.PlayBGM(entries[index].clip, crossfadeDuration);
                return;
            }
        }

        if (stopOnUnmatchedScene)
            audioManager.StopBGM(crossfadeDuration);
    }
}
