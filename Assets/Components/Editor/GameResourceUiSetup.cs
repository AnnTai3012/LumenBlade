using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

// Author the real prefab and scene references through Unity, preserving existing map content.
public static class GameResourceUiSetup
{
    public const string SettingsPath = "Assets/Components/UI/Gameplay/Resources and UI Settings.asset";
    public const string HudPath = "Assets/Components/UI/Gameplay/Game HUD.prefab";
    private const string PlayerPath = "Assets/Objects/Player/Player.prefab";
    private static readonly string[] ScenePaths =
    {
        "Assets/Objects/World/Scenes/Level 1.unity", "Assets/Objects/World/Scenes/Level 2.unity", "Assets/Objects/World/Scenes/Purchasing LevelsC1.unity"
    };
    private static readonly Color Gold = new Color(0.94f, 0.76f, 0.43f);
    private static readonly Color Ink = new Color(0.10f, 0.14f, 0.16f);
    private static readonly Color PaperInk = new Color(0.22f, 0.16f, 0.10f);
    private static GameResourceSettings settings;
    private static TMP_FontAsset font;
    private static Sprite whiteSprite;

    [MenuItem("Tools/BTL/Install Resources, HUD and Store")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Install the UI outside Play Mode.");
        settings = CreateSettings();
        font = settings.Font;
        whiteSprite = CreateSolidFillSprite();
        GameObject hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
        if (hud == null) hud = BuildHudPrefab();
        else RepairHudGraphics();
        if (settings.Blessings.Length < 4 || new SerializedObject(hud.GetComponent<GameplayPanelController>()).FindProperty("blessingButtons").arraySize < 4)
        {
            UpgradeBlessingsToFour();
            hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
        }
        UpdatePlayerPrefab();
        UpdateEnemyPrefabs();
        UpdateStatuePrefab();
        UpdateDoorPrefab();
        string previousActivePath = SceneManager.GetActiveScene().path;
        foreach (string path in ScenePaths)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            InstallInScene(scene, hud, path == ScenePaths[2]);
            if (path == ScenePaths[2]) EnsurePurchasingGround(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save " + path);
        }
        if (!string.IsNullOrEmpty(previousActivePath) && SceneManager.GetActiveScene().path != previousActivePath)
            EditorSceneManager.OpenScene(previousActivePath, OpenSceneMode.Single);
        var buildScenes = EditorBuildSettings.scenes.ToList();
        foreach (string path in ScenePaths)
        {
            var entry = buildScenes.FirstOrDefault(s => s.path == path);
            if (entry == null) buildScenes.Add(new EditorBuildSettingsScene(path, true));
            else entry.enabled = true;
        }
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("BTL resource UI installed: Level 1 → Level 2 → Purchasing Levels.");
    }

