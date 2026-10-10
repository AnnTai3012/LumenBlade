using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class PlayerHudSceneSetup
{
    private const string ScenePath = "Assets/Objects/World/Scenes/Level 1.unity";
    private const string PlayerPrefabPath = "Assets/Objects/Player/Player.prefab";
    private const string CompletedPreference = "BTL.PlayerHudSceneSetup.Completed";
    private const string CompletedSessionKey = "BTL.PlayerHudSceneSetup.CompletedThisSession";

    [MenuItem("Tools/BTL/Install Player HUD in LEVEL 1")]
    private static void Run()
    {
        if (EditorPrefs.GetBool(CompletedPreference) || SessionState.GetBool(CompletedSessionKey, false))
            return;

        if (EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += Run;
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            return;

        try
        {
            EnsurePlayerHasStamina();
            CreateOrUpdateHud(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"Could not save scene: {scene.path}");

            AssetDatabase.SaveAssets();
            EditorPrefs.SetBool(CompletedPreference, true);
            SessionState.SetBool(CompletedSessionKey, true);
            Debug.Log("Player stamina and bottom-left health/stamina HUD were added to LEVEL 1.");

            // This editor installer is only needed for the current one-time scene update.
            AssetDatabase.DeleteAsset("Assets/Components/Editor/PlayerHudSceneSetup.cs");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void EnsurePlayerHasStamina()
    {
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            if (prefabContents.GetComponent<PlayerStamina>() == null)
            {
                prefabContents.AddComponent<PlayerStamina>();
                PrefabUtility.SaveAsPrefabAsset(prefabContents, PlayerPrefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private static void CreateOrUpdateHud(Scene scene)
    {
        Canvas canvas = FindSceneCanvas(scene);
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvas = canvasObject.GetComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Transform resourceBarsTransform = canvas.transform.Find("ResourceBars");
        RectTransform resourceBars = resourceBarsTransform as RectTransform;
        if (resourceBars == null)
        {
            GameObject resourceBarsObject = new GameObject("ResourceBars", typeof(RectTransform));
            resourceBarsObject.transform.SetParent(canvas.transform, false);
            resourceBars = resourceBarsObject.GetComponent<RectTransform>();
        }

        SetRect(resourceBars, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24f, 24f), new Vector2(520f, 58f));

        RectTransform healthBar = GetOrCreateBarRoot(canvas.transform, resourceBars, "HealthBar");
        RectTransform staminaBar = GetOrCreateBarRoot(canvas.transform, resourceBars, "StaminaBar");

        Image healthFill = ConfigureBar(healthBar, "HealthBar_Fill", new Color(0.05f, 0.78f, 0.31f, 1f));
        Image staminaFill = ConfigureBar(staminaBar, "StaminaBar_Fill", new Color(0.08f, 0.82f, 0.92f, 1f));
        SetRect(healthBar, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0f, 32f), new Vector2(500f, 20f));
        SetRect(staminaBar, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(0f, 8f), new Vector2(360f, 12f));

        PlayerResourceHud hud = resourceBars.GetComponent<PlayerResourceHud>();
        if (hud == null)
            hud = resourceBars.gameObject.AddComponent<PlayerResourceHud>();

        Health playerHealth = FindComponentInScene<Health>(scene);
        PlayerStamina playerStamina = FindComponentInScene<PlayerStamina>(scene);
        SerializedObject serializedHud = new SerializedObject(hud);
        serializedHud.FindProperty("health").objectReferenceValue = playerHealth;
        serializedHud.FindProperty("stamina").objectReferenceValue = playerStamina;
        serializedHud.FindProperty("healthFill").objectReferenceValue = healthFill;
        serializedHud.FindProperty("staminaFill").objectReferenceValue = staminaFill;
        serializedHud.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(canvas.gameObject);
        EditorUtility.SetDirty(resourceBars.gameObject);
    }

    private static Canvas FindSceneCanvas(Scene scene)
    {
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.gameObject.scene == scene && canvas.renderMode != RenderMode.WorldSpace)
                return canvas;
        }

        return null;
    }

    private static RectTransform GetOrCreateBarRoot(Transform canvasRoot, RectTransform parent, string name)
    {
        RectTransform bar = parent.Find(name) as RectTransform;
        if (bar == null)
        {
            Transform existing = FindNamedTransform(canvasRoot, name);
            bar = existing as RectTransform;
        }

        if (bar == null)
        {
            GameObject barObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            bar = barObject.GetComponent<RectTransform>();
        }

        if (bar.parent != parent)
            bar.SetParent(parent, false);
        bar.name = name;
        return bar;
    }

    private static Image ConfigureBar(RectTransform bar, string fillName, Color fillColor)
    {
        Image background = bar.GetComponent<Image>();
        if (background == null)
            background = bar.gameObject.AddComponent<Image>();
        background.color = new Color(0.025f, 0.065f, 0.09f, 0.92f);
        background.raycastTarget = false;

        Image fill = null;
        Transform existingFill = bar.Find(fillName);
        if (existingFill == null)
            existingFill = bar.Find("Fill");
        if (existingFill != null)
            fill = existingFill.GetComponent<Image>();

        if (fill == null)
        {
            GameObject fillObject = new GameObject(fillName, typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(bar, false);
            fill = fillObject.GetComponent<Image>();
        }

        fill.name = fillName;
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        fill.raycastTarget = false;
        SetRect(fill.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f), Vector2.zero);
        return fill;
    }

    private static Transform FindNamedTransform(Transform root, string targetName)
    {
        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < descendants.Length; i++)
        {
            if (descendants[i] != root && string.Equals(descendants[i].name, targetName, StringComparison.OrdinalIgnoreCase))
                return descendants[i];
        }

        return null;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        T[] components = Resources.FindObjectsOfTypeAll<T>();
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component != null && component.gameObject.scene == scene)
                return component;
        }

        return null;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? Vector2.zero : new Vector2(0.5f, 0.5f);
        if (anchorMin == anchorMax)
        {
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = offsetMin;
        }
        else
        {
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
