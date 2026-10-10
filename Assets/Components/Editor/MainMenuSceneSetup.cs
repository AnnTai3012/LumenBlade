using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainMenuSceneSetup
{
    private const string HudPath = "Assets/Components/UI/Gameplay/Game HUD.prefab";

    [MenuItem("Tools/BTL/Create Main Menu Scene")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Create menu outside Play Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameSceneNavigation.MenuScenePath) != null)
            throw new InvalidOperationException("Main Menu scene already exists; preserve its authored contents.");
        var hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
        var settings = AssetDatabase.LoadAssetAtPath<GameResourceSettings>("Assets/Components/UI/Gameplay/Resources and UI Settings.asset");
        var menuMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Components/Audio/MenuBackGround.mp3");
        if (hud == null || settings == null || menuMusic == null) throw new InvalidOperationException("HUD, settings or menu music missing.");
        GameplayPauseAudioUiSetup.InstallSfxCategoryControls();
        var original = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .045f, .06f);
            var canvasObject = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = .5f;
            var menu = canvasObject.AddComponent<MainMenuController>();
            var panel = Rect("Main Menu Panel", canvasObject.transform, Vector2.zero, new Vector2(700f, 540f));
            panel.gameObject.AddComponent<UiSpriteFrame>().Configure(settings.ItemCellFrame, new Vector4(4f,4f,4f,4f),3f);
            Text("Title", panel, "MAIN MENU", new Vector2(0f,195f), new Vector2(580f,62f),40f,settings.Font);
            var sourcePanel = hud.transform.Find("Settings Overlay/Settings Panel");
            var buttonTemplate = sourcePanel.Find("Resume Game").gameObject;
            var play = Button(buttonTemplate, panel, "Play Game", "PLAY GAME", 75f);
            var settingButton = Button(buttonTemplate, panel, "Settings", "SETTINGS", -30f);
            var quit = Button(buttonTemplate, panel, "Quit Game", "QUIT GAME", -135f);
            var overlay = UnityEngine.Object.Instantiate(hud.transform.Find("Settings Overlay").gameObject,canvasObject.transform,false);
            overlay.name = "Settings Overlay";
            var settingsPanel = overlay.transform.Find("Settings Panel");
            var back = settingsPanel.Find("Resume Game").GetComponent<UnityEngine.UI.Button>();
            back.transform.Find("Label").GetComponent<TMP_Text>().text = "BACK";
            ((RectTransform)back.transform).anchoredPosition = new Vector2(0f,-220f);
            UnityEngine.Object.DestroyImmediate(settingsPanel.Find("Quit Game").gameObject);
            string[] rows = { "Music Volume Cell", "Ambience Cell", "Action Sounds Cell", "Combat Sounds Cell" };
            var sliders = rows.Select(n=>settingsPanel.Find(n).GetComponentInChildren<UnityEngine.UI.Slider>(true)).ToArray();
            for(int i=0;i<sliders.Length;i++) sliders[i].navigation=new UnityEngine.UI.Navigation {
                mode=UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp=i>0?sliders[i-1]:null,
                selectOnDown=i<sliders.Length-1?sliders[i+1]:back };
            back.navigation=new UnityEngine.UI.Navigation {mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=sliders[3]};
            play.navigation=new UnityEngine.UI.Navigation {mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnDown=settingButton};
            settingButton.navigation=new UnityEngine.UI.Navigation {mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=play,selectOnDown=quit};
            quit.navigation=new UnityEngine.UI.Navigation {mode=UnityEngine.UI.Navigation.Mode.Explicit,selectOnUp=settingButton};
            var serialized = new SerializedObject(menu);
            Set(serialized,"mainPanel",panel.gameObject);Set(serialized,"settingsPanel",overlay);
            Set(serialized,"playButton",play);Set(serialized,"settingsButton",settingButton);Set(serialized,"quitButton",quit);Set(serialized,"backButton",back);
            Set(serialized,"musicVolumeSlider",sliders[0]);Set(serialized,"musicVolumeValue",settingsPanel.Find(rows[0]+"/Volume Value").GetComponent<TMP_Text>());
            var groupSliders=serialized.FindProperty("sfxCategorySliders");var groupValues=serialized.FindProperty("sfxCategoryValues");
            groupSliders.arraySize=groupValues.arraySize=3;
            for(int i=0;i<3;i++) {groupSliders.GetArrayElementAtIndex(i).objectReferenceValue=sliders[i+1];groupValues.GetArrayElementAtIndex(i).objectReferenceValue=settingsPanel.Find(rows[i+1]+"/Volume Value").GetComponent<TMP_Text>();}
            serialized.ApplyModifiedPropertiesWithoutUndo();
            overlay.SetActive(false);
            var eventObject = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
            eventObject.GetComponent<UnityEngine.EventSystems.EventSystem>().firstSelectedGameObject=play.gameObject;
            if(!EditorSceneManager.SaveScene(scene,GameSceneNavigation.MenuScenePath))throw new InvalidOperationException("Cannot save menu scene.");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(GameSceneNavigation.MenuScenePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=GameSceneNavigation.MenuScenePath)).ToArray();
            var catalog=AssetDatabase.LoadAssetAtPath<GameAudioCatalog>("Assets/Components/Resources/GameAudioCatalog.asset");
            catalog.menuMusic=menuMusic;
            catalog.sceneProfiles=catalog.sceneProfiles.Where(p=>p!=null&&p.sceneName!=GameSceneNavigation.MenuSceneName).Concat(new[]{new AudioSceneProfile{sceneName=GameSceneNavigation.MenuSceneName,environment=GameAudioEnvironment.Sanctuary,music=menuMusic,ambienceVolume=0f,reverb=.03f}}).ToArray();
            EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
            var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(menuMusic));
            var importSettings=importer.defaultSampleSettings;importSettings.loadType=AudioClipLoadType.Streaming;importSettings.preloadAudioData=false;
            importer.defaultSampleSettings=importSettings;importer.SaveAndReimport();
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene,true);
        }
    }

    private static void Set(SerializedObject target,string field,UnityEngine.Object value)=>target.FindProperty(field).objectReferenceValue=value;
    private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;
    }
    private static void Text(string name,Transform parent,string value,Vector2 position,Vector2 size,float fontSize,TMP_FontAsset font)
    {
        var r=Rect(name,parent,position,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font;t.text=value;t.fontSize=fontSize;t.color=new Color(.95f,.81f,.45f);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;
    }
    private static UnityEngine.UI.Button Button(GameObject template,Transform parent,string name,string label,float y)
    {
        var go=UnityEngine.Object.Instantiate(template,parent,false);go.name=name;
        var rect=(RectTransform)go.transform;rect.anchoredPosition=new Vector2(0f,y);rect.sizeDelta=new Vector2(360f,76f);
        var text=go.transform.Find("Label").GetComponent<TMP_Text>();text.text=label;text.rectTransform.sizeDelta=new Vector2(340f,60f);
        var button=go.GetComponent<UnityEngine.UI.Button>();button.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
        if(go.GetComponent<UiButtonAudio>()==null)go.AddComponent<UiButtonAudio>();return button;
    }
}
