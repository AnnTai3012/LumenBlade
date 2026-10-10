using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameplayPauseAudioUiSetup
{
    private const string HudPath = "Assets/Components/UI/Gameplay/Game HUD.prefab";
    private const string ResourceSettingsPath = "Assets/Components/UI/Gameplay/Resources and UI Settings.asset";
    private const string AudioCatalogPath = "Assets/Components/Resources/GameAudioCatalog.asset";
    private const string AtlasPath = "Assets/Components/UI/Gameplay/32x32-Tilesheet.png";
    private static GameResourceSettings resourceSettings;
    private static TMP_FontAsset font;
    private static Sprite whiteSprite;
    private static Sprite sliderTrackSprite;
    private static Sprite sliderHandleSprite;

    [MenuItem("Tools/BTL/Install SFX Category Controls")]
    public static void InstallSfxCategoryControls()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Edit audio settings outside Play Mode.");
        resourceSettings = AssetDatabase.LoadAssetAtPath<GameResourceSettings>(ResourceSettingsPath);
        font = resourceSettings.Font;
        whiteSprite = CreateWhiteSprite();
        sliderTrackSprite = AtlasSprite("32x32-Tilesheet_5");
        sliderHandleSprite = AtlasSprite("32x32-Tilesheet_6");
        GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            RectTransform panel = root.transform.Find("Settings Overlay/Settings Panel") as RectTransform;
            Transform template = panel != null ? panel.Find("Music Volume Cell") : null;
            if (template == null) throw new System.InvalidOperationException("Existing SFX settings row is missing.");
            foreach (string oldName in new[] { "SFX Volume Cell", "UI Sounds Cell" })
            {
                Transform oldRow = panel.Find(oldName);
                if (oldRow != null) Object.DestroyImmediate(oldRow.gameObject);
            }
            panel.sizeDelta = new Vector2(860f, 590f);
            panel.GetComponent<UiSpriteFrame>().Configure(resourceSettings.ItemCellFrame, new Vector4(4f, 4f, 4f, 4f), 3f);
            ((RectTransform)panel.Find("Title")).anchoredPosition = new Vector2(0f, 255f);
            ((RectTransform)panel.Find("Subtitle")).anchoredPosition = new Vector2(0f, 210f);
            panel.Find("Subtitle").GetComponent<TMP_Text>().text = "ÂM THANH — NHẠC VÀ HIỆU ỨNG";
            string[] names = { "Music Volume Cell", "Ambience Cell", "Action Sounds Cell", "Combat Sounds Cell" };
            string[] labels = { "Music Volume", "Ambience", "Action Sounds", "Combat Sounds" };
            Slider[] sliders = new Slider[4];
            TMP_Text[] values = new TMP_Text[3];
            for (int i = 0; i < names.Length; i++)
            {
                RectTransform row = i == 0 ? panel.Find(names[i]) as RectTransform :
                    CloneCell(template, panel, names[i], new Vector2(700f, 68f), Vector2.zero);
                row.sizeDelta = new Vector2(700f, 68f);
                row.anchoredPosition = new Vector2(0f, 145f - i * 78f);
                row.GetComponent<UiSpriteFrame>().Configure(resourceSettings.ItemCellFrame, new Vector4(4f, 4f, 4f, 4f), 2f);
                TMP_Text label = row.Find("Label")?.GetComponent<TMP_Text>();
                if (label == null) label = Text("Label", row, labels[i], new Vector2(-185f, 0f), new Vector2(270f, 42f), 20f, Muted);
                label.text = labels[i];
                label.fontSize = 20f;
                label.rectTransform.anchoredPosition = new Vector2(-185f, 0f);
                label.rectTransform.sizeDelta = new Vector2(270f, 42f);
                label.alignment = TextAlignmentOptions.Left;
                sliders[i] = row.GetComponentInChildren<Slider>(true) ?? BuildSlider("Volume Slider", row, new Vector2(115f, 0f), 270f);
                if (sliders[i] == null) throw new System.InvalidOperationException("Missing volume slider.");
                if (i >= 1) values[i - 1] = Text("Volume Value", row, "100%", new Vector2(295f, 0f), new Vector2(78f, 40f), 19f, Gold);
            }
            Button resume = panel.Find("Resume Game").GetComponent<Button>();
            Button quit = panel.Find("Quit Game").GetComponent<Button>();
            quit.transform.Find("Label").GetComponent<TMP_Text>().text = "MENU";
            resume.GetComponent<UiSpriteFrame>().Configure(resourceSettings.ItemCellFrame, new Vector4(4f, 4f, 4f, 4f), 2f);
            quit.GetComponent<UiSpriteFrame>().Configure(resourceSettings.ItemCellFrame, new Vector4(4f, 4f, 4f, 4f), 2f);
            ((RectTransform)resume.transform).anchoredPosition = new Vector2(-165f, -220f);
            ((RectTransform)quit.transform).anchoredPosition = new Vector2(165f, -220f);
            for (int i = 0; i < sliders.Length; i++)
                sliders[i].navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? sliders[i - 1] : null,
                    selectOnDown = i < sliders.Length - 1 ? sliders[i + 1] : resume };
            resume.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = sliders[3], selectOnRight = quit };
            quit.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = sliders[3], selectOnLeft = resume };
            var serialized = new SerializedObject(root.GetComponent<GameplayPanelController>());
            var sliderRefs = serialized.FindProperty("sfxCategorySliders");
            var valueRefs = serialized.FindProperty("sfxCategoryValues");
            sliderRefs.arraySize = valueRefs.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                sliderRefs.GetArrayElementAtIndex(i).objectReferenceValue = sliders[i + 1];
                valueRefs.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.SaveAsPrefabAsset(root, HudPath) == null)
                throw new System.InvalidOperationException("Could not save SFX settings.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/BTL/Install Pause, Restart and Audio UI")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Install gameplay menus outside Play Mode.");

        resourceSettings = AssetDatabase.LoadAssetAtPath<GameResourceSettings>(ResourceSettingsPath);
        GameObject hudAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
        if (resourceSettings == null || hudAsset == null)
            throw new System.InvalidOperationException("The shared HUD and resource settings must exist before installing pause UI.");

        font = resourceSettings.Font;
        whiteSprite = CreateWhiteSprite();
        sliderTrackSprite = AtlasSprite("32x32-Tilesheet_5");
        sliderHandleSprite = AtlasSprite("32x32-Tilesheet_6");
        EnsureAudioCatalog();
        ConfigurePauseHud(hudAsset);
        InstallSfxCategoryControls();
        AssetDatabase.SaveAssets();
        Debug.Log("Installed reusable Settings and Restart panels, volume sliders, and button audio on the shared HUD.");
    }

    private static void ConfigurePauseHud(GameObject hudAsset)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Transform panelTemplate = root.transform.Find("Store Overlay/Store Panel");
            Transform cellTemplate = root.transform.Find("Store Overlay/Store Panel/Store Items/CryHeal Store Cell");
            if (panelTemplate == null || cellTemplate == null)
                throw new System.InvalidOperationException("Store Panel and CryHeal Store Cell templates could not be found in Game HUD.prefab.");

            GameplayPanelController panels = root.GetComponent<GameplayPanelController>();
            PlayerResourceHud resourceHud = root.GetComponent<PlayerResourceHud>();
            if (panels == null || resourceHud == null)
                throw new System.InvalidOperationException("The shared HUD is missing its gameplay panel or resource HUD controller.");

            ConfigureHealthBar(root.transform, resourceHud);
            RectTransform healthFrame = root.transform.Find("ResourceBars/HealthBar Rim") as RectTransform;
            RectTransform healthRecess = root.transform.Find("ResourceBars/HealthBar Recess") as RectTransform;
            SetObject(resourceHud, "healthFrame", healthFrame);
            SetObject(resourceHud, "healthRecess", healthRecess);

            RectTransform settingsOverlay = BuildOverlay("Settings Overlay", root.transform);
            RectTransform settingsPanel = ClonePanel(panelTemplate, settingsOverlay, "Settings Panel", new Vector2(860f, 560f));
            Text("Title", settingsPanel, "SETTINGS", new Vector2(0f, 220f), new Vector2(600f, 54f), 38f, Gold);
            Text("Subtitle", settingsPanel, "ÂM THANH", new Vector2(0f, 177f), new Vector2(600f, 32f), 20f, Muted);

            RectTransform musicCell = CloneCell(cellTemplate, settingsPanel, "Music Volume Cell", new Vector2(700f, 96f), new Vector2(0f, 78f));
            Text("Label", musicCell, "NHẠC NỀN", new Vector2(-190f, 0f), new Vector2(240f, 40f), 22f, Muted);
            Slider musicSlider = BuildSlider("Music Volume Slider", musicCell, new Vector2(115f, 0f), 270f);
            TMP_Text musicValue = Text("Volume Value", musicCell, "70%", new Vector2(295f, 0f), new Vector2(78f, 40f), 19f, Gold);

            Button resumeButton = BuildButton(cellTemplate, settingsPanel, "Resume Game", "RESUME GAME", new Vector2(-165f, -205f), new Vector2(290f, 72f));
            Button settingsQuitButton = BuildButton(cellTemplate, settingsPanel, "Quit Game", "QUIT GAME", new Vector2(165f, -205f), new Vector2(290f, 72f));

            RectTransform restartOverlay = BuildOverlay("Restart Overlay", root.transform);
            RectTransform restartPanel = ClonePanel(panelTemplate, restartOverlay, "Restart Panel", new Vector2(720f, 360f));
            Text("Title", restartPanel, "GAME OVER", new Vector2(0f, 75f), new Vector2(600f, 76f), 46f, Gold);
            Button restartButton = BuildButton(cellTemplate, restartPanel, "Restart Game", "RESTART", new Vector2(-150f, -78f), new Vector2(260f, 76f));
            Button restartQuitButton = BuildButton(cellTemplate, restartPanel, "Quit Game", "QUIT GAME", new Vector2(150f, -78f), new Vector2(260f, 76f));

            musicSlider.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = resumeButton };
            resumeButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = settingsQuitButton, selectOnUp = musicSlider };
            settingsQuitButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = resumeButton, selectOnUp = musicSlider };
            restartButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = restartQuitButton };
            restartQuitButton.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = restartButton };

            restartOverlay.gameObject.SetActive(false);
            settingsOverlay.gameObject.SetActive(false);

            SetObject(panels, "settingsPanel", settingsOverlay.gameObject);
            SetObject(panels, "restartPanel", restartOverlay.gameObject);
            SetObject(panels, "resumeButton", resumeButton);
            SetObject(panels, "settingsQuitButton", settingsQuitButton);
            SetObject(panels, "restartButton", restartButton);
            SetObject(panels, "restartQuitButton", restartQuitButton);
            SetObject(panels, "musicVolumeSlider", musicSlider);
            SetObject(panels, "musicVolumeValue", musicValue);

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                if (button.GetComponent<UiButtonAudio>() == null)
                    button.gameObject.AddComponent<UiButtonAudio>();

            if (PrefabUtility.SaveAsPrefabAsset(root, HudPath) == null)
                throw new System.InvalidOperationException("Unity could not save the shared HUD prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static RectTransform BuildOverlay(string name, Transform parent)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = GetOrAdd<Image>(rect);
        image.sprite = whiteSprite;
        image.color = new Color(0.015f, 0.022f, 0.031f, 0.87f);
        image.raycastTarget = true;
        return rect;
    }

    private static RectTransform ClonePanel(Transform template, Transform parent, string name, Vector2 size)
    {
        Transform existing = parent.Find(name);
        GameObject copy = existing != null ? existing.gameObject : Object.Instantiate(template.gameObject, parent, false);
        if (existing == null)
        {
            copy.name = name;
            ClearChildren(copy.transform);
        }
        RectTransform rect = (RectTransform)copy.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        UiSpriteFrame frame = copy.GetComponent<UiSpriteFrame>();
        if (frame != null) frame.raycastTarget = false;
        return rect;
    }

    private static RectTransform CloneCell(Transform template, Transform parent, string name, Vector2 size, Vector2 position)
    {
        Transform existing = parent.Find(name);
        GameObject copy = existing != null ? existing.gameObject : Object.Instantiate(template.gameObject, parent, false);
        if (existing == null)
        {
            copy.name = name;
            ClearChildren(copy.transform);
        }
        LayoutElement layout = copy.GetComponent<LayoutElement>();
        if (layout != null) Object.DestroyImmediate(layout);
        RectTransform rect = (RectTransform)copy.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        UiSpriteFrame frame = copy.GetComponent<UiSpriteFrame>();
        if (frame != null)
        {
            frame.Configure(resourceSettings.ShopCellFrame, new Vector4(4f, 4f, 4f, 12f), 3f);
            frame.raycastTarget = false;
        }
        return rect;
    }

    private static Button BuildButton(Transform cellTemplate, Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        RectTransform rect = CloneCell(cellTemplate, parent, name, size, position);
        UiSpriteFrame frame = rect.GetComponent<UiSpriteFrame>();
        frame.raycastTarget = true;
        Button button = GetOrAdd<Button>(rect);
        ConfigureButton(button, frame);
        TMP_Text text = Text("Label", rect, label, Vector2.zero, size - new Vector2(18f, 8f), 21f, Gold);
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static Slider BuildSlider(string name, Transform parent, Vector2 position, float width)
    {
        RectTransform root = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(width, 40f));
        Image hitArea = GetOrAdd<Image>(root);
        hitArea.sprite = whiteSprite;
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;
        Slider slider = GetOrAdd<Slider>(root);
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform trackRect = CreateRect("Track", root, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0f, 21f));
        Image track = GetOrAdd<Image>(trackRect);
        track.sprite = sliderTrackSprite;
        track.color = Color.white;
        track.raycastTarget = false;

        // Slider stretches its handle vertically across this area when updating its value.
        RectTransform handleArea = CreateRect("Handle Slide Area", root, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-54f, 12f));
        RectTransform handleRect = CreateRect("Handle", handleArea, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(54f, 0f));
        Image handle = GetOrAdd<Image>(handleRect);
        handle.sprite = sliderHandleSprite;
        handle.color = Color.white;
        handle.raycastTarget = false;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle;
        slider.SetValueWithoutNotify(0.8f);
        return slider;
    }

    private static TMP_Text Text(string name, Transform parent, string value, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        TMP_Text text = GetOrAdd<TextMeshProUGUI>(rect);
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = parent.Find(name) as RectTransform;
        if (rect == null)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = LayerMask.NameToLayer("UI");
            rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
        }
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void ConfigureButton(Button button, Graphic graphic)
    {
        button.targetGraphic = graphic;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.91f, 0.63f);
        colors.selectedColor = new Color(1f, 0.87f, 0.51f);
        colors.pressedColor = new Color(0.68f, 0.57f, 0.35f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }

    private static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.GetChild(i).gameObject);
    }

    private static void SetObject(Object target, string propertyName, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new System.InvalidOperationException($"Serialized field '{propertyName}' was not found on '{target.name}'.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Sprite CreateWhiteSprite()
    {
        const string path = "Assets/Components/UI/Gameplay/HUD Fill.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new System.InvalidOperationException("HUD Fill.png is missing from the shared UI assets.");
        return sprite;
    }

    private static Sprite AtlasSprite(string name)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AtlasPath))
            if (asset is Sprite sprite && sprite.name == name) return sprite;
        throw new System.InvalidOperationException($"Required UI sprite '{name}' was not found in {AtlasPath}.");
    }

    private static T GetOrAdd<T>(Transform target) where T : Component => target.GetComponent<T>() ?? target.gameObject.AddComponent<T>();

    private static void ConfigureHealthBar(Transform root, PlayerResourceHud hud)
    {
        Transform bars = root.Find("ResourceBars");
        if (bars == null) throw new System.InvalidOperationException("The shared HUD has no ResourceBars.");
        RectTransform rim = CreateRect("HealthBar Rim", bars, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(84f, 20f), new Vector2(352f, 68f));
        HudShapeGraphic oldRim = rim.GetComponent<HudShapeGraphic>();
        if (oldRim != null) Object.DestroyImmediate(oldRim);
        Image frame = GetOrAdd<Image>(rim);
        frame.sprite = AtlasSprite("32x32-Tilesheet_11");
        frame.color = Color.white;
        frame.type = Image.Type.Simple;
        frame.raycastTarget = false;
        Transform recess = bars.Find("HealthBar Recess");
        if (recess != null) recess.gameObject.SetActive(false);
        ConfigureFill(bars, "HealthBar_Fill", AtlasSprite("32x32-Tilesheet_14"), new Vector2(92f, 28f), new Vector2(336f, 24f));
        ConfigureFill(bars, "StaminaBar_Fill", AtlasSprite("32x32-Tilesheet_12"), new Vector2(92f, 13f), new Vector2(285f, 12f));
        RectTransform shield = bars.Find("ShieldBar_Fill") as RectTransform;
        if (shield != null)
        {
            shield.anchoredPosition = new Vector2(92f, 57f);
            shield.sizeDelta = new Vector2(336f, 5f);
        }
        TMP_Text hpValue = bars.Find("Health Value").GetComponent<TMP_Text>();
        hpValue.rectTransform.anchoredPosition = new Vector2(362f, 73f);
        hpValue.rectTransform.sizeDelta = new Vector2(164f, 22f);
        hpValue.fontSize = 17f;
        TMP_Text shieldValue = bars.Find("Shield Value").GetComponent<TMP_Text>();
        shieldValue.rectTransform.anchoredPosition = new Vector2(365f, 54f);
        shieldValue.rectTransform.sizeDelta = new Vector2(176f, 18f);
        shieldValue.fontSize = 14f;
        SetObject(hud, "healthFrame", rim);
        SetObject(hud, "healthRecess", recess);
    }

    private static void ConfigureFill(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero, position, size);
        Image image = GetOrAdd<Image>(rect);
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = 0;
        image.fillAmount = 1f;
        image.raycastTarget = false;
    }

    private static void EnsureAudioCatalog()
    {
        if (AssetDatabase.LoadAssetAtPath<GameAudioCatalog>(AudioCatalogPath) != null)
            return;

        Directory.CreateDirectory("Assets/Components/Resources");
        GameAudioCatalog catalog = ScriptableObject.CreateInstance<GameAudioCatalog>();
        catalog.name = "Game Audio Catalog";
        AssetDatabase.CreateAsset(catalog, AudioCatalogPath);
    }

    private static Color Gold => new Color(0.94f, 0.76f, 0.43f);
    private static Color Muted => new Color(0.83f, 0.83f, 0.76f);
}
