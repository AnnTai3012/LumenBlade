using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private UnityEngine.UI.Button playButton;
    [SerializeField] private UnityEngine.UI.Button settingsButton;
    [SerializeField] private UnityEngine.UI.Button quitButton;
    [SerializeField] private UnityEngine.UI.Button backButton;
    [SerializeField] private UnityEngine.UI.Slider musicVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValue;
    [SerializeField] private UnityEngine.UI.Slider[] sfxCategorySliders = Array.Empty<UnityEngine.UI.Slider>();
    [SerializeField] private TMP_Text[] sfxCategoryValues = Array.Empty<TMP_Text>();
    [SerializeField] private string firstLevelPath = GameSceneNavigation.FirstLevelPath;
    private AudioSettingsBinding audioSettings;

    private void Awake()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        audioSettings = new AudioSettingsBinding(musicVolumeSlider, musicVolumeValue, sfxCategorySliders, sfxCategoryValues);
        playButton.onClick.AddListener(PlayGame);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(QuitGame);
        backButton.onClick.AddListener(CloseSettings);
    }

    private void Start() => Focus(playButton);
    private void Update()
    {
        if (settingsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) CloseSettings();
    }

    public void PlayGame()
    {
        if (GameSceneNavigation.Load(firstLevelPath, () =>
        {
            PlayerPrefs.Save();
            RunResources.EndCurrentRun();
        })) playButton.interactable = false;
    }

    public void OpenSettings()
    {
        if (GameSceneNavigation.IsLoading) return;
        audioSettings.Refresh();
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
        GameAudioManager.PlayCue(GameAudioCue.SettingsOpen);
        Focus(backButton);
    }

    public void CloseSettings()
    {
        PlayerPrefs.Save();
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
        GameAudioManager.PlayCue(GameAudioCue.SettingsClose);
        Focus(settingsButton);
    }

    public void QuitGame() => GameSceneNavigation.QuitApplication();
    private static void Focus(UnityEngine.UI.Button button)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void OnDestroy()
    {
        audioSettings?.Dispose();
        if (playButton != null) playButton.onClick.RemoveListener(PlayGame);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        if (backButton != null) backButton.onClick.RemoveListener(CloseSettings);
    }
}
