using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// StageSelect 허브 씬에 단일 인스턴스로 배치되는 게임 클리어 패널 컨트롤러.
/// 현재 활성 스테이지의 EnemySpawner 를 StageSelectController 가 런타임에 주입하며,
/// 보스 사망 시 패널을 표시하고 다음 스테이지/재시작/메인메뉴 전환을 처리한다.
/// </summary>
public class GameClearController : MonoBehaviour
{
    [SerializeField] private CanvasGroup gameClearCanvasGroup;
    [SerializeField] private Button nextStageButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    private EnemySpawner spawner;
    private BossEnemy boss;
    private string currentStageScene;
    private string nextStageScene;

    void Start()
    {
        gameClearCanvasGroup.alpha = 0f;
        gameClearCanvasGroup.interactable = false;
        gameClearCanvasGroup.blocksRaycasts = false;

        nextStageButton.onClick.AddListener(OnNextStageClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    /// <summary>
    /// StageSelectController 가 스테이지 시작 시 호출. 이전 바인딩을 해제하고 새 스폰너를 구독한다.
    /// nextStageSceneName 이 null 이면 마지막 스테이지로 간주해 "다음 스테이지" 버튼을 숨긴다.
    /// </summary>
    public void BindToCurrentStage(EnemySpawner newSpawner, string currentStageSceneName, string nextStageSceneName)
    {
        // 이전 바인딩 해제
        if (spawner != null)
            spawner.OnBossSpawned -= OnBossSpawned;
        if (boss != null)
            boss.OnDeath -= OnBossDeath;

        spawner = newSpawner;
        boss = null;
        currentStageScene = currentStageSceneName;
        nextStageScene = nextStageSceneName;

        bool hasNext = !string.IsNullOrEmpty(nextStageScene);
        nextStageButton.gameObject.SetActive(hasNext);

        if (spawner != null)
            spawner.OnBossSpawned += OnBossSpawned;
        else
            Debug.LogWarning("[GameClearController] BindToCurrentStage: spawner 가 null 입니다.");
    }

    private void OnBossSpawned(BossEnemy spawnedBoss)
    {
        boss = spawnedBoss;
        boss.OnDeath += OnBossDeath;
        spawner.OnBossSpawned -= OnBossSpawned;
        spawner = null;
    }

    private void OnBossDeath(Enemy _)
    {
        Time.timeScale = 0f;
        gameClearCanvasGroup.alpha = 1f;
        gameClearCanvasGroup.interactable = true;
        gameClearCanvasGroup.blocksRaycasts = true;

        GameObject focusTarget = nextStageButton.gameObject.activeSelf
            ? nextStageButton.gameObject
            : restartButton.gameObject;
        EventSystem.current.SetSelectedGameObject(focusTarget);
    }

    private void OnNextStageClicked()
    {
        Time.timeScale = 1f;
        StageSelectController.PendingRestartSceneName = nextStageScene;
        SceneManager.LoadScene("StageSelect");
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        StageSelectController.PendingRestartSceneName = currentStageScene;
        SceneManager.LoadScene("StageSelect");
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        if (boss != null)
            boss.OnDeath -= OnBossDeath;
        if (spawner != null)
            spawner.OnBossSpawned -= OnBossSpawned;
    }
}
