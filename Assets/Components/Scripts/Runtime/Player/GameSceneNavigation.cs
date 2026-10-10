using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSceneNavigation
{
    public const string MenuSceneName = "Main Menu";
    public const string MenuScenePath = "Assets/Objects/World/Scenes/Main Menu.unity";
    public const string FirstLevelPath = "Assets/Objects/World/Scenes/Level 1.unity";
    public static bool IsLoading { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => IsLoading = false;

    public static bool Load(string scenePath, Action beforeLoad = null)
    {
        if (IsLoading) return false;
        if (string.IsNullOrEmpty(scenePath) || !Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError("Scene is not enabled in Build Settings: " + scenePath);
            return false;
        }
        IsLoading = true;
        try
        {
            beforeLoad?.Invoke();
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            AsyncOperation operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            operation.completed += _ => IsLoading = false;
            return true;
        }
        catch
        {
            IsLoading = false;
            throw;
        }
    }

    public static void QuitApplication()
    {
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
