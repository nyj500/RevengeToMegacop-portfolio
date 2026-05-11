using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private PlayerStateController playerStateController;
    [SerializeField] private CanvasGroup gameOverCanvasGroup;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    void Start()
    {
        gameOverCanvasGroup.alpha = 0f;
        gameOverCanvasGroup.interactable = false;
        gameOverCanvasGroup.blocksRaycasts = false;

        playerStateController.OnDeath += OnPlayerDeath;
        restartButton.onClick.AddListener(OnRestartClicked);
        mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnPlayerDeath()
    {
        Time.timeScale = 0f;
        gameOverCanvasGroup.alpha = 1f;
        gameOverCanvasGroup.interactable = true;
        gameOverCanvasGroup.blocksRaycasts = true;
        EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        StageSelectController.PendingRestartSceneName = SceneManager.GetActiveScene().name;
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
        if (playerStateController != null)
            playerStateController.OnDeath -= OnPlayerDeath;
    }
}
