using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public static class PauseMenuSetup
    {
        const string PauseConfigPath = MapEditorPaths.PauseMenuConfig;
        const string PersistenceConfigPath = MapEditorPaths.PersistenceConfig;
        static string PrefabPath => MapEditorPaths.PauseMenuPrefab;
        static string FlyerPath => MapEditorPaths.FlyerPrefab;

        public static string Setup()
        {
            EnsureFolder(MapEditorPaths.UserDataRoot + "/PauseMenu");
            EnsureFolder(MapEditorPaths.UserPrefabRoot + "/UI");

            var config = EnsureConfig();
            var prefab = EnsurePrefab(config);
            MapFileListUiSetup.UpgradePrefabs();
            WireFlyer(config, prefab);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "暂停界面已配置。Esc 打开，可回到游戏、保存或加载场景。";
        }

        static PauseMenuConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PauseMenuConfig>(PauseConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PauseMenuConfig>();
                AssetDatabase.CreateAsset(config, PauseConfigPath);
            }

            var so = new SerializedObject(config);
            so.FindProperty("saveHint").stringValue =
                "点击已有存档覆盖 · 右侧可改名或删除 · 输入名称后建立新存档";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        static GameObject EnsurePrefab(PauseMenuConfig config)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null && existing.GetComponent<PauseMenuView>() != null)
                return existing;

            var root = CreateRoot(config);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        static GameObject CreateRoot(PauseMenuConfig config)
        {
            var font = UiTmpFontSetup.LoadProjectFont();
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            var root = new GameObject("PauseMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(PauseMenuView));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 205;
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
            dimmer.AddComponent<PauseMenuDimmerClick>().Bind(root.GetComponent<PauseMenuView>());

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

            var main = CreateMainPage(window.transform, font, sprite, config);
            var save = CreateSavePage(window.transform, font, sprite, config);

            var viewSo = new SerializedObject(root.GetComponent<PauseMenuView>());
            viewSo.FindProperty("mainPage").objectReferenceValue = main;
            viewSo.FindProperty("savePage").objectReferenceValue = save;
            viewSo.FindProperty("title").objectReferenceValue = main.transform.Find("Title").GetComponent<TMP_Text>();
            viewSo.FindProperty("hint").objectReferenceValue = main.transform.Find("Hint").GetComponent<TMP_Text>();
            viewSo.FindProperty("resumeButton").objectReferenceValue = main.transform.Find("Resume").GetComponent<Button>();
            viewSo.FindProperty("saveButton").objectReferenceValue = main.transform.Find("Save").GetComponent<Button>();
            viewSo.FindProperty("loadButton").objectReferenceValue = main.transform.Find("Load").GetComponent<Button>();
            viewSo.FindProperty("resumeLabel").objectReferenceValue =
                main.transform.Find("Resume").GetComponentInChildren<TMP_Text>();
            viewSo.FindProperty("saveLabel").objectReferenceValue =
                main.transform.Find("Save").GetComponentInChildren<TMP_Text>();
            viewSo.FindProperty("loadLabel").objectReferenceValue =
                main.transform.Find("Load").GetComponentInChildren<TMP_Text>();
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            save.SetActive(false);
            SettingsPanelSetup.AttachToPauseRoot(root, font, null);
            return root;
        }

        static GameObject CreateMainPage(Transform window, TMP_FontAsset font, Sprite sprite, PauseMenuConfig config)
        {
            var main = CreateUi("MainPage", window);
            Stretch(main);

            var title = CreateText(main.transform, "Title", config.Title, font, 36, TextAlignmentOptions.Center);
            PlaceTop(title.rectTransform, -28f, 48f);

            var hint = CreateText(main.transform, "Hint", config.Hint, font, 18, TextAlignmentOptions.Center);
            hint.color = new Color(0.75f, 0.75f, 0.78f, 1f);
            PlaceTop(hint.rectTransform, -76f, 28f);

            CreateMenuButton(main.transform, "Resume", config.ResumeLabel, font, sprite, 0);
            CreateMenuButton(main.transform, "Save", config.SaveLabel, font, sprite, 1);
            CreateMenuButton(main.transform, "Load", config.LoadLabel, font, sprite, 2);
            return main;
        }

        static GameObject CreateSavePage(Transform window, TMP_FontAsset font, Sprite sprite, PauseMenuConfig config)
        {
            var save = CreateUi("SavePage", window);
            Stretch(save);
            save.AddComponent<MapSavePanelView>();

            var title = CreateText(save.transform, "Title", config.SaveTitle, font, 32, TextAlignmentOptions.Center);
            PlaceTop(title.rectTransform, -16f, 44f);

            var hint = CreateText(save.transform, "Hint", config.SaveHint, font, 16, TextAlignmentOptions.Center);
            hint.color = new Color(0.75f, 0.75f, 0.78f, 1f);
            hint.enableWordWrapping = true;
            PlaceTop(hint.rectTransform, -56f, 32f);

            var empty = CreateText(save.transform, "Empty", "还没有保存过的地图", font, 20, TextAlignmentOptions.Center);
            empty.color = new Color(0.7f, 0.7f, 0.72f, 1f);
            var emptyRt = empty.rectTransform;
            emptyRt.anchorMin = new Vector2(0f, 0.42f);
            emptyRt.anchorMax = new Vector2(1f, 0.7f);
            emptyRt.offsetMin = new Vector2(24f, 0f);
            emptyRt.offsetMax = new Vector2(-24f, 0f);
            empty.enableWordWrapping = true;

            var scroll = CreateUi("Scroll", save.transform);
            var scrollRt = scroll.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(24f, 132f);
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

            var nameInput = CreateNameField(save.transform, font, sprite, config);
            var nameRt = nameInput.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0f);
            nameRt.anchorMax = new Vector2(1f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.anchoredPosition = new Vector2(-90f, 72f);
            nameRt.sizeDelta = new Vector2(-220f, 48f);

            var create = CreateSmallButton(save.transform, "Create", config.CreateLabel, font, sprite);
            var createRt = create.GetComponent<RectTransform>();
            createRt.anchorMin = new Vector2(1f, 0f);
            createRt.anchorMax = new Vector2(1f, 0f);
            createRt.pivot = new Vector2(1f, 0f);
            createRt.anchoredPosition = new Vector2(-24f, 72f);
            createRt.sizeDelta = new Vector2(168f, 48f);

            var back = CreateSmallButton(save.transform, "Back", config.BackLabel, font, sprite);
            var backRt = back.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0.5f, 0f);
            backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot = new Vector2(0.5f, 0f);
            backRt.anchoredPosition = new Vector2(0f, 16f);
            backRt.sizeDelta = new Vector2(200f, 44f);

            var templates = CreateUi("Templates", save.transform);
            templates.SetActive(false);
            var row = MapFileListUiSetup.CreateRow(templates.transform, font, sprite);

            var saveSo = new SerializedObject(save.GetComponent<MapSavePanelView>());
            saveSo.FindProperty("rowTemplate").objectReferenceValue = row.GetComponent<MapLoadRowView>();
            saveSo.FindProperty("contentRoot").objectReferenceValue = contentRt;
            saveSo.FindProperty("emptyState").objectReferenceValue = empty.gameObject;
            saveSo.FindProperty("emptyLabel").objectReferenceValue = empty;
            saveSo.FindProperty("title").objectReferenceValue = title;
            saveSo.FindProperty("hint").objectReferenceValue = hint;
            saveSo.FindProperty("nameField").objectReferenceValue = nameInput.GetComponent<TMP_InputField>();
            saveSo.FindProperty("createButton").objectReferenceValue = create.GetComponent<Button>();
            saveSo.FindProperty("backButton").objectReferenceValue = back.GetComponent<Button>();
            saveSo.FindProperty("createLabel").objectReferenceValue = create.GetComponentInChildren<TMP_Text>();
            saveSo.FindProperty("backLabel").objectReferenceValue = back.GetComponentInChildren<TMP_Text>();
            saveSo.ApplyModifiedPropertiesWithoutUndo();
            return save;
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
            rt.anchoredPosition = new Vector2(0f, 40f - index * 88f);
            rt.sizeDelta = new Vector2(420f, 72f);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.2f, 0.22f, 0.28f, 1f);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.75f, 0.88f, 1f, 1f);
            colors.pressedColor = new Color(0.6f, 0.72f, 0.9f, 1f);
            button.colors = colors;
            var text = CreateText(go.transform, "Label", label, font, 28, TextAlignmentOptions.Center);
            Stretch(text.gameObject);
            text.raycastTarget = false;
        }

        static GameObject CreateSmallButton(
            Transform parent,
            string name,
            string label,
            TMP_FontAsset font,
            Sprite sprite)
        {
            var go = CreateUi(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.2f, 0.22f, 0.28f, 1f);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.75f, 0.88f, 1f, 1f);
            button.colors = colors;
            var text = CreateText(go.transform, "Label", label, font, 20, TextAlignmentOptions.Center);
            Stretch(text.gameObject);
            text.raycastTarget = false;
            return go;
        }

        static GameObject CreateNameField(
            Transform parent,
            TMP_FontAsset font,
            Sprite sprite,
            PauseMenuConfig config)
        {
            var go = CreateUi("NameInput", parent);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.06f, 0.06f, 0.08f, 1f);
            var input = go.AddComponent<TMP_InputField>();

            var area = CreateUi("TextArea", go.transform);
            Stretch(area);
            var areaRt = area.GetComponent<RectTransform>();
            areaRt.offsetMin = new Vector2(12f, 6f);
            areaRt.offsetMax = new Vector2(-12f, -6f);
            area.AddComponent<RectMask2D>();

            var placeholder = CreateText(area.transform, "Placeholder", config.NamePlaceholder, font, 20, TextAlignmentOptions.Left);
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            Stretch(placeholder.gameObject);
            placeholder.raycastTarget = false;

            var text = CreateText(area.transform, "Text", string.Empty, font, 20, TextAlignmentOptions.Left);
            Stretch(text.gameObject);
            text.raycastTarget = false;

            input.textViewport = areaRt;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = font;
            input.pointSize = 20;
            input.caretColor = Color.white;
            input.customCaretColor = true;
            return go;
        }

        static void WireFlyer(PauseMenuConfig config, GameObject panelPrefab)
        {
            var contents = PrefabUtility.LoadPrefabContents(FlyerPath);
            try
            {
                PauseMenuView view = contents.GetComponentInChildren<PauseMenuView>(true);
                if (view == null && panelPrefab != null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, contents.transform);
                    instance.name = "PauseMenu";
                    instance.SetActive(false);
                    view = instance.GetComponent<PauseMenuView>();
                }
                else if (view != null)
                {
                    view.gameObject.SetActive(false);
                }

                var savePanel = view != null ? view.GetComponentInChildren<MapSavePanelView>(true) : null;
                var controller = contents.GetComponent<PauseMenuController>();
                if (controller == null)
                    controller = contents.AddComponent<PauseMenuController>();

                var persistence = contents.GetComponent<MapPersistenceController>();
                var so = new SerializedObject(controller);
                so.FindProperty("config").objectReferenceValue = config;
                so.FindProperty("persistenceConfig").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<MapPersistenceConfig>(PersistenceConfigPath);
                so.FindProperty("view").objectReferenceValue = view;
                so.FindProperty("savePanel").objectReferenceValue = savePanel;
                so.FindProperty("persistence").objectReferenceValue = persistence;
                so.FindProperty("gameplayCursor").objectReferenceValue = contents.GetComponent<GameplayCursor>();
                so.FindProperty("placement").objectReferenceValue = contents.GetComponent<PlacementController>();
                so.FindProperty("backpack").objectReferenceValue = contents.GetComponentInChildren<BackpackPanelView>(true);
                so.FindProperty("settingsConfig").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<SettingsPanelConfig>(SettingsPanelSetup.ConfigPath);
                so.ApplyModifiedPropertiesWithoutUndo();

                if (persistence != null)
                {
                    var pso = new SerializedObject(persistence);
                    pso.FindProperty("pauseMenu").objectReferenceValue = controller;
                    pso.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, FlyerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
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
