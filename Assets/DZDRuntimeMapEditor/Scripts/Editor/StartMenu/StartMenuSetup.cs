using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public static class StartMenuSetup
    {
        const string ConfigPath = MapEditorPaths.StartMenuConfig;
        const string SessionPath = MapEditorPaths.MapSessionRequest;
        const string PersistenceConfigPath = MapEditorPaths.PersistenceConfig;
        const string ToastChannelPath = MapEditorPaths.ToastChannel;
        static string ToastPrefabPath => MapEditorPaths.ToastHudPrefab;
        static string LoadPanelPrefabPath => MapEditorPaths.MapLoadPanelPrefab;
        static string MenuPrefabPath => MapEditorPaths.StartMenuPrefab;
        static string FlyerPath => MapEditorPaths.FlyerPrefab;
        const string StartScenePath = MapEditorPaths.StartScene;
        const string EditorScenePath = MapEditorPaths.EditorScene;

        public static string Setup()
        {
            EnsureFolder(MapEditorPaths.UserDataRoot + "/StartMenu");
            EnsureFolder(MapEditorPaths.UserPrefabRoot + "/UI");
            EnsureFolder(MapEditorPaths.SampleScenesRoot);

            var config = EnsureConfig();
            var session = EnsureSession();
            var menuPrefab = EnsureMenuPrefab(config, session);
            WireFlyer(session);
            WireOpenSceneFlyers(session);
            EnsureStartScene(config, session, menuPrefab);
            ApplyBuildSettings();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "开始界面已配置。Play 从 StartScene 进入；新建地图打开 SampleScene。";
        }

        static StartMenuConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<StartMenuConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<StartMenuConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            var so = new SerializedObject(config);
            so.FindProperty("editorSceneName").stringValue = "SampleScene";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        static MapSessionRequest EnsureSession()
        {
            var session = AssetDatabase.LoadAssetAtPath<MapSessionRequest>(SessionPath);
            if (session != null)
                return session;

            session = ScriptableObject.CreateInstance<MapSessionRequest>();
            AssetDatabase.CreateAsset(session, SessionPath);
            return session;
        }

        static GameObject EnsureMenuPrefab(StartMenuConfig config, MapSessionRequest session)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
            if (existing == null || existing.GetComponent<StartMenuView>() == null)
            {
                var root = CreateMenuRoot(config);
                PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
                Object.DestroyImmediate(root);
                existing = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
            }

            WireMenuPrefab(existing, config, session);
            return AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
        }

        static void WireMenuPrefab(GameObject prefab, StartMenuConfig config, MapSessionRequest session)
        {
            if (prefab == null)
                return;

            var contents = PrefabUtility.LoadPrefabContents(MenuPrefabPath);
            try
            {
                var controller = contents.GetComponent<StartMenuController>();
                if (controller == null)
                    controller = contents.AddComponent<StartMenuController>();

                var view = contents.GetComponent<StartMenuView>();
                var persistenceConfig = AssetDatabase.LoadAssetAtPath<MapPersistenceConfig>(PersistenceConfigPath);
                var so = new SerializedObject(controller);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("persistenceConfig").objectReferenceValue = persistenceConfig;
                so.FindProperty("session").objectReferenceValue = session;
                so.FindProperty("view").objectReferenceValue = view;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, MenuPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static GameObject CreateMenuRoot(StartMenuConfig config)
        {
            var font = UiTmpFontSetup.LoadProjectFont();
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            var root = new GameObject("StartMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(StartMenuView), typeof(StartMenuController));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            UiTmpFontSetup.EnableTmpShaderChannels(canvas);

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var group = root.GetComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            var dimmer = CreateUi("Background", root.transform);
            Stretch(dimmer);
            var dimmerImage = dimmer.AddComponent<Image>();
            dimmerImage.sprite = sprite;
            dimmerImage.color = new Color(0.07f, 0.08f, 0.1f, 1f);
            dimmerImage.raycastTarget = true;

            var window = CreateUi("Window", root.transform);
            var windowRt = window.GetComponent<RectTransform>();
            windowRt.anchorMin = new Vector2(0.5f, 0.5f);
            windowRt.anchorMax = new Vector2(0.5f, 0.5f);
            windowRt.pivot = new Vector2(0.5f, 0.5f);
            windowRt.sizeDelta = new Vector2(640f, 480f);
            var windowImage = window.AddComponent<Image>();
            windowImage.sprite = sprite;
            windowImage.color = new Color(0.1f, 0.1f, 0.12f, 0.96f);
            windowImage.raycastTarget = true;

            var title = CreateText(window.transform, "Title", config.Title, font, 36, TextAlignmentOptions.Center);
            PlaceTop(title.rectTransform, -28f, 48f);

            var hint = CreateText(window.transform, "Hint", config.Hint, font, 18, TextAlignmentOptions.Center);
            hint.color = new Color(0.75f, 0.75f, 0.78f, 1f);
            hint.enableWordWrapping = true;
            PlaceTop(hint.rectTransform, -80f, 40f);

            CreateMenuButton(window.transform, "OpenSaves", config.OpenSaveLabel, font, sprite, 0);
            CreateMenuButton(window.transform, "NewMap", config.NewMapLabel, font, sprite, 1);

            var viewSo = new SerializedObject(root.GetComponent<StartMenuView>());
            viewSo.FindProperty("title").objectReferenceValue = title;
            viewSo.FindProperty("hint").objectReferenceValue = hint;
            viewSo.FindProperty("openSaveButton").objectReferenceValue =
                window.transform.Find("OpenSaves").GetComponent<Button>();
            viewSo.FindProperty("newMapButton").objectReferenceValue =
                window.transform.Find("NewMap").GetComponent<Button>();
            viewSo.FindProperty("openSaveLabel").objectReferenceValue =
                window.transform.Find("OpenSaves").GetComponentInChildren<TMP_Text>();
            viewSo.FindProperty("newMapLabel").objectReferenceValue =
                window.transform.Find("NewMap").GetComponentInChildren<TMP_Text>();
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static void CreateMenuButton(
            Transform parent,
            string name,
            string label,
            TMP_FontAsset font,
            Sprite sprite,
            int index)
        {
            var go = CreateUi(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 20f - index * 96f);
            rt.sizeDelta = new Vector2(420f, 72f);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.2f, 0.22f, 0.28f, 1f);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.75f, 0.88f, 1f, 1f);
            colors.pressedColor = new Color(0.6f, 0.72f, 0.9f, 1f);
            button.colors = colors;
            var text = CreateText(go.transform, "Label", label, font, 28, TextAlignmentOptions.Center);
            Stretch(text.gameObject);
            text.raycastTarget = false;
        }

        static void WireFlyer(MapSessionRequest session)
        {
            var contents = PrefabUtility.LoadPrefabContents(FlyerPath);
            try
            {
                var persistence = contents.GetComponent<MapPersistenceController>()
                    ?? contents.GetComponentInChildren<MapPersistenceController>(true);
                if (persistence == null)
                    return;

                AssignSession(persistence, session);
                PrefabUtility.SaveAsPrefabAsset(contents, FlyerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void WireOpenSceneFlyers(MapSessionRequest session)
        {
            var controllers = Object.FindObjectsByType<MapPersistenceController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < controllers.Length; i++)
                AssignSession(controllers[i], session);
        }

        static void AssignSession(MapPersistenceController persistence, MapSessionRequest session)
        {
            if (persistence == null)
                return;

            var so = new SerializedObject(persistence);
            so.FindProperty("session").objectReferenceValue = session;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(persistence);
        }

        static void EnsureStartScene(StartMenuConfig config, MapSessionRequest session, GameObject menuPrefab)
        {
            var loadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LoadPanelPrefabPath);
            var toastPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ToastPrefabPath);
            Scene scene;
            var closeAfter = false;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath) == null)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                closeAfter = true;
                PopulateStartScene(scene, config, session, menuPrefab, loadPrefab, toastPrefab);
                EditorSceneManager.SaveScene(scene, StartScenePath);
            }
            else
            {
                scene = FindOpenScene(StartScenePath);
                if (!scene.IsValid())
                {
                    scene = EditorSceneManager.OpenScene(StartScenePath, OpenSceneMode.Additive);
                    closeAfter = true;
                }

                PopulateStartScene(scene, config, session, menuPrefab, loadPrefab, toastPrefab);
                EditorSceneManager.SaveScene(scene);
            }

            if (closeAfter && SceneManager.sceneCount > 1)
                EditorSceneManager.CloseScene(scene, true);
        }

        static Scene FindOpenScene(string path)
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.path == path)
                    return scene;
            }

            return default;
        }

        static void PopulateStartScene(
            Scene scene,
            StartMenuConfig config,
            MapSessionRequest session,
            GameObject menuPrefab,
            GameObject loadPrefab,
            GameObject toastPrefab)
        {
            EditorSceneManager.SetActiveScene(scene);
            EnsureCamera(scene);
            EnsureEventSystem(scene);

            var menu = FindInScene<StartMenuView>(scene);
            if (menu == null && menuPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(menuPrefab, scene);
                instance.name = "StartMenu";
                menu = instance.GetComponent<StartMenuView>();
            }

            var load = FindInScene<MapLoadPanelView>(scene);
            if (load == null && loadPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(loadPrefab, scene);
                instance.name = "MapLoadPanel";
                instance.SetActive(false);
                load = instance.GetComponent<MapLoadPanelView>();
            }
            else if (load != null)
            {
                load.gameObject.SetActive(false);
            }

            if (FindInScene<ToastHudView>(scene) == null && toastPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(toastPrefab, scene);
                instance.name = "ToastHud";
                instance.SetActive(true);
            }

            if (menu == null)
                return;

            var controller = menu.GetComponent<StartMenuController>();
            if (controller == null)
                controller = menu.gameObject.AddComponent<StartMenuController>();

            var persistenceConfig = AssetDatabase.LoadAssetAtPath<MapPersistenceConfig>(PersistenceConfigPath);
            var so = new SerializedObject(controller);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("persistenceConfig").objectReferenceValue = persistenceConfig;
            so.FindProperty("session").objectReferenceValue = session;
            so.FindProperty("view").objectReferenceValue = menu;
            so.FindProperty("loadPanel").objectReferenceValue = load;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void EnsureCamera(Scene scene)
        {
            var camera = FindInScene<Camera>(scene);
            if (camera == null)
            {
                var go = new GameObject("Main Camera");
                SceneManager.MoveGameObjectToScene(go, scene);
                camera = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
                go.tag = "MainCamera";
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.1f, 1f);
            camera.orthographic = false;
        }

        static void EnsureEventSystem(Scene scene)
        {
            var existing = FindInScene<EventSystem>(scene);
            if (existing != null)
            {
                var standalone = existing.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                    Object.DestroyImmediate(standalone);
                if (existing.GetComponent<InputSystemUIInputModule>() == null)
                    existing.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
                return;
            }

            var go = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        static T FindInScene<T>(Scene scene) where T : Component
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = roots[i].GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }

            return null;
        }

        static void ApplyBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(StartScenePath, true),
                new EditorBuildSettingsScene(EditorScenePath, true)
            };
        }

        static GameObject CreateUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void PlaceTop(RectTransform rt, float y, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(-48f, height);
        }

        static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string value,
            TMP_FontAsset font,
            int size,
            TextAlignmentOptions alignment)
        {
            var go = CreateUi(name, parent);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = true;
            text.text = value;
            return text;
        }

        static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
