using System.Collections;
using System.Collections.Generic;
using System.Linq;

using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스테이지 선택 허브 씬의 메인 상태머신.
/// Stage 씬 3개를 Additive 로드 후 선택/전환 연출을 처리하고,
/// 게임 시작 시 Cinemachine Priority 블렌딩으로 카메라를 탑다운 각도로 전환한다.
/// </summary>
public class StageSelectController : MonoBehaviour
{
    /// <summary>
    /// GameOver 에서 재시도 시 StageSelect 재로드 후 자동 시작할 씬 이름.
    /// StageSelect Initialize 가 소비한 뒤 null 로 리셋한다.
    /// </summary>
    public static string PendingRestartSceneName;

    [SerializeField] private StageEntry[] stages;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private CinemachineBrain cameraBrain;
    [SerializeField] private CinemachineCamera previewVCam;
    [SerializeField] private CinemachineCamera gameplayVCam;
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private CanvasGroup stageInfoPanel;
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text stageDescText;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;
    [SerializeField] private CanvasGroup gameplayHud;
    [SerializeField] private GameClearController gameClearController;
    [SerializeField] private BossHpBar bossHpBar;

    [Header("Debug")]
    [SerializeField] private Toggle skipToBossToggle;

    [SerializeField] private int previewPriority = 10;
    [SerializeField] private int gameplayPriority = 20;
    [SerializeField] private int inactiveGameplayPriority = 5;
    [SerializeField] private float gameplayBlendDuration = 1.2f;
    [SerializeField] private float fadeDuration = 0.35f;

    private int currentIndex;
    private bool isLoading = true;
    private bool isSwitching;

    void Start()
    {
        if (stages == null || stages.Length != 3)
            Debug.LogWarning("[StageSelectController] stages 배열에 정확히 3개의 StageEntry 를 설정해야 합니다.");

        prevButton.onClick.AddListener(OnPrevClicked);
        nextButton.onClick.AddListener(OnNextClicked);
        startButton.onClick.AddListener(OnStartClicked);
        backButton.onClick.AddListener(OnBackClicked);

#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
        if (skipToBossToggle != null) skipToBossToggle.gameObject.SetActive(false);
#endif

        StartCoroutine(InitializeRoutine());
    }

    void OnDestroy()
    {
        prevButton.onClick.RemoveListener(OnPrevClicked);
        nextButton.onClick.RemoveListener(OnNextClicked);
        startButton.onClick.RemoveListener(OnStartClicked);
        backButton.onClick.RemoveListener(OnBackClicked);
    }

    private IEnumerator InitializeRoutine()
    {
        playerController.enabled = false;
        previewVCam.Priority = previewPriority;
        gameplayVCam.Priority = inactiveGameplayPriority;
        screenFader.SetOpaque();
        stageInfoPanel.alpha = 0f;
        stageInfoPanel.interactable = false;
        stageInfoPanel.blocksRaycasts = false;

        gameplayHud.alpha = 0f;
        gameplayHud.interactable = false;
        gameplayHud.blocksRaycasts = false;

        // 3개 Stage 씬을 병렬로 Additive 로드
        List<AsyncOperation> loadOps = new();
        foreach (StageEntry stage in stages)
            loadOps.Add(SceneManager.LoadSceneAsync(stage.sceneName, LoadSceneMode.Additive));

        while (loadOps.Exists(op => !op.isDone))
            yield return null;

        // StageRoot / PlayerSpawn 캐싱 및 전체 비활성화
        foreach (StageEntry stage in stages)
        {
            Scene loadedScene = SceneManager.GetSceneByName(stage.sceneName);
            GameObject[] roots = loadedScene.GetRootGameObjects();
            stage.stageRoot = roots.FirstOrDefault(root => root.name == "StageRoot");
            if (stage.stageRoot != null)
            {
                stage.playerSpawn = stage.stageRoot.transform.Find("PlayerSpawn");
                stage.stageRoot.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"[StageSelectController] '{stage.sceneName}' 씬에 'StageRoot' 이름의 루트 오브젝트가 없습니다.");
            }
        }

