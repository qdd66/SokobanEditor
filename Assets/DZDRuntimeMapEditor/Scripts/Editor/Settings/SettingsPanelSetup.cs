using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public static class SettingsPanelSetup
    {
        public const string ConfigPath = MapEditorPaths.SettingsPanelConfig;
        public const string BgTexturePath = MapEditorPaths.OptionalPauseBackground;
        static string PausePrefabPath => MapEditorPaths.PauseMenuPrefab;
        static string FlyerPath => MapEditorPaths.FlyerPrefab;
        const string FlyConfigPath = MapEditorPaths.FlyConfig;
        const string PlacementConfigPath = MapEditorPaths.PlacementConfig;
        const string PauseConfigPath = MapEditorPaths.PauseMenuConfig;
        const string PersistenceConfigPath = MapEditorPaths.PersistenceConfig;
        const string LocaleSettingsPath = MapEditorPaths.LocaleSettings;
        const string ToastChannelPath = MapEditorPaths.ToastChannel;

        static readonly Color Ink = new Color(0.92f, 0.93f, 0.95f, 1f);
        static readonly Color Muted = new Color(0.68f, 0.7f, 0.74f, 1f);
        static readonly Color Conflict = new Color(1f, 0.48f, 0.42f, 1f);
        static readonly Color Panel = new Color(0.08f, 0.08f, 0.1f, 0.97f);
        static readonly Color TabOff = new Color(0.16f, 0.16f, 0.18f, 1f);
        static readonly Color TabOn = new Color(0.28f, 0.28f, 0.32f, 1f);
        static readonly Color Row = new Color(0.14f, 0.14f, 0.16f, 0.94f);
        static readonly Color Highlight = new Color(0.38f, 0.38f, 0.42f, 1f);
        static readonly Color Pressed = new Color(0.22f, 0.22f, 0.24f, 1f);

        public static string Setup()
        {
            EnsureFolder(MapEditorPaths.UserDataRoot + "/Settings");
            EnsureFolder(MapEditorPaths.UserPrefabRoot + "/UI");

            var config = EnsureConfig();
            var pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath);
            if (pausePrefab == null)
                PauseMenuSetup.Setup();

            PatchPausePrefab(config);
            WireFlyer(config);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "设置面板已配置。暂停菜单中打开「设置」，可在运行时改 SO。";
        }

        public static Sprite LoadBg0()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(BgTexturePath);
            for (var i = 0; i < assets.Length; i++)
            {
                var sprite = assets[i] as Sprite;
                if (sprite != null && sprite.name == "BG_0")
                    return sprite;
            }

            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        }

        public static void AttachToPauseRoot(GameObject pauseRoot, TMP_FontAsset font, SettingsPanelConfig config)
        {
            if (pauseRoot == null || font == null)
                return;
            if (config == null)
                config = EnsureConfig();
            var bg = config.PanelBackground != null ? config.PanelBackground : LoadBg0();
            EnsureSettingsButton(pauseRoot.transform, font, config.PauseMenuConfig);
            EnsureSettingsPage(pauseRoot.transform, font, bg, config);
            WirePauseView(pauseRoot);
        }

        static SettingsPanelConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<SettingsPanelConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SettingsPanelConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            var so = new SerializedObject(config);
            so.FindProperty("flyConfig").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<FirstPersonFlyConfig>(FlyConfigPath);
            so.FindProperty("placementConfig").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<PlacementConfig>(PlacementConfigPath);
            so.FindProperty("pauseMenuConfig").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<PauseMenuConfig>(PauseConfigPath);
            so.FindProperty("persistenceConfig").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<MapPersistenceConfig>(PersistenceConfigPath);
            so.FindProperty("localeSettings").objectReferenceValue = EnsureLocaleSettings();
            if (so.FindProperty("panelBackground").objectReferenceValue == null)
                so.FindProperty("panelBackground").objectReferenceValue = LoadBg0();
            so.FindProperty("toastChannel").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ToastChannel>(ToastChannelPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        static MapEditorLocaleSettings EnsureLocaleSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapEditorLocaleSettings>(LocaleSettingsPath);
            if (settings != null)
                return settings;

            settings = ScriptableObject.CreateInstance<MapEditorLocaleSettings>();
            AssetDatabase.CreateAsset(settings, LocaleSettingsPath);
            return settings;
        }

        static void PatchPausePrefab(SettingsPanelConfig config)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PausePrefabPath);
            if (prefab == null)
                return;

            var contents = PrefabUtility.LoadPrefabContents(PausePrefabPath);
            try
            {
                var font = UiTmpFontSetup.LoadProjectFont();
                AttachToPauseRoot(contents, font, config);
                PrefabUtility.SaveAsPrefabAsset(contents, PausePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void EnsureSettingsButton(Transform pauseRoot, TMP_FontAsset font, PauseMenuConfig pauseConfig)
        {
            var main = pauseRoot.Find("Window/MainPage");
            if (main == null)
                return;
            if (main.Find("Settings") != null)
                return;

            Sprite sprite = null;
            var load = main.Find("Load");
            if (load != null)
            {
                var image = load.GetComponent<Image>();
                if (image != null)
                    sprite = image.sprite;
            }

            if (sprite == null)
                sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            var label = pauseConfig != null ? pauseConfig.SettingsLabel : "设置";
            var go = CreateUi("Settings", main);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 40f - 3 * 88f);
            rt.sizeDelta = new Vector2(420f, 72f);
            var imageGo = go.AddComponent<Image>();
            imageGo.sprite = sprite;
            imageGo.color = new Color(0.2f, 0.22f, 0.28f, 1f);
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.75f, 0.88f, 1f, 1f);
            colors.pressedColor = new Color(0.6f, 0.72f, 0.9f, 1f);
            button.colors = colors;
            var text = CreateText(go.transform, "Label", label, font, 28, TextAlignmentOptions.Center, Color.white);
            Stretch(text.gameObject);
            text.raycastTarget = false;
        }

        static GameObject EnsureSettingsPage(
            Transform pauseRoot,
            TMP_FontAsset font,
            Sprite bg,
            SettingsPanelConfig config)
        {
            var existing = pauseRoot.Find("SettingsPage");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            return CreateSettingsPage(pauseRoot, font, bg, config);
        }

        static GameObject CreateSettingsPage(
            Transform pauseRoot,
            TMP_FontAsset font,
            Sprite bg,
            SettingsPanelConfig config)
        {
            var page = CreateUi("SettingsPage", pauseRoot);
            page.AddComponent<SettingsPanelView>();
            var pageRt = page.GetComponent<RectTransform>();
            Stretch(page);
            pageRt.SetAsLastSibling();

            var window = CreateUi("Window", page.transform);
            Stretch(window);
            var windowImage = window.AddComponent<Image>();
            windowImage.sprite = bg;
            windowImage.color = Panel;
            windowImage.raycastTarget = true;

            var title = CreateText(window.transform, "Title", config.Title, font, 34, TextAlignmentOptions.Center, Ink);
            PlaceTop(title.rectTransform, -18f, 44f);

            var hint = CreateText(window.transform, "Hint", config.Hint, font, 16, TextAlignmentOptions.Center, Muted);
            hint.enableWordWrapping = true;
            PlaceTop(hint.rectTransform, -58f, 28f);

            var tabs = CreateUi("Tabs", window.transform);
            var tabsRt = tabs.GetComponent<RectTransform>();
            tabsRt.anchorMin = new Vector2(0f, 1f);
            tabsRt.anchorMax = new Vector2(1f, 1f);
            tabsRt.pivot = new Vector2(0.5f, 1f);
            tabsRt.anchoredPosition = new Vector2(0f, -92f);
            tabsRt.sizeDelta = new Vector2(-96f, 52f);
            var tabsLayout = tabs.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 10f;
            tabsLayout.childAlignment = TextAnchor.MiddleCenter;
            tabsLayout.childControlWidth = true;
            tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.childForceExpandHeight = true;

            var controlTab = CreateTab(tabs.transform, "ControlTab", config.ControlTab, font, bg, true);
            var characterTab = CreateTab(tabs.transform, "CharacterTab", config.CharacterTab, font, bg, false);
            var placementTab = CreateTab(tabs.transform, "PlacementTab", config.PlacementTab, font, bg, false);
            var sokobanTab = CreateTab(tabs.transform, "SokobanTab", config.SokobanTab, font, bg, false);

            var scroll = CreateUi("Scroll", window.transform);
            var scrollRt = scroll.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(48f, 80f);
            scrollRt.offsetMax = new Vector2(-48f, -156f);
            var scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;

            var viewport = CreateUi("Viewport", scroll.transform);
            Stretch(viewport);
            viewport.AddComponent<RectMask2D>();
            var viewportImage = viewport.AddComponent<Image>();
            viewportImage.sprite = bg;
            viewportImage.color = new Color(0.05f, 0.05f, 0.06f, 0.55f);
            viewportImage.raycastTarget = true;

            var content = CreateUi("Content", viewport.transform);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
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

            var back = CreateTab(window.transform, "Back", config.BackLabel, font, bg, false);
            var backRt = back.GetComponent<RectTransform>();
            Object.DestroyImmediate(back.GetComponent<LayoutElement>());
            backRt.anchorMin = new Vector2(0.5f, 0f);
            backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot = new Vector2(0.5f, 0f);
            backRt.anchoredPosition = new Vector2(0f, 16f);
            backRt.sizeDelta = new Vector2(220f, 44f);

            var templates = CreateUi("Templates", window.transform);
            templates.SetActive(false);
            var group = CreateText(templates.transform, "GroupHeader", "分组", font, 20, TextAlignmentOptions.Left, Ink);
            var groupElement = group.gameObject.AddComponent<LayoutElement>();
            groupElement.minHeight = 32f;
            groupElement.preferredHeight = 32f;
            var row = CreateRow(templates.transform, font, bg);

            var viewSo = new SerializedObject(page.GetComponent<SettingsPanelView>());
            viewSo.FindProperty("title").objectReferenceValue = title;
            viewSo.FindProperty("hint").objectReferenceValue = hint;
            viewSo.FindProperty("backButton").objectReferenceValue = back.GetComponent<Button>();
            viewSo.FindProperty("backLabel").objectReferenceValue = back.GetComponentInChildren<TMP_Text>();
            viewSo.FindProperty("controlTab").objectReferenceValue = controlTab.GetComponent<Button>();
            viewSo.FindProperty("characterTab").objectReferenceValue = characterTab.GetComponent<Button>();
            viewSo.FindProperty("placementTab").objectReferenceValue = placementTab.GetComponent<Button>();
            viewSo.FindProperty("sokobanTab").objectReferenceValue = sokobanTab.GetComponent<Button>();
            viewSo.FindProperty("controlTabLabel").objectReferenceValue = controlTab.GetComponentInChildren<TMP_Text>();
            viewSo.FindProperty("characterTabLabel").objectReferenceValue = characterTab.GetComponentInChildren<TMP_Text>();
            viewSo.FindProperty("placementTabLabel").objectReferenceValue = placementTab.GetComponentInChildren<TMP_Text>();
            viewSo.FindProperty("sokobanTabLabel").objectReferenceValue = sokobanTab.GetComponentInChildren<TMP_Text>();
            viewSo.FindProperty("contentRoot").objectReferenceValue = contentRt;
            viewSo.FindProperty("rowTemplate").objectReferenceValue = row.GetComponent<SettingsSettingRowView>();
            viewSo.FindProperty("groupTemplate").objectReferenceValue = group;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            page.SetActive(false);
            return page;
        }

        static GameObject CreateTab(
            Transform parent,
            string name,
            string label,
            TMP_FontAsset font,
            Sprite sprite,
            bool selected)
        {
            var go = CreateUi(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = selected ? TabOn : TabOff;
            var button = go.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = Highlight;
            colors.pressedColor = Pressed;
            button.colors = colors;
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = 44f;
            element.preferredHeight = 44f;
            var text = CreateText(go.transform, "Label", label, font, 20, TextAlignmentOptions.Center, Ink);
            Stretch(text.gameObject);
            text.raycastTarget = false;
            return go;
        }

        static GameObject CreateRow(Transform parent, TMP_FontAsset font, Sprite sprite)
        {
            var row = CreateUi("SettingRow", parent);
            var rowImage = row.AddComponent<Image>();
            rowImage.sprite = sprite;
            rowImage.color = Row;
            rowImage.raycastTarget = true;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            var rowElement = row.AddComponent<LayoutElement>();
            rowElement.minHeight = 52f;
            rowElement.preferredHeight = 52f;
            row.AddComponent<SettingsSettingRowView>();

            var label = CreateText(row.transform, "Label", "名称", font, 18, TextAlignmentOptions.Left, Ink);
            var labelElement = label.gameObject.AddComponent<LayoutElement>();
            labelElement.minWidth = 200f;
            labelElement.preferredWidth = 240f;
            labelElement.flexibleWidth = 0f;

            var slider = CreateSlider(row.transform, sprite);
            var sliderElement = slider.gameObject.AddComponent<LayoutElement>();
            sliderElement.minWidth = 160f;
            sliderElement.flexibleWidth = 1f;

            var value = CreateText(row.transform, "Value", "0", font, 16, TextAlignmentOptions.Right, Ink);
            var valueElement = value.gameObject.AddComponent<LayoutElement>();
            valueElement.minWidth = 90f;
            valueElement.preferredWidth = 120f;
            valueElement.flexibleWidth = 0f;

            var toggle = CreateToggle(row.transform, sprite);
            var toggleElement = toggle.gameObject.AddComponent<LayoutElement>();
            toggleElement.minWidth = 36f;
            toggleElement.preferredWidth = 36f;

            var key = CreateTab(row.transform, "Key", "W", font, sprite, false);
            var keyElement = key.GetComponent<LayoutElement>();
            keyElement.minWidth = 160f;
            keyElement.preferredWidth = 180f;
            keyElement.flexibleWidth = 0f;
            key.GetComponentInChildren<TMP_Text>().fontSize = 18;

            var conflict = CreateText(row.transform, "Conflict", string.Empty, font, 14, TextAlignmentOptions.Left, Conflict);
            var conflictElement = conflict.gameObject.AddComponent<LayoutElement>();
            conflictElement.minWidth = 80f;
            conflictElement.flexibleWidth = 1f;
            conflict.enableWordWrapping = false;
            conflict.overflowMode = TextOverflowModes.Ellipsis;

            var enumButton = CreateTab(row.transform, "Enum", "选项", font, sprite, false);
            var enumElement = enumButton.GetComponent<LayoutElement>();
            enumElement.minWidth = 200f;
            enumElement.preferredWidth = 260f;
            enumElement.flexibleWidth = 0f;
            enumButton.GetComponentInChildren<TMP_Text>().fontSize = 18;

            var rowSo = new SerializedObject(row.GetComponent<SettingsSettingRowView>());
            rowSo.FindProperty("label").objectReferenceValue = label;
            rowSo.FindProperty("slider").objectReferenceValue = slider;
            rowSo.FindProperty("valueLabel").objectReferenceValue = value;
            rowSo.FindProperty("toggle").objectReferenceValue = toggle;
            rowSo.FindProperty("keyButton").objectReferenceValue = key.GetComponent<Button>();
            rowSo.FindProperty("keyLabel").objectReferenceValue = key.GetComponentInChildren<TMP_Text>();
            rowSo.FindProperty("conflictLabel").objectReferenceValue = conflict;
            rowSo.FindProperty("enumButton").objectReferenceValue = enumButton.GetComponent<Button>();
            rowSo.FindProperty("enumLabel").objectReferenceValue = enumButton.GetComponentInChildren<TMP_Text>();
            rowSo.ApplyModifiedPropertiesWithoutUndo();
            return row;
        }

        static Slider CreateSlider(Transform parent, Sprite sprite)
        {
            var go = CreateUi("Slider", parent);
            var slider = go.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;

            var background = CreateUi("Background", go.transform);
            Stretch(background);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = sprite;
            backgroundImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            var fillArea = CreateUi("Fill Area", go.transform);
            Stretch(fillArea);
            var fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.offsetMin = new Vector2(6f, 8f);
            fillAreaRt.offsetMax = new Vector2(-6f, -8f);

            var fill = CreateUi("Fill", fillArea.transform);
            Stretch(fill);
            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = sprite;
            fillImage.color = new Color(0.48f, 0.48f, 0.52f, 1f);

            var handleArea = CreateUi("Handle Slide Area", go.transform);
            Stretch(handleArea);
            var handleAreaRt = handleArea.GetComponent<RectTransform>();
            handleAreaRt.offsetMin = new Vector2(8f, 4f);
            handleAreaRt.offsetMax = new Vector2(-8f, -4f);

            var handle = CreateUi("Handle", handleArea.transform);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.anchorMin = new Vector2(0f, 0f);
            handleRt.anchorMax = new Vector2(0f, 1f);
            handleRt.pivot = new Vector2(0.5f, 0.5f);
            handleRt.sizeDelta = new Vector2(18f, 0f);
            handleRt.anchoredPosition = Vector2.zero;
            var handleImage = handle.AddComponent<Image>();
            handleImage.sprite = sprite;
            handleImage.color = new Color(0.78f, 0.78f, 0.82f, 1f);

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImage;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        static Toggle CreateToggle(Transform parent, Sprite sprite)
        {
            var go = CreateUi("Toggle", parent);
            var toggle = go.AddComponent<Toggle>();

            var background = CreateUi("Background", go.transform);
            Stretch(background);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = sprite;
            backgroundImage.color = new Color(0.12f, 0.12f, 0.14f, 1f);

            var check = CreateUi("Checkmark", background.transform);
            Stretch(check);
            var checkRt = check.GetComponent<RectTransform>();
            checkRt.offsetMin = new Vector2(6f, 6f);
            checkRt.offsetMax = new Vector2(-6f, -6f);
            var checkImage = check.AddComponent<Image>();
            checkImage.sprite = sprite;
            checkImage.color = new Color(0.78f, 0.78f, 0.82f, 1f);

            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkImage;
            toggle.isOn = false;
            return toggle;
        }

        static void WirePauseView(GameObject pauseRoot)
        {
            var view = pauseRoot.GetComponent<PauseMenuView>();
            if (view == null)
                return;

            var window = pauseRoot.transform.Find("Window");
            var main = window != null ? window.Find("MainPage") : null;
            var settingsButton = main != null ? main.Find("Settings") : null;
            var settingsPage = pauseRoot.transform.Find("SettingsPage");
            var so = new SerializedObject(view);
            if (settingsButton != null)
            {
                so.FindProperty("settingsButton").objectReferenceValue = settingsButton.GetComponent<Button>();
                so.FindProperty("settingsLabel").objectReferenceValue = settingsButton.GetComponentInChildren<TMP_Text>();
            }

            so.FindProperty("menuWindow").objectReferenceValue = window != null ? window.gameObject : null;
            so.FindProperty("settingsPage").objectReferenceValue =
                settingsPage != null ? settingsPage.gameObject : null;
            so.FindProperty("settingsView").objectReferenceValue =
                settingsPage != null ? settingsPage.GetComponent<SettingsPanelView>() : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireFlyer(SettingsPanelConfig config)
        {
            var contents = PrefabUtility.LoadPrefabContents(FlyerPath);
            try
            {
                var controller = contents.GetComponent<PauseMenuController>();
                if (controller == null)
                    return;

                var so = new SerializedObject(controller);
                so.FindProperty("settingsConfig").objectReferenceValue = config;
                so.ApplyModifiedPropertiesWithoutUndo();
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
            TextAlignmentOptions alignment,
            Color color)
        {
            var go = CreateUi(name, parent);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
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
