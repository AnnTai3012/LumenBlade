using System;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class ChestLootUiSetup
{
    private const string ChestPath = "Assets/Objects/Items/Chest.prefab";
    private const string HudPath = "Assets/Components/UI/Gameplay/Game HUD.prefab";
    private const string SettingsPath = "Assets/Components/UI/Gameplay/Resources and UI Settings.asset";

    [MenuItem("Tools/BTL/Set Up Chest Loot and Item Notifications")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Set up chest loot outside Play Mode.");
        GameResourceSettings settings = AssetDatabase.LoadAssetAtPath<GameResourceSettings>(SettingsPath);
        if (settings == null || settings.Font == null)
            throw new InvalidOperationException("Resources and UI Settings with a font is required.");
        ConfigureChest(settings);
        ConfigureNotifications(settings);
    }

    private static void ConfigureChest(GameResourceSettings settings)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ChestPath);
        try
        {
            TreasureChestInteractable chest = root.GetComponent<TreasureChestInteractable>();
            if (chest == null) chest = root.AddComponent<TreasureChestInteractable>();
            Transform prompt = root.transform.Find("Interaction Prompt");
            if (prompt == null)
            {
                var promptObject = new GameObject("Interaction Prompt", typeof(RectTransform), typeof(TextMeshPro));
                prompt = promptObject.transform;
                prompt.SetParent(root.transform, false);
                prompt.localPosition = new Vector3(0f, 0.8f, 0f);
                var label = prompt.GetComponent<TextMeshPro>();
                label.font = settings.Font;
                label.fontSize = 3f;
                label.text = "F - Mở rương";
                label.alignment = TextAlignmentOptions.Center;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.color = new Color(1f, 0.89f, 0.6f);
                label.rectTransform.sizeDelta = new Vector2(5f, 0.6f);
                label.GetComponent<Renderer>().sortingOrder = 50;
                promptObject.SetActive(false);
            }
            var serialized = new SerializedObject(chest);
            serialized.FindProperty("animator").objectReferenceValue = root.GetComponent<Animator>();
            serialized.FindProperty("interactionPrompt").objectReferenceValue = prompt.GetComponent<TMP_Text>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, ChestPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigureNotifications(GameResourceSettings settings)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            PlayerItemReceiptUI notices = root.GetComponent<PlayerItemReceiptUI>();
            if (notices == null) notices = root.AddComponent<PlayerItemReceiptUI>();
            RectTransform container = root.transform.Find("Item Receipt Notifications") as RectTransform;
            if (container == null)
            {
                container = NewRect("Item Receipt Notifications", root.transform);
                container.anchorMin = Vector2.zero;
                container.anchorMax = Vector2.one;
                container.offsetMin = container.offsetMax = Vector2.zero;
            }
            container.SetAsLastSibling();
            var serialized = new SerializedObject(notices);
            serialized.FindProperty("settings").objectReferenceValue = settings;
            var rows = serialized.FindProperty("rows");
            rows.arraySize = System.Enum.GetValues(typeof(ConsumableKind)).Length;
            for (int i = 0; i < rows.arraySize; i++)
            {
                ConsumableKind kind = (ConsumableKind)i;
                RectTransform row = container.Find(kind + " Receipt") as RectTransform;
                if (row == null)
                {
                    row = NewRect(kind + " Receipt", container);
                    row.sizeDelta = new Vector2(310f, 44f);
                    var visibility = row.gameObject.AddComponent<CanvasGroup>();
                    visibility.interactable = false;
                    visibility.blocksRaycasts = false;

                    RectTransform background = NewRect("Background", row);
                    background.anchorMin = Vector2.zero;
                    background.anchorMax = Vector2.one;
                    background.offsetMin = background.offsetMax = Vector2.zero;
                    var plate = background.gameObject.AddComponent<UnityEngine.UI.Image>();
                    plate.color = new Color(0.025f, 0.04f, 0.055f, 0.82f);
                    plate.raycastTarget = false;

                    RectTransform iconRect = NewRect("Item Icon", row);
                    iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
                    iconRect.anchoredPosition = new Vector2(23f, 0f);
                    iconRect.sizeDelta = new Vector2(28f, 34f);
                    var icon = iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();
                    icon.sprite = settings.Icon(kind);
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;

                    RectTransform labelRect = NewRect("Received Text", row);
                    labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0.5f);
                    labelRect.pivot = new Vector2(0f, 0.5f);
                    labelRect.anchoredPosition = new Vector2(44f, 0f);
                    labelRect.sizeDelta = new Vector2(258f, 38f);
                    var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
                    label.font = settings.Font;
                    label.fontSize = 24f;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.color = kind == ConsumableKind.CryHeal ? new Color(1f, 0.91f, 0.7f) : new Color(0.75f, 0.94f, 1f);
                    label.text = "Nhận " + GameResourceSettings.ItemName(kind) + " +1";
                    label.raycastTarget = false;
                    row.gameObject.SetActive(false);
                }
                var element = rows.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = row;
                element.FindPropertyRelative("icon").objectReferenceValue = row.Find("Item Icon").GetComponent<UnityEngine.UI.Image>();
                element.FindPropertyRelative("label").objectReferenceValue = row.Find("Received Text").GetComponent<TMP_Text>();
                element.FindPropertyRelative("visibility").objectReferenceValue = row.GetComponent<CanvasGroup>();
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }
}
