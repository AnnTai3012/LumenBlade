using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameplayFramePacing : MonoBehaviour
{
    private const string SettingsResourceName = "GamePerformanceSettings";
    private static GameplayFramePacing instance;
    private GamePerformanceSettings settings;
    private int previousFrameRate;
    private int previousVSyncCount;
    private float previousFixedStep;
    private float previousMaximumStep;
    private int appliedFrameRate;
    private int appliedVSyncCount;
    private float appliedFixedStep;
    private float appliedMaximumStep;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBeforeFirstScene()
    {
        if (instance == null)
            new GameObject("Gameplay Frame Pacing", typeof(GameplayFramePacing));
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        previousFrameRate = Application.targetFrameRate;
        previousVSyncCount = QualitySettings.vSyncCount;
        previousFixedStep = Time.fixedDeltaTime;
        previousMaximumStep = Time.maximumDeltaTime;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += RestoreEditorFramePacing;
#endif
        settings = Resources.Load<GamePerformanceSettings>(SettingsResourceName);
        ApplySettings();
    }

    public void ApplySettings()
    {
        appliedFrameRate = settings != null ? settings.TargetFrameRate : 60;
        appliedFixedStep = 1f / (settings != null ? settings.PhysicsFrequency : 60);
        appliedMaximumStep = settings != null ? settings.MaximumPhysicsCatchUp : 0.1f;
        appliedVSyncCount = 0;
        // The Editor uses a software cap; compatible desktop displays use native pacing in builds.
        if (!Application.isEditor && !Application.isMobilePlatform &&
            (settings == null || settings.UseVSyncWhenCompatible))
            appliedVSyncCount = CalculateVSyncCount(appliedFrameRate, Screen.currentResolution.refreshRateRatio.value);

        Application.targetFrameRate = appliedFrameRate;
        QualitySettings.vSyncCount = appliedVSyncCount;
        Time.fixedDeltaTime = appliedFixedStep;
        Time.maximumDeltaTime = appliedMaximumStep;
    }

    internal static int CalculateVSyncCount(int targetFrameRate, double refreshRate)
    {
        if (targetFrameRate <= 0 || double.IsNaN(refreshRate) || double.IsInfinity(refreshRate) || refreshRate <= 0)
            return 0;

        double divider = System.Math.Round(refreshRate / targetFrameRate);
        if (divider < 1 || divider > 4)
            return 0;
        return System.Math.Abs(refreshRate / divider - targetFrameRate) <= 0.5 ? (int)divider : 0;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused && instance == this)
            ApplySettings();
    }

#if UNITY_EDITOR
    private void RestoreEditorFramePacing(UnityEditor.PlayModeStateChange state)
    {
        if (state != UnityEditor.PlayModeStateChange.EnteredEditMode)
            return;

        UnityEditor.EditorApplication.playModeStateChanged -= RestoreEditorFramePacing;
        // The Editor can restore its cached Play Mode FPS after OnDestroy has already run.
        if (Application.targetFrameRate == appliedFrameRate) Application.targetFrameRate = previousFrameRate;
        if (QualitySettings.vSyncCount == appliedVSyncCount) QualitySettings.vSyncCount = previousVSyncCount;
    }
#endif

    private void OnDestroy()
    {
        if (instance != this)
            return;

        // Restore only values still owned by this host, including when leaving Editor Play Mode.
        if (Application.targetFrameRate == appliedFrameRate) Application.targetFrameRate = previousFrameRate;
        if (QualitySettings.vSyncCount == appliedVSyncCount) QualitySettings.vSyncCount = previousVSyncCount;
        if (Mathf.Approximately(Time.fixedDeltaTime, appliedFixedStep)) Time.fixedDeltaTime = previousFixedStep;
        if (Mathf.Approximately(Time.maximumDeltaTime, appliedMaximumStep)) Time.maximumDeltaTime = previousMaximumStep;
        instance = null;
    }
}
