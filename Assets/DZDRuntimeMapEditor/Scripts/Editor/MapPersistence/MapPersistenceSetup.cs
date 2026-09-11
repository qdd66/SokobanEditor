using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public static class MapPersistenceSetup
    {
        const string ConfigPath = MapEditorPaths.PersistenceConfig;
        const string CatalogPath = MapEditorPaths.PlaceableCatalog;
        const string ToastChannelPath = MapEditorPaths.ToastChannel;
        static string PanelPrefabPath => MapEditorPaths.MapLoadPanelPrefab;
        static string FlyerPath => MapEditorPaths.FlyerPrefab;

        public static string Setup()
        {
            EnsureFolder(MapEditorPaths.UserDataRoot + "/MapPersistence");
            EnsureFolder(MapEditorPaths.UserPrefabRoot + "/UI");

            var filled = FillItemIds();
            var config = EnsureConfig();
            var panel = EnsurePanelPrefab();
            MapFileListUiSetup.UpgradePrefabs();
            WireFlyer(config, panel);
            WireScene();
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "地图存档已配置。Ctrl+S 保存，Ctrl+O 打开读取列表。已补全 " + filled + " 条物品存档编号。";
        }

        static int FillItemIds()
        {
            var guids = AssetDatabase.FindAssets("t:PlaceableItemDef");
            var filled = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(path);
                if (def == null)
                    continue;

                var so = new SerializedObject(def);
                var id = so.FindProperty("id");
                if (id == null || !string.IsNullOrEmpty(id.stringValue))
                    continue;

                id.stringValue = Path.GetFileNameWithoutExtension(path);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(def);
                filled++;
            }

            return filled;
        }

        static MapPersistenceConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<MapPersistenceConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<MapPersistenceConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            var so = new SerializedObject(config);
            so.FindProperty("toastChannel").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ToastChannel>(ToastChannelPath);
            so.FindProperty("defaultFileName").stringValue = "Map";
            so.FindProperty("useProjectMapsFolderInEditor").boolValue = true;
            so.FindProperty("maxUndoSteps").intValue = 32;
            so.FindProperty("saveCamera").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        static GameObject EnsurePanelPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            if (existing != null)
            {
                var view = existing.GetComponent<MapLoadPanelView>();
                if (view != null)
                    return existing;
            }

            var root = CreatePanelRoot();
            PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        }

        static GameObject CreatePanelRoot()
        {
            var font = UiTmpFontSetup.LoadProjectFont();
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            var root = new GameObject("MapLoadPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(MapLoadPanelView));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;
            UiTmpFontSetup.EnableTmpShaderChannels(canvas);

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var group = root.GetComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;

            var dimmer = CreateUi("Dimmer", root.transform);
            Stretch(dimmer);
            var dimmerImage = dimmer.AddComponent<Image>();
            dimmerImage.sprite = sprite;
            dimmerImage.color = new Color(0f, 0f, 0f, 0.55f);
            dimmerImage.raycastTarget = true;
            dimmer.AddComponent<MapLoadDimmerClick>().Bind(root.GetComponent<MapLoadPanelView>());

            var window = CreateUi("Window", root.transform);
            var windowRt = window.GetComponent<RectTransform>();
            windowRt.anchorMin = new Vector2(0.5f, 0.5f);
            windowRt.anchorMax = new Vector2(0.5f, 0.5f);
            windowRt.pivot = new Vector2(0.5f, 0.5f);
            windowRt.sizeDelta = new Vector2(760f, 640f);
            var windowImage = window.AddComponent<Image>();
            windowImage.sprite = sprite;
            windowImage.color = new Color(0.1f, 0.1f, 0.12f, 0.96f);
            windowImage.raycastTarget = true;

            var title = CreateText(window.transform, "Title", "读取地图", font, 32, TextAlignmentOptions.Center);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);
            titleRt.sizeDelta = new Vector2(-40f, 44f);

            var hint = CreateText(
                window.transform,
                "Hint",
                "点击一项加载 · 右侧可改名或删除 · Esc 关闭",
                font,
                16,
                TextAlignmentOptions.Center);
            hint.color = new Color(0.75f, 0.75f, 0.78f, 1f);
            var hintRt = hint.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 1f);
            hintRt.anchorMax = new Vector2(1f, 1f);
            hintRt.pivot = new Vector2(0.5f, 1f);
            hintRt.anchoredPosition = new Vector2(0f, -52f);
            hintRt.sizeDelta = new Vector2(-40f, 36f);
            hint.enableWordWrapping = true;
            hint.overflowMode = TextOverflowModes.Ellipsis;

            var empty = CreateText(window.transform, "Empty", "还没有保存过的地图", font, 22, TextAlignmentOptions.Center);
            empty.color = new Color(0.7f, 0.7f, 0.72f, 1f);
            var emptyRt = empty.GetComponent<RectTransform>();
            emptyRt.anchorMin = new Vector2(0f, 0.35f);
            emptyRt.anchorMax = new Vector2(1f, 0.65f);
            emptyRt.offsetMin = new Vector2(24f, 0f);
            emptyRt.offsetMax = new Vector2(-24f, 0f);
            empty.enableWordWrapping = true;

            var scroll = CreateUi("Scroll", window.transform);
            var scrollRt = scroll.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(24f, 24f);
            scrollRt.offsetMax = new Vector2(-24f, -96f);
            var scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            var viewport = CreateUi("Viewport", scroll.transform);
            Stretch(viewport);
            viewport.AddComponent<RectMask2D>();
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.sprite = sprite;
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;

            var content = CreateUi("Content", viewport.transform);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            contentLayout.spacing = 8f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRt;

            var templates = CreateUi("Templates", root.transform);
            templates.SetActive(false);
            var row = MapFileListUiSetup.CreateRow(templates.transform, font, sprite);

            var viewSo = new SerializedObject(root.GetComponent<MapLoadPanelView>());
            viewSo.FindProperty("rowTemplate").objectReferenceValue = row.GetComponent<MapLoadRowView>();
            viewSo.FindProperty("contentRoot").objectReferenceValue = contentRt;
            viewSo.FindProperty("emptyState").objectReferenceValue = empty.gameObject;
            viewSo.FindProperty("emptyLabel").objectReferenceValue = empty;
            viewSo.FindProperty("hint").objectReferenceValue = hint;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        static void WireFlyer(MapPersistenceConfig config, GameObject panelPrefab)
        {
            var contents = PrefabUtility.LoadPrefabContents(FlyerPath);
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(CatalogPath);
                var placement = contents.GetComponent<PlacementController>();
                var placedRoot = placement != null
                    ? new SerializedObject(placement).FindProperty("placedItemsRoot").objectReferenceValue as Transform
                    : null;

                var session = contents.GetComponent<MapEditSession>();
                if (session == null)
                    session = contents.AddComponent<MapEditSession>();

                var sessionSo = new SerializedObject(session);
                sessionSo.FindProperty("catalog").objectReferenceValue = catalog;
                sessionSo.FindProperty("placedItemsRoot").objectReferenceValue = placedRoot;
                sessionSo.FindProperty("config").objectReferenceValue = config;
                sessionSo.ApplyModifiedPropertiesWithoutUndo();

                if (placement != null)
                {
                    var pso = new SerializedObject(placement);
                    pso.FindProperty("editSession").objectReferenceValue = session;
                    pso.ApplyModifiedPropertiesWithoutUndo();
                }

                MapLoadPanelView panelView = contents.GetComponentInChildren<MapLoadPanelView>(true);
                if (panelView == null && panelPrefab != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, contents.transform);
                    instance.name = "MapLoadPanel";
                    instance.SetActive(false);
                    panelView = instance.GetComponent<MapLoadPanelView>();
                }
                else if (panelView != null)
                {
                    panelView.gameObject.SetActive(false);
                }

                var persistence = contents.GetComponent<MapPersistenceController>();
                if (persistence == null)
                    persistence = contents.AddComponent<MapPersistenceController>();

                var backpack = contents.GetComponentInChildren<BackpackPanelView>(true);
                var so = new SerializedObject(persistence);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("editSession").objectReferenceValue = session;
                so.FindProperty("fly").objectReferenceValue = contents.GetComponent<FirstPersonFlyController>();
                so.FindProperty("gameplayCursor").objectReferenceValue = contents.GetComponent<GameplayCursor>();
                so.FindProperty("placement").objectReferenceValue = placement;
                so.FindProperty("backpack").objectReferenceValue = backpack;
                so.FindProperty("loadPanel").objectReferenceValue = panelView;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, FlyerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void WireScene()
        {
            var placements = Object.FindObjectsByType<PlacementController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < placements.Length; i++)
            {
                var placement = placements[i];
                var placedRoot = new SerializedObject(placement).FindProperty("placedItemsRoot").objectReferenceValue as Transform;
                var session = placement.GetComponent<MapEditSession>();
                if (session == null)
                    continue;

                var so = new SerializedObject(session);
                if (placedRoot != null)
                    so.FindProperty("placedItemsRoot").objectReferenceValue = placedRoot;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(session);
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
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
