using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SharedGameplaySceneSetup
{
    private const string HudPrefabPath = "Assets/Components/UI/Gameplay/Game HUD.prefab";
    private const string PlayerPrefabPath = "Assets/Objects/Player/Player.prefab";
    private const string SettingsPath = "Assets/Components/UI/Gameplay/Resources and UI Settings.asset";

    [MenuItem("Tools/BTL/Install Shared Gameplay UI and Respawn in Current Scene")]
    public static void InstallInActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Install shared gameplay UI outside Play Mode.");

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrEmpty(scene.path))
            throw new InvalidOperationException("Open a saved gameplay scene before installing the shared UI.");

        GameResourceSettings settings = AssetDatabase.LoadAssetAtPath<GameResourceSettings>(SettingsPath);
        GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
        if (settings == null || hudPrefab == null)
            throw new InvalidOperationException("The shared HUD prefab and resource settings asset are required.");

        Health playerHealth = FindInScene<Health>(scene);
        PlayerStamina playerStamina = FindInScene<PlayerStamina>(scene);
        if (playerHealth == null || playerStamina == null)
            throw new InvalidOperationException($"Scene '{scene.path}' needs a Player with Health and PlayerStamina components.");

        GameplayPanelController panels = FindInScene<GameplayPanelController>(scene);
        if (panels == null)
        {
            GameObject hud = PrefabUtility.InstantiatePrefab(hudPrefab, scene) as GameObject;
            if (hud == null)
                throw new InvalidOperationException("Could not instantiate the shared HUD prefab.");
            Undo.RegisterCreatedObjectUndo(hud, "Install shared gameplay HUD");
            panels = hud.GetComponent<GameplayPanelController>();
            if (panels == null)
                throw new InvalidOperationException("The shared HUD prefab has no GameplayPanelController.");
        }

        EnsureEventSystem(scene);
        EnsureRunResources(scene, settings);
        EnsureLevelResourceFlow(scene, settings, panels, playerHealth, playerStamina);
        EnsurePlayerRespawnComponent();
        StatueRecoverySceneSetup.InstallInScene(scene);
        EnsureSceneInBuildSettings(scene.path);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Could not save scene '{scene.path}'.");

        AssetDatabase.SaveAssets();
        Debug.Log($"Installed the shared HUD, level resource flow, and start checkpoint respawn in '{scene.path}'.");
    }

    private static void EnsureEventSystem(Scene scene)
    {
        if (FindInScene<EventSystem>(scene) != null)
            return;

        var eventSystem = new GameObject("UI EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        SceneManager.MoveGameObjectToScene(eventSystem, scene);
        Undo.RegisterCreatedObjectUndo(eventSystem, "Install UI EventSystem");
    }

    private static void EnsureRunResources(Scene scene, GameResourceSettings settings)
    {
        RunResources resources = FindInScene<RunResources>(scene);
        if (resources == null)
        {
            var owner = new GameObject("Run Resources");
            SceneManager.MoveGameObjectToScene(owner, scene);
            Undo.RegisterCreatedObjectUndo(owner, "Install Run Resources");
            resources = Undo.AddComponent<RunResources>(owner);
        }

        SetObjectReference(resources, "settings", settings);
    }

    private static void EnsureLevelResourceFlow(
        Scene scene,
        GameResourceSettings settings,
        GameplayPanelController panels,
        Health playerHealth,
        PlayerStamina playerStamina)
    {
        LevelResourceFlow flow = FindInScene<LevelResourceFlow>(scene);
        if (flow == null)
        {
            var flowObject = new GameObject("Level Resources");
            SceneManager.MoveGameObjectToScene(flowObject, scene);
            Undo.RegisterCreatedObjectUndo(flowObject, "Install Level Resource Flow");
            flow = Undo.AddComponent<LevelResourceFlow>(flowObject);
        }

        SetObjectReference(flow, "settings", settings);
        SetObjectReference(flow, "panels", panels);
        SetObjectReference(flow, "playerHealth", playerHealth);
        SetObjectReference(flow, "playerStamina", playerStamina);
        EditorUtility.SetDirty(flow);
    }

    private static void EnsurePlayerRespawnComponent()
    {
        GameObject playerPrefab = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (playerPrefab == null)
            throw new InvalidOperationException($"Could not load player prefab '{PlayerPrefabPath}'.");

        try
        {
            if (playerPrefab.GetComponent<PlayerCheckpointRespawn>() != null)
                return;

            playerPrefab.AddComponent<PlayerCheckpointRespawn>();
            if (PrefabUtility.SaveAsPrefabAsset(playerPrefab, PlayerPrefabPath) == null)
                throw new InvalidOperationException("Could not save PlayerCheckpointRespawn to the Player prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(playerPrefab);
        }
    }

    private static void EnsureSceneInBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        for (int i = 0; i < current.Length; i++)
        {
            if (!string.Equals(current[i].path, scenePath, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!current[i].enabled)
            {
                current[i] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = current;
            }
            return;
        }

        Array.Resize(ref current, current.Length + 1);
        current[current.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = current;
    }

    private static void SetObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Could not find serialized field '{propertyName}' on {target.name}.");

        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }
}
