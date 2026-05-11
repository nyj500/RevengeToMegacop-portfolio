using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private OptionsPanelController optionsPanelController;
    [SerializeField] private CanvasGroup mainMenuCanvasGroup;

    void Start()
    {
        startButton.onClick.AddListener(OnStartClicked);
        optionsButton.onClick.AddListener(OnOptionsClicked);
        quitButton.onClick.AddListener(OnQuitClicked);
        optionsPanelController.OnBackButtonClicked += OnOptionsBackClicked;
        EventSystem.current.SetSelectedGameObject(startButton.gameObject);
    }

    private void OnStartClicked()
    {
        SceneManager.LoadScene("StageSelect");
    }

    private void OnOptionsClicked()
    {
        mainMenuCanvasGroup.alpha = 0f;
        mainMenuCanvasGroup.interactable = false;
        mainMenuCanvasGroup.blocksRaycasts = false;
        optionsPanelController.Show();
    }

    private void OnOptionsBackClicked()
    {
        optionsPanelController.Hide();
        mainMenuCanvasGroup.alpha = 1f;
        mainMenuCanvasGroup.interactable = true;
        mainMenuCanvasGroup.blocksRaycasts = true;
        EventSystem.current.SetSelectedGameObject(optionsButton.gameObject);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