        int startIndex = 0;
        if (!string.IsNullOrEmpty(PendingRestartSceneName))
        {
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i].sceneName == PendingRestartSceneName)
                {
                    startIndex = i;
                    break;
                }
            }
        }

        currentIndex = startIndex;
        if (stages[startIndex].stageRoot != null)
            stages[startIndex].stageRoot.SetActive(true);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(stages[startIndex].sceneName));
        SnapPlayerToStage(startIndex);
        SnapPreviewVCam();
        UpdateStageInfoUI();

        stageInfoPanel.alpha = 1f;
        stageInfoPanel.interactable = true;
        stageInfoPanel.blocksRaycasts = true;

        yield return screenFader.FadeOut(fadeDuration);

        isLoading = false;

        if (!string.IsNullOrEmpty(PendingRestartSceneName))
        {
            PendingRestartSceneName = null;
            StartCoroutine(StartGameRoutine());
        }
    }

    private IEnumerator SwitchStageRoutine(int nextIndex)
    {
        isSwitching = true;
        stageInfoPanel.interactable = false;
        stageInfoPanel.blocksRaycasts = false;

        yield return screenFader.FadeIn(fadeDuration);

        if (stages[currentIndex].stageRoot != null)
            stages[currentIndex].stageRoot.SetActive(false);

        currentIndex = nextIndex;

        if (stages[currentIndex].stageRoot != null)
            stages[currentIndex].stageRoot.SetActive(true);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(stages[currentIndex].sceneName));

        SnapPlayerToStage(currentIndex);
        SnapPreviewVCam();
        UpdateStageInfoUI();

        yield return screenFader.FadeOut(fadeDuration);

        stageInfoPanel.interactable = true;
        stageInfoPanel.blocksRaycasts = true;
        isSwitching = false;
    }

    private IEnumerator StartGameRoutine()
    {
        isSwitching = true;
        stageInfoPanel.alpha = 0f;
        stageInfoPanel.interactable = false;
        stageInfoPanel.blocksRaycasts = false;

        cameraBrain.DefaultBlend = new CinemachineBlendDefinition(
            CinemachineBlendDefinition.Styles.EaseInOut, gameplayBlendDuration);
        gameplayVCam.Priority = gameplayPriority;

        yield return null; // 한 프레임 양보 후 IsBlending 이 true 가 됨
        while (cameraBrain.IsBlending)
            yield return null;

        playerController.enabled = true;
        gameplayHud.alpha = 1f;
        isSwitching = false;

        GameObject currentRoot = stages[currentIndex].stageRoot;
        if (currentRoot != null)
        {
            EnemySpawner spawner = currentRoot.GetComponentInChildren<EnemySpawner>(true);
            if (spawner != null)
            {
                spawner.SkipToBoss = skipToBossToggle != null && skipToBossToggle.isOn;
                spawner.gameObject.SetActive(true);
            }

            if (gameClearController != null)
            {
                string currentSceneName = stages[currentIndex].sceneName;
                string nextSceneName = (currentIndex + 1 < stages.Length) ? stages[currentIndex + 1].sceneName : null;
                gameClearController.BindToCurrentStage(spawner, currentSceneName, nextSceneName);
            }

            if (bossHpBar != null)
                bossHpBar.BindSpawner(spawner);
        }
    }

    private void OnPrevClicked()
    {
        if (isLoading || isSwitching)
            return;
        int previousIndex = (currentIndex - 1 + stages.Length) % stages.Length;
        StartCoroutine(SwitchStageRoutine(previousIndex));
    }

    private void OnNextClicked()
    {
        if (isLoading || isSwitching)
            return;
        int nextIndex = (currentIndex + 1) % stages.Length;
        StartCoroutine(SwitchStageRoutine(nextIndex));
    }

    private void OnStartClicked()
    {
        if (isLoading || isSwitching)
            return;
        StartCoroutine(StartGameRoutine());
    }

    private void OnBackClicked()
    {
        if (isLoading || isSwitching)
            return;
        SceneManager.LoadScene("MainMenu");
    }

    private void SnapPlayerToStage(int index)
    {
        Transform spawn = stages[index].playerSpawn;
        if (spawn == null)
            return;
        // CharacterController 가 활성 상태에서 transform.position 을 직접 변경하면
        // 내부 상태가 불일치할 수 있다. 이동 전에 비활성화 후 복원한다.
        playerTransform.TryGetComponent(out CharacterController characterController);
        if (characterController != null) characterController.enabled = false;
        playerTransform.SetPositionAndRotation(spawn.position, spawn.rotation);
        if (characterController != null) characterController.enabled = true;
        if (playerTransform.TryGetComponent(out Rigidbody playerRigidbody))
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
        Physics.SyncTransforms();
    }

    private void SnapPreviewVCam()
    {
        // 플레이어 순간이동 후 Cinemachine damping 잔여를 제거해 카메라 날아가는 현상 방지
        previewVCam.PreviousStateIsValid = false;
    }

    private void UpdateStageInfoUI()
    {
        StageEntry currentStage = stages[currentIndex];
        stageNameText.text = currentStage.displayName;
        stageDescText.text = currentStage.description;
    }
}
