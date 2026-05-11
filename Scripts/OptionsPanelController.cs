using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionsPanelController : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button backButton;
    [SerializeField] private CanvasGroup canvasGroup;

    public event Action OnBackButtonClicked;

    void Start()
    {
        VolumeSettings.LoadAndApply(audioMixer);

        masterVolumeSlider.value = VolumeSettings.GetVolume("MasterVolume");
        bgmVolumeSlider.value = VolumeSettings.GetVolume("BGMVolume");
        sfxVolumeSlider.value = VolumeSettings.GetVolume("SFXVolume");

        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        backButton.onClick.AddListener(OnBackClicked);
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        EventSystem.current.SetSelectedGameObject(masterVolumeSlider.gameObject);
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        PlayerPrefs.Save();
    }

    private void OnMasterVolumeChanged(float value)
    {
        VolumeSettings.SetVolume(audioMixer, "MasterVolume", value);
    }

    private void OnBGMVolumeChanged(float value)
    {
        VolumeSettings.SetVolume(audioMixer, "BGMVolume", value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        VolumeSettings.SetVolume(audioMixer, "SFXVolume", value);
    }

    private void OnBackClicked()
    {
        OnBackButtonClicked?.Invoke();
    }
}
