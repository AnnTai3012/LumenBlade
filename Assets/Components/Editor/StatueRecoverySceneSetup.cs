using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class StatueRecoverySceneSetup
{
    private const string HudPath = "Assets/Components/UI/Gameplay/Game HUD.prefab";
    private const string SettingsPath = "Assets/Components/UI/Gameplay/Resources and UI Settings.asset";
    private const string SupplyStatuePath = "Assets/Components/UI/Gameplay/Stutue_0.prefab";
    private const string SpecialStatuePath = "Assets/Components/UI/Gameplay/Bless Statue( store).prefab";

    [MenuItem("Tools/BTL/Install Start Stores and Revive Cross")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Install stores outside Play Mode.");
        var settings = AssetDatabase.LoadAssetAtPath<GameResourceSettings>(SettingsPath);
        if (settings == null) throw new InvalidOperationException("Resources and UI Settings is missing.");
        var config = new SerializedObject(settings);
        config.FindProperty("cryHealPrice").intValue = 6;
        config.FindProperty("cryShieldPrice").intValue = 5;
        config.FindProperty("reviveCrossPrice").intValue = 30;
        config.FindProperty("reviveCrossCapacity").intValue = 2;
        config.FindProperty("reviveCrossIcon").objectReferenceValue = AssetDatabase.LoadAllAssetsAtPath("Assets/Objects/Items/ThanhGia.png").OfType<Sprite>().First();
        config.ApplyModifiedPropertiesWithoutUndo();
        UpgradeHudPrefab();
        ConfigureStatuePrefab(SupplyStatuePath, StoreCatalog.Supplies, settings);
        ConfigureStatuePrefab(SpecialStatuePath, StoreCatalog.SpecialItems, settings);
        foreach (string path in new[] { "Assets/Objects/World/Scenes/Level 1.unity", "Assets/Objects/World/Scenes/Level 2.unity", "Assets/Objects/World/Scenes/Level 3.unity", "Assets/Objects/World/Scenes/Purchasing LevelsC1.unity", "Assets/Objects/World/Scenes/Level BossC1.unity" })
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                InstallInScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        AssetDatabase.SaveAssets();
    }

    public static void UpgradeHudPrefab()
    {
        var settings = AssetDatabase.LoadAssetAtPath<GameResourceSettings>(SettingsPath);
        GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Transform bars = root.transform.Find("ResourceBars");
            Transform cross = bars.Find("ReviveCross Cell");
            if (cross == null)
            {
                cross = UnityEngine.Object.Instantiate(bars.Find("CryShield Cell").gameObject, bars).transform;
                cross.name = "ReviveCross Cell";
            }
            ((RectTransform)cross).anchoredPosition = new Vector2(277f, 96f);
            cross.Find("Item Icon").GetComponent<UnityEngine.UI.Image>().sprite = settings.Icon(ConsumableKind.ReviveCross);
            TMP_Text hotkey = cross.Find("Hotkey").GetComponent<TMP_Text>();
            hotkey.text = "AUTO";
            hotkey.fontSize = 10f;
            hotkey.rectTransform.anchoredPosition = new Vector2(24f, 16f);
            hotkey.rectTransform.sizeDelta = new Vector2(38f, 22f);
            cross.Find("Count").GetComponent<TMP_Text>().text = "0/" + settings.Capacity(ConsumableKind.ReviveCross);
            var kinds = new[] { ConsumableKind.CryHeal, ConsumableKind.CryShield, ConsumableKind.ReviveCross };
            var slots = kinds.Select(k => bars.Find(k + " Cell")).ToArray();
            PlayerResourceHud hud = root.GetComponent<PlayerResourceHud>();
            References(hud, "itemSlots", slots.Select(s => (UnityEngine.Object)s).ToArray());
            References(hud, "itemButtons", slots.Select(s => (UnityEngine.Object)s.GetComponent<UnityEngine.UI.Button>()).ToArray());
            References(hud, "itemCounts", slots.Select(s => (UnityEngine.Object)s.Find("Count").GetComponent<TMP_Text>()).ToArray());

            Transform panel = root.transform.Find("Store Overlay/Store Panel");
            Transform items = panel.Find("Store Items");
            Transform crossCard = items.Find("ReviveCross Store Cell");
            if (crossCard == null)
            {
                crossCard = UnityEngine.Object.Instantiate(items.Find("CryHeal Store Cell").gameObject, items).transform;
                crossCard.name = "ReviveCross Store Cell";
                crossCard.Find("Buy CryHeal").name = "Buy ReviveCross";
            }
            crossCard.Find("Item Title").GetComponent<TMP_Text>().text = "THÁNH GIÁ";
            crossCard.Find("Item Icon").GetComponent<UnityEngine.UI.Image>().sprite = settings.Icon(ConsumableKind.ReviveCross);
            TMP_Text crossEffect = crossCard.Find("Effect").GetComponent<TMP_Text>();
            crossEffect.fontSize = 17f;
            crossEffect.rectTransform.sizeDelta = new Vector2(244f, 44f);
            var cards = kinds.Select(k => items.Find(k + " Store Cell")).ToArray();
            GameplayPanelController panels = root.GetComponent<GameplayPanelController>();
            References(panels, "storeCells", cards.Select(c => (UnityEngine.Object)c.gameObject).ToArray());
            References(panels, "purchaseButtons", cards.Select(c => (UnityEngine.Object)c.GetComponentInChildren<UnityEngine.UI.Button>(true)).ToArray());
            References(panels, "storeCounts", cards.Select(c => (UnityEngine.Object)c.Find("Owned Count").GetComponent<TMP_Text>()).ToArray());
            References(panels, "storePrices", cards.Select(c => (UnityEngine.Object)c.Find("Price").GetComponent<TMP_Text>()).ToArray());
            References(panels, "storeEffects", cards.Select(c => (UnityEngine.Object)c.Find("Effect").GetComponent<TMP_Text>()).ToArray());
            Reference(panels, "storeTitle", panel.Find("Store Title").GetComponent<TMP_Text>());
            Reference(panels, "storeSubtitle", panel.Find("Store Subtitle").GetComponent<TMP_Text>());
            crossCard.gameObject.SetActive(false);

            ExtendReceipts(root, settings);
            PlayerRecoveryEffectUI recovery = root.GetComponent<PlayerRecoveryEffectUI>();
            if (recovery == null) recovery = root.AddComponent<PlayerRecoveryEffectUI>();
            ConfigureEffect(root.transform, recovery, "heal", "Heal Effect Frame", "Assets/Objects/Player/Ui/AnimHeal/Heal.anim", settings);
            ConfigureEffect(root.transform, recovery, "revive", "Revive Effect Frame", "Assets/Objects/Player/Ui/AnimRevive/Revive.anim", settings);
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ExtendReceipts(GameObject root, GameResourceSettings settings)
    {
        PlayerItemReceiptUI receipts = root.GetComponent<PlayerItemReceiptUI>();
        Transform container = root.transform.Find("Item Receipt Notifications");
        Transform row = container.Find("ReviveCross Receipt");
        if (row == null)
        {
            row = UnityEngine.Object.Instantiate(container.Find("CryShield Receipt").gameObject, container).transform;
            row.name = "ReviveCross Receipt";
        }
        row.Find("Item Icon").GetComponent<UnityEngine.UI.Image>().sprite = settings.Icon(ConsumableKind.ReviveCross);
        row.Find("Received Text").GetComponent<TMP_Text>().text = "Nhận Thánh giá +1";
        row.gameObject.SetActive(false);
        var serialized = new SerializedObject(receipts);
        SerializedProperty rows = serialized.FindProperty("rows");
        rows.arraySize = 3;
        SerializedProperty added = rows.GetArrayElementAtIndex(2);
        added.FindPropertyRelative("root").objectReferenceValue = row;
        added.FindPropertyRelative("icon").objectReferenceValue = row.Find("Item Icon").GetComponent<UnityEngine.UI.Image>();
        added.FindPropertyRelative("label").objectReferenceValue = row.Find("Received Text").GetComponent<TMP_Text>();
        added.FindPropertyRelative("visibility").objectReferenceValue = row.GetComponent<CanvasGroup>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureEffect(Transform canvas, PlayerRecoveryEffectUI recovery, string field, string name, string clipPath, GameResourceSettings settings)
    {
        RectTransform frame = canvas.Find(name) as RectTransform;
        if (frame == null)
        {
            frame = Rect(name, canvas, new Vector2(108f, 112f));
            frame.gameObject.AddComponent<CanvasRenderer>();
            var border = frame.gameObject.AddComponent<UiSpriteFrame>();
            border.Configure(settings.ItemCellFrame, new Vector4(4f, 4f, 4f, 4f), 2f);
            border.raycastTarget = false;
            var group = frame.gameObject.AddComponent<CanvasGroup>();
            group.interactable = group.blocksRaycasts = false;
            var icon = Rect("Animation", frame, new Vector2(78f, 78f));
            icon.anchoredPosition = new Vector2(0f, 7f);
            var image = icon.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            var text = Rect("Effect Text", frame, new Vector2(94f, 20f));
            text.anchoredPosition = new Vector2(0f, -39f);
            var label = text.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = settings.Font;
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.9f, 0.65f);
            label.raycastTarget = false;
        }
        // The recovery sprite overlays the body; the old notification frame stays hidden.
        var frameBorder = frame.GetComponent<UiSpriteFrame>();
        if (frameBorder != null) frameBorder.enabled = false;
        frame.Find("Effect Text").gameObject.SetActive(false);
        ((RectTransform)frame.Find("Animation")).anchoredPosition = Vector2.zero;
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b => b.propertyName == "m_Sprite");
        ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        var serialized = new SerializedObject(recovery);
        serialized.FindProperty("worldOffset").vector3Value = Vector3.zero;
        var effect = serialized.FindProperty(field);
        effect.FindPropertyRelative("root").objectReferenceValue = frame;
        effect.FindPropertyRelative("image").objectReferenceValue = frame.Find("Animation").GetComponent<UnityEngine.UI.Image>();
        effect.FindPropertyRelative("label").objectReferenceValue = frame.Find("Effect Text").GetComponent<TMP_Text>();
        effect.FindPropertyRelative("visibility").objectReferenceValue = frame.GetComponent<CanvasGroup>();
        effect.FindPropertyRelative("duration").floatValue = clip.length;
        SerializedProperty frames = effect.FindPropertyRelative("frames");
        SerializedProperty times = effect.FindPropertyRelative("frameTimes");
        frames.arraySize = times.arraySize = keys.Length;
        for (int i = 0; i < keys.Length; i++)
        {
            frames.GetArrayElementAtIndex(i).objectReferenceValue = keys[i].value;
            times.GetArrayElementAtIndex(i).floatValue = keys[i].time;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        frame.gameObject.SetActive(false);
        int storeIndex = canvas.Find("Store Overlay").GetSiblingIndex();
        if (frame.GetSiblingIndex() > storeIndex) frame.SetSiblingIndex(storeIndex);
    }

    private static void ConfigureStatuePrefab(string path, StoreCatalog catalog, GameResourceSettings settings)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ConfigureStatue(root, catalog, settings, null, null);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void InstallInScene(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Health player = roots.SelectMany(r => r.GetComponentsInChildren<Health>(true)).FirstOrDefault();
        GameplayPanelController panels = roots.SelectMany(r => r.GetComponentsInChildren<GameplayPanelController>(true)).FirstOrDefault();
        LevelResourceFlow flow = roots.SelectMany(r => r.GetComponentsInChildren<LevelResourceFlow>(true)).FirstOrDefault();
        if (player == null || panels == null || flow == null) return;
        var settings = AssetDatabase.LoadAssetAtPath<GameResourceSettings>(SettingsPath);
        foreach (SpriteRenderer preview in roots.SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true)))
            if (preview.name == "HoiSinh_0" && preview.sprite != null &&
                AssetDatabase.GetAssetPath(preview.sprite.texture) == "Assets/Objects/Player/Ui/HoiSinh.png")
                preview.gameObject.SetActive(false);
        bool purchasing = flow.IsPurchasingLevel;
        string prefabPath = purchasing ? SpecialStatuePath : SupplyStatuePath;
        var statues = roots.SelectMany(r => r.GetComponentsInChildren<SpriteRenderer>(true))
            .Where(r => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(r.gameObject) == prefabPath).ToArray();
        if (statues.Length == 0)
        {
            GameObject statue = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), scene);
            statue.name = purchasing ? "Special Item Store Statue" : "Start Store Statue";
            statue.transform.position = player.transform.position + new Vector3(1.75f, 0.16f, 0f);
            if (!purchasing) statue.transform.localScale = new Vector3(1.607f, 1.441f, 1f);
            statues = new[] { statue.GetComponent<SpriteRenderer>() };
        }
        foreach (SpriteRenderer statue in statues)
        {
            if (!purchasing && statue.name == "Start Store Statue")
            {
                var doors = roots.SelectMany(r => r.GetComponentsInChildren<LevelExitDoor>(true))
                    .Select(d => d.GetComponent<Collider2D>()).Where(c => c != null).ToArray();
                // Keep the entire shop interaction area away from return-door triggers.
                for (int step = 0; step < 16 && doors.Any(c => ((Vector2)c.ClosestPoint(statue.transform.position) - (Vector2)statue.transform.position).sqrMagnitude < 1.9f * 1.9f); step++)
                    statue.transform.position += Vector3.right * 0.25f;
            }
            ConfigureStatue(statue.gameObject, purchasing ? StoreCatalog.SpecialItems : StoreCatalog.Supplies, settings, panels, player.transform);
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void ConfigureStatue(GameObject root, StoreCatalog catalog, GameResourceSettings settings, GameplayPanelController panels, Transform player)
    {
        var collider = root.GetComponent<BoxCollider2D>();
        if (collider == null) collider = root.AddComponent<BoxCollider2D>();
        Sprite sprite = root.GetComponent<SpriteRenderer>().sprite;
        collider.size = sprite.bounds.size;
        collider.offset = sprite.bounds.center;
        collider.isTrigger = true;
        var store = root.GetComponent<StoreStatueInteractable>();
        if (store == null) store = root.AddComponent<StoreStatueInteractable>();
        var serialized = new SerializedObject(store);
        serialized.FindProperty("catalog").intValue = (int)catalog;
        serialized.FindProperty("panels").objectReferenceValue = panels;
        serialized.FindProperty("player").objectReferenceValue = player;
        RectTransform prompt = root.transform.Find("Store Interaction Prompt") as RectTransform;
        if (prompt == null)
        {
            prompt = Rect("Store Interaction Prompt", root.transform, new Vector2(260f, 38f));
            prompt.localScale = Vector3.one * 0.01f;
            var canvas = prompt.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;
            var label = Rect("F Prompt", prompt, new Vector2(260f, 38f));
            var text = label.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = settings.Font;
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.9f, 0.65f);
            text.raycastTarget = false;
        }
        prompt.localPosition = new Vector3(0f, sprite.bounds.max.y + 0.4f, 0f);
        TMP_Text promptText = prompt.GetComponentInChildren<TMP_Text>(true);
        promptText.rectTransform.sizeDelta = new Vector2(260f, 38f);
        promptText.enableAutoSizing = true;
        promptText.fontSizeMin = 16f;
        promptText.fontSizeMax = 24f;
        promptText.textWrappingMode = TextWrappingModes.NoWrap;
        promptText.text = catalog == StoreCatalog.SpecialItems ? "[F] VẬT PHẨM ĐẶC BIỆT" : "[F] TIẾP TẾ";
        prompt.gameObject.SetActive(false);
        serialized.FindProperty("interactionPrompt").objectReferenceValue = prompt.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }

    private static void Reference(UnityEngine.Object owner, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(owner);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void References(UnityEngine.Object owner, string field, UnityEngine.Object[] values)
    {
        var serialized = new SerializedObject(owner);
        SerializedProperty array = serialized.FindProperty(field);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