    public static void RepairHudGraphics()
    {
        Sprite fillSprite = CreateSolidFillSprite();
        GameResourceSettings config = AssetDatabase.LoadAssetAtPath<GameResourceSettings>(SettingsPath);
        ConfigurePixelArtTextures();
        GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            bool changed = false;
            foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.MaskableGraphic>(true))
            {
                if ((graphic is UiSpriteFrame || graphic is HudShapeGraphic) && graphic.GetComponent<CanvasRenderer>() == null)
                {
                    graphic.gameObject.AddComponent<CanvasRenderer>();
                    changed = true;
                }
            }
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (image.name.EndsWith("_Fill", StringComparison.Ordinal) && image.sprite != fillSprite)
                {
                    image.sprite = fillSprite;
                    changed = true;
                }
            foreach (ConsumableKind kind in Enum.GetValues(typeof(ConsumableKind)))
            {
                Transform cell = root.transform.Find("Store Overlay/Store Panel/Store Items/" + kind + " Store Cell");
                if (cell == null) continue;
                UiSpriteFrame frame = cell.GetComponent<UiSpriteFrame>();
                frame.Configure(config.ShopCellFrame, new Vector4(4f, 4f, 4f, 12f), 3f);
                ((RectTransform)cell.Find("Item Title")).anchoredPosition = new Vector2(135, 272);
                ((RectTransform)cell.Find("Item Icon")).anchoredPosition = new Vector2(135, 205);
                ((RectTransform)cell.Find("Item Icon")).sizeDelta = new Vector2(56, 88);
                changed = true;
            }
            SetArray(root.GetComponent<GameplayPanelController>(), "storeEffects",
                Enum.GetValues(typeof(ConsumableKind)).Cast<ConsumableKind>()
                    .Select(kind => root.transform.Find("Store Overlay/Store Panel/Store Items/" + kind + " Store Cell/Effect"))
                    .Where(effect => effect != null).Select(effect => effect.GetComponent<TMP_Text>()).ToArray());
            changed = true;
            if (changed) PrefabUtility.SaveAsPrefabAsset(root, HudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("Tools/BTL/Upgrade Blessings to Four Choices")]
    public static void UpgradeBlessingsToFour()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Upgrade blessings outside Play Mode.");
        settings = AssetDatabase.LoadAssetAtPath<GameResourceSettings>(SettingsPath);
        var config = new SerializedObject(settings);
        var choices = config.FindProperty("blessings");
        choices.arraySize = 4;
        var bundle = choices.GetArrayElementAtIndex(3);
        bundle.FindPropertyRelative("title").stringValue = "Vật phẩm";
        bundle.FindPropertyRelative("description").stringValue = "Nhận 1 CryHeal\nvà 1 CryShield.\nNhấn 1 / 2 để dùng.";
        bundle.FindPropertyRelative("reward").intValue = (int)BlessingReward.ConsumableBundle;
        var damage = choices.GetArrayElementAtIndex(2);
        damage.FindPropertyRelative("title").stringValue = "Sức mạnh";
        damage.FindPropertyRelative("description").stringValue = "+{damagePercent}% sát thương mọi đòn đánh.\nTối đa +{damageCap}%.";
        damage.FindPropertyRelative("reward").intValue = (int)BlessingReward.AllDamage;
        var damageIcon = config.FindProperty("damageBlessingIcon");
        if (damageIcon.objectReferenceValue == null)
            damageIcon.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath).GetComponent<SpriteRenderer>().sprite;
        damage.FindPropertyRelative("icon").objectReferenceValue = damageIcon.objectReferenceValue;
        config.ApplyModifiedPropertiesWithoutUndo();
        font = settings.Font;

        GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            GameplayPanelController panels = root.GetComponent<GameplayPanelController>();
            Transform content = root.transform.Find("Blessing Overlay/Blessing Content");
            RectTransform contentRect = (RectTransform)content;
            contentRect.sizeDelta = new Vector2(1160, 550);
            Transform row = content.Find("Three Choices") ?? content.Find("Four Choices");
            row.name = "Four Choices";
            ((RectTransform)row).sizeDelta = new Vector2(1120, 382);
            row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().spacing = 20f;
            PositionText(content.Find("Blessing Title"), new Vector2(580, 515), new Vector2(900, 52), 40);
            PositionText(content.Find("Blessing Subtitle"), new Vector2(580, 463), new Vector2(960, 36), 23);
            PositionText(content.Find("Blessing Hint"), new Vector2(580, 25), new Vector2(960, 30), 17);
            content.Find("Blessing Hint").GetComponent<TMP_Text>().text = "NHẤN 1 / 2 / 3 / 4 HOẶC CHỌN BẰNG CHUỘT";

            var buttons = new List<UnityEngine.UI.Button>();
            var titles = new List<TMP_Text>();
            var descriptions = new List<TMP_Text>();
            var icons = new List<UnityEngine.UI.Image>();
            var secondaryIcons = new List<UnityEngine.UI.Image>();
            for (int i = 0; i < 4; i++)
            {
                Transform card = row.Find("Blessing Choice " + (i + 1));
                if (card == null)
                {
                    card = UnityEngine.Object.Instantiate(row.GetChild(0).gameObject, row, false).transform;
                    card.name = "Blessing Choice " + (i + 1);
                }
                card.SetSiblingIndex(i);
                var layout = card.GetComponent<UnityEngine.UI.LayoutElement>();
                layout.preferredWidth = 264;
                layout.preferredHeight = 382;
                PositionText(card.Find("Number"), new Vector2(132, 350), new Vector2(226, 30), 18);
                card.Find("Number").GetComponent<TMP_Text>().text = "0" + (i + 1);
                PositionText(card.Find("Blessing Title"), new Vector2(132, 187), new Vector2(234, 58), 26);
                PositionText(card.Find("Blessing Description"), new Vector2(132, 118), new Vector2(226, 98), 20);
                TMP_Text title = card.Find("Blessing Title").GetComponent<TMP_Text>();
                TMP_Text description = card.Find("Blessing Description").GetComponent<TMP_Text>();
                title.text = settings.Blessings[i].title;
                description.text = settings.Blessings[i].description
                    .Replace("{damagePercent}", (settings.DamageBonusPerBlessing * 100f).ToString("0.#"))
                    .Replace("{damageCap}", (settings.MaxDamageBonus * 100f).ToString("0.#"));
                var primary = card.Find("Blessing Icon").GetComponent<UnityEngine.UI.Image>();
                bool isBundle = settings.Blessings[i].reward == BlessingReward.ConsumableBundle;
                primary.rectTransform.anchoredPosition = new Vector2(isBundle ? 94 : 132, 269);
                primary.rectTransform.sizeDelta = isBundle ? new Vector2(48, 88) : new Vector2(76, 96);
                primary.sprite = settings.Blessings[i].icon != null ? settings.Blessings[i].icon :
                    settings.Blessings[i].reward == BlessingReward.StaminaRecovery ? settings.SpiritStoneIcon :
                    settings.Blessings[i].reward == BlessingReward.AllDamage ? settings.DamageBlessingIcon : settings.Icon(ConsumableKind.CryHeal);
                Transform secondary = card.Find("Secondary Blessing Icon");
                if (secondary == null)
                    secondary = Icon("Secondary Blessing Icon", card, settings.Icon(ConsumableKind.CryShield), new Vector2(170, 269), new Vector2(48, 88)).transform;
                secondary.gameObject.SetActive(isBundle);
                var secondaryImage = secondary.GetComponent<UnityEngine.UI.Image>();
                secondaryImage.sprite = settings.Icon(ConsumableKind.CryShield);
                secondaryImage.rectTransform.anchoredPosition = new Vector2(170, 269);
                secondaryImage.rectTransform.sizeDelta = new Vector2(48, 88);
                var button = card.GetComponentInChildren<UnityEngine.UI.Button>(true);
                button.name = "Choose Blessing " + (i + 1);
                var buttonRect = (RectTransform)button.transform;
                buttonRect.anchoredPosition = new Vector2(132, 40);
                buttonRect.sizeDelta = new Vector2(218, 46);
                PositionText(button.transform.Find("Label"), new Vector2(109, 23), new Vector2(206, 34), 21);
                buttons.Add(button); titles.Add(title); descriptions.Add(description);
                icons.Add(primary); secondaryIcons.Add(secondaryImage);
            }
            SetArray(panels, "blessingButtons", buttons);
            SetArray(panels, "blessingTitles", titles);
            SetArray(panels, "blessingDescriptions", descriptions);
            SetArray(panels, "blessingIcons", icons);
            SetArray(panels, "blessingSecondaryIcons", secondaryIcons);
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)row);
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    private static void PositionText(Transform text, Vector2 position, Vector2 size, float fontSize)
    {
        var rect = (RectTransform)text;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        text.GetComponent<TMP_Text>().fontSize = fontSize;
    }

    public static void FixPurchasingGround()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePaths[2]);
        if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePaths[2], OpenSceneMode.Single);
        EnsurePurchasingGround(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsurePurchasingGround(Scene scene)
    {
        foreach (var tilemap in Find<UnityEngine.Tilemaps.Tilemap>(scene))
        {
            var collider = Ensure<UnityEngine.Tilemaps.TilemapCollider2D>(tilemap.gameObject);
            collider.isTrigger = false;
            collider.ProcessTilemapChanges();
        }
    }

    public static void UpdateDoorPrefab()
    {
        const string path = "Assets/Objects/Items/EntranceForward.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            LevelExitDoor door = root.GetComponent<LevelExitDoor>();
            SetString(door, "targetSceneName", "Level 2");
            SetBool(door, "requireKey", true);
            SetBool(door, "offerBlessingOnArrival", true);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static Sprite CreateSolidFillSprite()
    {
        const string path = "Assets/Components/UI/Gameplay/HUD Fill.png";
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;
        var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white); pixel.Apply();
        System.IO.File.WriteAllBytes(path, pixel.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(pixel);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void ConfigurePixelArtTextures()
    {
        foreach (string path in new[] { "Assets/Components/UI/Gameplay/32x32-Tilesheet.png", "Assets/Objects/Items/red_crystal_Healing.png",
                     "Assets/Objects/Items/blue_crystal_Shleid.png", "Assets/Objects/Items/SpiritStone.gif" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.filterMode == FilterMode.Point && importer.textureCompression == TextureImporterCompression.Uncompressed && !importer.mipmapEnabled) continue;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private static GameResourceSettings CreateSettings()
    {
        var config = AssetDatabase.LoadAssetAtPath<GameResourceSettings>(SettingsPath);
        if (config != null) return config;
        config = ScriptableObject.CreateInstance<GameResourceSettings>();
        AssetDatabase.CreateAsset(config, SettingsPath);
        Set(config, "cryHealIcon", FirstSprite("Assets/Objects/Items/red_crystal_Healing.png"));
        Set(config, "cryShieldIcon", FirstSprite("Assets/Objects/Items/blue_crystal_Shleid.png"));
        Set(config, "spiritStoneIcon", FirstSprite("Assets/Objects/Items/SpiritStone.gif"));
        Set(config, "keyIcon", PrefabSprite("Assets/Objects/Items/KeyPickup.prefab"));
        Set(config, "damageBlessingIcon", AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath).GetComponent<SpriteRenderer>().sprite);
        Set(config, "itemCellFrame", PrefabSprite("Assets/Components/UI/Gameplay/Cell_item.prefab"));
        Set(config, "shopCellFrame", PrefabSprite("Assets/Components/UI/Gameplay/CellinShop.prefab"));
        Set(config, "shopPanelFrame", PrefabSprite("Assets/Components/UI/Gameplay/Pannel Shop.prefab"));
        Set(config, "blessingPanelFrame", PrefabSprite("Assets/Components/UI/Gameplay/Upgrade Board.prefab"));
        Set(config, "font", CreateFont());
        Set(config, "healEffectPrefab", CreateEffectPrefab("Heal Effect", "Assets/Objects/Player/Ui/Healing1.png",
            "Assets/Objects/Player/Ui/AnimHeal/Healing1_0.controller", 1.4f));
        Set(config, "shieldEffectPrefab", CreateEffectPrefab("Shield Effect", "Assets/Objects/Player/Ui/Shield.png",
            "Assets/Objects/Player/Ui/AnimShleid/Shield Round Hex Scan Loop 128x128 BW_0.controller", 1.6f));
        CreatePickupPrefab(ConsumableKind.CryHeal, config);
        CreatePickupPrefab(ConsumableKind.CryShield, config);
        EditorUtility.SetDirty(config);
        return config;
    }

    private static TMP_FontAsset CreateFont()
    {
        const string path = "Assets/Components/UI/Gameplay/Game UI Font.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null) return existing;
        Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
        if (source == null) throw new InvalidOperationException("TMP Essential Resources must be imported first.");
        var created = TMP_FontAsset.CreateFontAsset(source, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        created.name = "Game UI Font";
        created.isMultiAtlasTexturesEnabled = true;
        AssetDatabase.CreateAsset(created, path);
        created.material.name = "Game UI Font Material";
        created.material.hideFlags = HideFlags.None;
        AssetDatabase.AddObjectToAsset(created.material, created);
        foreach (Texture2D texture in created.atlasTextures)
        {
            texture.hideFlags = HideFlags.None;
            AssetDatabase.AddObjectToAsset(texture, created);
        }
        // Prewarm all Vietnamese letters, numbers and punctuation used by these panels.
        created.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 /%+·:().,–×" +
            "ÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ" +
            "àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ", out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("UI font missing characters: " + missing);
        EditorUtility.SetDirty(created);
        return created;
    }

    private static GameObject CreateEffectPrefab(string name, string texturePath, string controllerPath, float scale)
    {
        string path = "Assets/Objects/Player/Ui/" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;
        var root = new GameObject(name, typeof(SpriteRenderer), typeof(Animator));
        root.transform.localPosition = Vector3.zero;
        root.transform.localScale = Vector3.one * scale;
        root.GetComponent<SpriteRenderer>().sprite = FirstSprite(texturePath);
        root.GetComponent<SpriteRenderer>().sortingOrder = 30;
        root.GetComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return saved;
    }

    private static void CreatePickupPrefab(ConsumableKind kind, GameResourceSettings config)
    {
        string path = "Assets/Objects/Items/" + kind + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
        var root = new GameObject(kind.ToString(), typeof(SpriteRenderer), typeof(Animator), typeof(CircleCollider2D), typeof(ConsumablePickup));
        root.GetComponent<SpriteRenderer>().sprite = config.Icon(kind);
        root.GetComponent<SpriteRenderer>().sortingOrder = 10;
        root.GetComponent<CircleCollider2D>().isTrigger = true;
        root.GetComponent<CircleCollider2D>().radius = 0.3f;
        root.GetComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            kind == ConsumableKind.CryHeal ? "Assets/Objects/Items/Anim CryHeal/red_crystal_0000-sheet_0.controller" : "Assets/Objects/Items/Anim Shield/blue_crystal_0000-sheet_0.controller");
        SetInt(root.GetComponent<ConsumablePickup>(), "kind", (int)kind);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    private static void UpdatePlayerPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPath);
        try
        {
            Ensure<PlayerStamina>(root);
            var items = Ensure<PlayerConsumables>(root);
            Set(items, "settings", settings);
            Set(root.GetComponent<PlayerController>(), "consumables", items);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void UpdateEnemyPrefabs()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Objects/Enemies", "Assets/Objects/Bosses" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source.GetComponentInChildren<MonsterHealth>(true) == null) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (MonsterHealth monster in root.GetComponentsInChildren<MonsterHealth>(true))
                    Ensure<SpiritStoneRewardOnDeath>(monster.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    private static void UpdateStatuePrefab()
    {
        const string path = "Assets/Components/UI/Gameplay/Bless Statue( store).prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            BoxCollider2D collider = Ensure<BoxCollider2D>(root);
            Sprite sprite = root.GetComponent<SpriteRenderer>().sprite;
            collider.size = sprite.bounds.size;
            collider.offset = sprite.bounds.center;
            collider.isTrigger = true;
            Ensure<StoreStatueInteractable>(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static GameObject BuildHudPrefab()
    {
        RectTransform canvasRect = Rect("Game HUD", null, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        Canvas canvas = canvasRect.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        canvas.sortingOrder = 100;
        var scaler = canvasRect.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        PlayerResourceHud hud = canvasRect.gameObject.AddComponent<PlayerResourceHud>();
        GameplayPanelController panels = canvasRect.gameObject.AddComponent<GameplayPanelController>();
        Set(panels, "settings", settings);
        BuildResourceHud(canvasRect, hud);
        BuildStore(canvasRect, panels);
        BuildBlessings(canvasRect, panels);
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(canvasRect.gameObject, HudPath);
        UnityEngine.Object.DestroyImmediate(canvasRect.gameObject);
        return saved;
    }

    private static void BuildResourceHud(RectTransform canvas, PlayerResourceHud hud)
    {
        RectTransform corner = Rect("ResourceBars", canvas, Vector2.zero, Vector2.zero, new Vector2(32, 28), new Vector2(480, 205));
        Shape("HealthBar Rim", corner, new Vector2(78, 26), new Vector2(392, 36), new Color(0.18f, 0.37f, 0.38f), HudShape.BevelBar);
        Shape("HealthBar Recess", corner, new Vector2(82, 30), new Vector2(380, 28), new Color(0.035f, 0.045f, 0.06f), HudShape.BevelBar);
        UnityEngine.UI.Image hp = Fill("HealthBar_Fill", corner, new Vector2(89, 35), new Vector2(356, 18), new Color(0.75f, 0.08f, 0.11f));
        UnityEngine.UI.Image sp = Fill("StaminaBar_Fill", corner, new Vector2(89, 17), new Vector2(285, 7), new Color(0.25f, 0.76f, 0.81f));
        UnityEngine.UI.Image shield = Fill("ShieldBar_Fill", corner, new Vector2(89, 57), new Vector2(356, 5), new Color(0.45f, 0.86f, 1f));
        shield.gameObject.SetActive(false);
        TMP_Text hpValue = Text("Health Value", corner, "100 / 100", new Vector2(260, 44), new Vector2(230, 28), 19, Color.white);
        TMP_Text shieldValue = Text("Shield Value", corner, "", new Vector2(373, 75), new Vector2(190, 26), 17, new Color(0.54f, 0.88f, 1f));
        Shape("Portrait Rim", corner, new Vector2(5, 18), new Vector2(87, 66), Gold, HudShape.Seal);
        Shape("Portrait Inset", corner, new Vector2(10, 23), new Vector2(77, 56), Ink, HudShape.Seal);
        Sprite portrait = PrefabSprite(PlayerPath);
        Icon("Portrait", corner, portrait, new Vector2(48, 54), new Vector2(52, 56));
        Text("HP Label", corner, "HP", new Vector2(48, 28), new Vector2(48, 16), 14, Gold);

        var itemButtons = new List<UnityEngine.UI.Button>();
        var slots = new List<RectTransform>();
        var counts = new List<TMP_Text>();
        for (int i = 0; i < 2; i++)
        {
            ConsumableKind kind = (ConsumableKind)i;
            RectTransform cell = Rect(kind + " Cell", corner, Vector2.zero, Vector2.zero, new Vector2(89 + i * 94, 96), new Vector2(88, 88));
            UiSpriteFrame frame = Frame(cell, settings.ItemCellFrame, 4, 3f);
            frame.raycastTarget = true;
            UnityEngine.UI.Button button = cell.gameObject.AddComponent<UnityEngine.UI.Button>();
            ConfigureButton(button, frame);
            Icon("Item Icon", cell, settings.Icon(kind), new Vector2(44, 48), new Vector2(41, 61));
            Text("Hotkey", cell, (i + 1).ToString(), new Vector2(15, 16), new Vector2(22, 22), 19, Gold);
            TMP_Text count = Text("Count", cell, "0/5", new Vector2(62, 15), new Vector2(44, 24), 18, Color.white);
            itemButtons.Add(button); slots.Add(cell); counts.Add(count);
        }
        RectTransform marker = Rect("Selected Item Marker", slots[0], new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, 16), new Vector2(30, 34));
        var markerGraphic = marker.gameObject.AddComponent<HudShapeGraphic>();
        markerGraphic.SetShape(HudShape.SelectionMarker); markerGraphic.raycastTarget = false;
        TMP_Text feedback = Text("Item Feedback", corner, "", new Vector2(238, 87), new Vector2(340, 26), 17, Gold);

        RectTransform wallet = Rect("Wallet and Key", canvas, Vector2.one, Vector2.one, new Vector2(-36, -28), new Vector2(232, 115));
        Shape("Wallet Background", wallet, Vector2.zero, new Vector2(232, 115), new Color(0.035f, 0.06f, 0.08f, 0.85f), HudShape.BevelBar);
        Icon("SpiritStone Icon", wallet, settings.SpiritStoneIcon, new Vector2(36, 82), new Vector2(36, 36));
        Text("SpiritStone Label", wallet, "SPIRITSTONE", new Vector2(142, 98), new Vector2(152, 22), 15, Gold);
        TMP_Text stones = Text("SpiritStone Value", wallet, "0", new Vector2(143, 72), new Vector2(150, 34), 29, Color.white);
        UnityEngine.UI.Image key = Icon("Key Icon", wallet, settings.KeyIcon, new Vector2(35, 30), new Vector2(32, 32));
        Text("Key Label", wallet, "CHÌA KHÓA", new Vector2(134, 40), new Vector2(155, 22), 14, Gold);
        TMP_Text keyCount = Text("Key Value", wallet, "0 / 1", new Vector2(133, 20), new Vector2(155, 25), 20, Color.white);

        Set(hud, "healthFill", hp); Set(hud, "staminaFill", sp); Set(hud, "shieldFill", shield);
        Set(hud, "healthValue", hpValue); Set(hud, "shieldValue", shieldValue); Set(hud, "itemFeedback", feedback);
        Set(hud, "spiritStoneValue", stones); Set(hud, "keyValue", keyCount); Set(hud, "keyIcon", key);
        Set(hud, "selectionMarker", marker); SetArray(hud, "itemButtons", itemButtons); SetArray(hud, "itemSlots", slots); SetArray(hud, "itemCounts", counts);
    }

    private static void BuildStore(RectTransform canvas, GameplayPanelController panels)
    {
        RectTransform overlay = Overlay("Store Overlay", canvas);
        RectTransform panel = Rect("Store Panel", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 590));
        Frame(panel, settings.ShopPanelFrame, 22, 3f);
        Text("Store Title", panel, "BỆ ĐÁ CỔ", new Vector2(430, 528), new Vector2(550, 55), 36, Gold);
        Text("Store Subtitle", panel, "Đổi SpiritStone lấy vật phẩm tiêu hao", new Vector2(430, 480), new Vector2(600, 34), 20, new Color(0.83f, 0.83f, 0.76f));
        Icon("Wallet Icon", panel, settings.SpiritStoneIcon, new Vector2(668, 525), new Vector2(30, 30));
        TMP_Text wallet = Text("Store Wallet", panel, "0", new Vector2(736, 525), new Vector2(100, 34), 27, Color.white);
        UnityEngine.UI.Button close = Button("Close Store", panel, "×", new Vector2(786, 537), new Vector2(40, 40), 28);
        RectTransform row = Rect("Store Items", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -16), new Vector2(626, 330));
        LayoutRow(row, 34);
        var buttons = new List<UnityEngine.UI.Button>(); var counts = new List<TMP_Text>(); var prices = new List<TMP_Text>(); var effects = new List<TMP_Text>();
        for (int i = 0; i < 2; i++)
        {
            ConsumableKind kind = (ConsumableKind)i;
            RectTransform card = LayoutChild(kind + " Store Cell", row, new Vector2(270, 330));
            UiSpriteFrame cellFrame = Frame(card, settings.ShopCellFrame, 4, 3f);
            cellFrame.Configure(settings.ShopCellFrame, new Vector4(4,4,4,12),3f);
            Text("Item Title", card, i == 0 ? "ĐÁ HỒI MÁU" : "ĐÁ KHIÊN", new Vector2(135, 272), new Vector2(245, 34), 23, Gold);
            Icon("Item Icon", card, settings.Icon(kind), new Vector2(135, 205), new Vector2(56, 88));
            effects.Add(Text("Effect", card, i == 0 ? "Hồi 30% HP tối đa" : "Khiên 25% HP · 8 giây", new Vector2(135, 145), new Vector2(244, 36), 19, Color.white));
            counts.Add(Text("Owned Count", card, "Đang có 0 / 5", new Vector2(135, 113), new Vector2(240, 27), 18, new Color(0.72f, 0.75f, 0.73f)));
            prices.Add(Text("Price", card, settings.Price(kind) + " SpiritStone", new Vector2(135, 82), new Vector2(240, 27), 21, Gold));
            buttons.Add(Button("Buy " + kind, card, "MUA", new Vector2(135, 36), new Vector2(204, 44), 20));
        }
        TMP_Text feedback = Text("Store Feedback", panel, "", new Vector2(430, 78), new Vector2(680, 30), 20, Gold);
        Text("Close Hint", panel, "ESC  ·  ĐÓNG CỬA HÀNG", new Vector2(430, 43), new Vector2(600, 26), 17, new Color(0.66f, 0.72f, 0.71f));
        Set(panels, "storePanel", overlay.gameObject); Set(panels, "closeStoreButton", close);
        Set(panels, "storeWallet", wallet); Set(panels, "storeFeedback", feedback);
        SetArray(panels, "purchaseButtons", buttons); SetArray(panels, "storeCounts", counts); SetArray(panels, "storePrices", prices);
        SetArray(panels, "storeEffects", effects);
        overlay.gameObject.SetActive(false);
    }

    private static void BuildBlessings(RectTransform canvas, GameplayPanelController panels)
    {
        RectTransform overlay = Overlay("Blessing Overlay", canvas);
        RectTransform content = Rect("Blessing Content", overlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1160, 550));
        Text("Blessing Title", content, "CHÚC PHÚC", new Vector2(580, 515), new Vector2(900, 52), 40, Gold);
        Text("Blessing Subtitle", content, "Chọn một chúc phúc để bắt đầu màn", new Vector2(580, 463), new Vector2(960, 36), 23, new Color(0.88f, 0.87f, 0.81f));
        RectTransform row = Rect("Four Choices", content, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -34), new Vector2(1120, 382));
        LayoutRow(row, 20);
        var buttons = new List<UnityEngine.UI.Button>(); var titles = new List<TMP_Text>();
        var descriptions = new List<TMP_Text>(); var icons = new List<UnityEngine.UI.Image>();
        var secondaryIcons = new List<UnityEngine.UI.Image>();
        for (int i = 0; i < 4; i++)
        {
            RectTransform card = LayoutChild("Blessing Choice " + (i + 1), row, new Vector2(264, 382));
            Frame(card, settings.BlessingPanelFrame, 8, 3f);
            Text("Number", card, "0" + (i + 1), new Vector2(132, 350), new Vector2(226, 30), 18, PaperInk);
            bool isBundle = settings.Blessings[i].reward == BlessingReward.ConsumableBundle;
            Sprite icon = settings.Blessings[i].icon != null ? settings.Blessings[i].icon :
                settings.Blessings[i].reward == BlessingReward.StaminaRecovery ? settings.SpiritStoneIcon :
                settings.Blessings[i].reward == BlessingReward.AllDamage ? settings.DamageBlessingIcon : settings.Icon(ConsumableKind.CryHeal);
            icons.Add(Icon("Blessing Icon", card, icon, new Vector2(isBundle ? 94 : 132, 269), isBundle ? new Vector2(48, 88) : new Vector2(76, 96)));
            var secondary = Icon("Secondary Blessing Icon", card, settings.Icon(ConsumableKind.CryShield), new Vector2(170, 269), new Vector2(48, 88));
            secondary.gameObject.SetActive(isBundle); secondaryIcons.Add(secondary);
            titles.Add(Text("Blessing Title", card, settings.Blessings[i].title, new Vector2(132, 187), new Vector2(234, 58), 26, PaperInk));
            string description = settings.Blessings[i].description
                .Replace("{damagePercent}", (settings.DamageBonusPerBlessing * 100f).ToString("0.#"))
                .Replace("{damageCap}", (settings.MaxDamageBonus * 100f).ToString("0.#"));
            descriptions.Add(Text("Blessing Description", card, description, new Vector2(132, 118), new Vector2(226, 98), 20, PaperInk));
            buttons.Add(Button("Choose Blessing " + (i + 1), card, "CHỌN", new Vector2(132, 40), new Vector2(218, 46), 21));
        }
        Text("Blessing Hint", content, "NHẤN 1 / 2 / 3 / 4 HOẶC CHỌN BẰNG CHUỘT", new Vector2(580, 25), new Vector2(960, 30), 17, Gold);
        Set(panels, "blessingPanel", overlay.gameObject);
        SetArray(panels, "blessingButtons", buttons); SetArray(panels, "blessingTitles", titles);
        SetArray(panels, "blessingDescriptions", descriptions); SetArray(panels, "blessingIcons", icons);
        SetArray(panels, "blessingSecondaryIcons", secondaryIcons);
        overlay.gameObject.SetActive(false);
    }

    public static RunResources EnsureSceneResourceManager(Scene scene, GameResourceSettings config)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Set up the scene resource manager outside Play Mode.");
        RunResources manager = Find<RunResources>(scene).FirstOrDefault();
        if (manager == null)
        {
            var owner = new GameObject("Run Resources");
            SceneManager.MoveGameObjectToScene(owner, scene);
            manager = owner.AddComponent<RunResources>();
        }
        Set(manager, "settings", config);
        return manager;
    }

    private static void InstallInScene(Scene scene, GameObject hudPrefab, bool purchasing)
    {
        EnsureSceneResourceManager(scene, settings);
        Health health = Find<Health>(scene).Single();
        PlayerStamina stamina = Ensure<PlayerStamina>(health.gameObject);
        PlayerConsumables consumables = Ensure<PlayerConsumables>(health.gameObject);
        Set(consumables, "settings", settings);
        Set(health.GetComponent<PlayerController>(), "consumables", consumables);
        GameObject hudRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Game HUD");
        if (hudRoot == null) hudRoot = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, scene);
        PlayerResourceHud hud = hudRoot.GetComponent<PlayerResourceHud>();
        GameplayPanelController panels = hudRoot.GetComponent<GameplayPanelController>();
        Set(hud, "health", health); Set(hud, "stamina", stamina);
        if (!Find<EventSystem>(scene).Any())
        {
            var events = new GameObject("UI EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(events, scene);
        }
        LevelResourceFlow flow = Find<LevelResourceFlow>(scene).FirstOrDefault();
        if (flow == null)
        {
            var flowRoot = new GameObject("Level Resources");
            SceneManager.MoveGameObjectToScene(flowRoot, scene);
            flow = flowRoot.AddComponent<LevelResourceFlow>();
        }
        Set(flow, "settings", settings); Set(flow, "panels", panels); Set(flow, "playerHealth", health); Set(flow, "playerStamina", stamina);
        SetBool(flow, "purchasingLevel", purchasing);
        foreach (MonsterHealth monster in Find<MonsterHealth>(scene))
        {
            SpiritStoneRewardOnDeath reward = Ensure<SpiritStoneRewardOnDeath>(monster.gameObject);
            if (string.IsNullOrEmpty(new SerializedObject(reward).FindProperty("rewardId").stringValue))
                SetString(reward, "rewardId", Guid.NewGuid().ToString("N"));
        }
        foreach (LevelExitDoor door in Find<LevelExitDoor>(scene))
        {
            if (!purchasing) SetString(door, "targetSceneName", scene.path == ScenePaths[0] ? "Level 2" : "Purchasing Levels");
            SetBool(door, "requireKey", !purchasing);
            SetBool(door, "offerBlessingOnArrival", !purchasing);
        }
        foreach (StoreStatueInteractable statue in Find<StoreStatueInteractable>(scene))
        {
            Set(statue, "panels", panels); Set(statue, "player", health.transform);
            EnsureStatuePrompt(statue);
        }
        foreach (SpriteRenderer renderer in Find<SpriteRenderer>(scene).ToArray())
        {
            if (renderer.name == "red_crystal_0000-sheet_0" || renderer.name == "blue_crystal_0000-sheet_0")
            {
                var pickup = Ensure<ConsumablePickup>(renderer.gameObject);
                SetInt(pickup, "kind", renderer.name.StartsWith("red_") ? 0 : 1);
                var collider = renderer.GetComponent<CircleCollider2D>();
                collider.isTrigger = true; collider.radius = 0.3f;
            }
            if (renderer.name == "Healing1_0" || renderer.name == "Shield Round Hex Scan Loop 128x128 BW_0")
                renderer.gameObject.SetActive(false);
        }
    }

    private static void EnsureStatuePrompt(StoreStatueInteractable statue)
    {
        if (statue.transform.Find("Store Interaction Prompt") != null) return;
        RectTransform rect = Rect("Store Interaction Prompt", statue.transform, new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0,2.15f),new Vector2(220,35));
        rect.localScale = Vector3.one * 0.01f;
        rect.gameObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        TMP_Text text = Text("F Prompt", rect, "[F] CỬA HÀNG", new Vector2(0,0), new Vector2(220,35), 26, Gold);
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f,0.5f);
        text.rectTransform.anchoredPosition = Vector2.zero;
        rect.GetComponent<Canvas>().sortingOrder = 50;
        Set(statue,"interactionPrompt",rect.gameObject);
        rect.gameObject.SetActive(false);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform Overlay(string name, Transform parent)
    {
        var rect = Rect(name,parent,Vector2.zero,new Vector2(0.5f,0.5f),Vector2.zero,Vector2.zero);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = new Color(0.025f,0.04f,0.06f,0.84f); image.raycastTarget = true;
        return rect;
    }

    private static UiSpriteFrame Frame(RectTransform rect, Sprite sprite, float border, float scale)
    {
        var frame = rect.gameObject.AddComponent<UiSpriteFrame>();
        frame.Configure(sprite,new Vector4(border,border,border,border),scale); frame.raycastTarget = false;
        return frame;
    }

    private static void Shape(string name,Transform parent,Vector2 position,Vector2 size,Color tint,HudShape shape)
    {
        RectTransform rect = Rect(name,parent,Vector2.zero,Vector2.zero,position,size);
        var graphic = rect.gameObject.AddComponent<HudShapeGraphic>(); graphic.SetShape(shape); graphic.color = tint; graphic.raycastTarget = false;
    }

    private static UnityEngine.UI.Image Fill(string name,Transform parent,Vector2 position,Vector2 size,Color tint)
    {
        RectTransform rect = Rect(name,parent,Vector2.zero,Vector2.zero,position,size);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite = whiteSprite; image.color = tint;
        image.type = UnityEngine.UI.Image.Type.Filled; image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; image.fillOrigin = 0;
        image.raycastTarget = false; image.fillAmount = 1f;
        return image;
    }

    private static UnityEngine.UI.Image Icon(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size)
    {
        RectTransform rect = Rect(name,parent,Vector2.zero,new Vector2(0.5f,0.5f),position,size);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Text(string name,Transform parent,string value,Vector2 position,Vector2 size,float fontSize,Color tint)
    {
        RectTransform rect = Rect(name,parent,Vector2.zero,new Vector2(0.5f,0.5f),position,size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = value; text.fontSize = fontSize;
        text.color = tint; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Truncate;
        return text;
    }

    private static UnityEngine.UI.Button Button(string name,Transform parent,string value,Vector2 position,Vector2 size,int fontSize)
    {
        RectTransform rect = Rect(name,parent,Vector2.zero,new Vector2(0.5f,0.5f),position,size);
        UiSpriteFrame frame = Frame(rect,settings.ItemCellFrame,4,2f); frame.raycastTarget = true;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); ConfigureButton(button,frame);
        TMP_Text text = Text("Label",rect,value,size * 0.5f,size - new Vector2(12,4),fontSize,Gold);
        text.fontStyle = FontStyles.Bold;
        return button;
    }

    private static void ConfigureButton(UnityEngine.UI.Button button,UnityEngine.UI.Graphic graphic)
    {
        button.targetGraphic = graphic;
        var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1f,0.91f,0.63f);
        colors.selectedColor = new Color(1f,0.87f,0.51f); colors.pressedColor = new Color(0.68f,0.57f,0.35f);
        colors.disabledColor = new Color(0.45f,0.45f,0.45f,0.75f); colors.fadeDuration = 0.08f; button.colors = colors;
    }

    private static void LayoutRow(RectTransform row,float spacing)
    {
        var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter; layout.spacing = spacing; layout.childControlHeight = true; layout.childControlWidth = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = false;
    }

    private static RectTransform LayoutChild(string name,Transform parent,Vector2 size)
    {
        RectTransform rect = Rect(name,parent,Vector2.zero,Vector2.zero,Vector2.zero,size);
        var layout = rect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); layout.preferredWidth = size.x; layout.preferredHeight = size.y;
        return rect;
    }

    private static T Ensure<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        if (component == null) component = root.AddComponent<T>();
        return component;
    }
    private static IEnumerable<T> Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true));
    private static Sprite FirstSprite(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name,StringComparer.Ordinal).First();
    private static Sprite PrefabSprite(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<SpriteRenderer>().sprite;

    private static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetArray<T>(UnityEngine.Object target,string field,IEnumerable<T> values) where T : UnityEngine.Object
    {
        var so = new SerializedObject(target); var property = so.FindProperty(field); T[] array = values.ToArray(); property.arraySize = array.Length;
        for (int i = 0; i < array.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetInt(UnityEngine.Object target,string field,int value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).intValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetBool(UnityEngine.Object target,string field,bool value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetString(UnityEngine.Object target,string field,string value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).stringValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
}
