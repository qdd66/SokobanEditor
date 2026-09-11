using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public static class BackpackPanelSetup
    {
        const string CategoriesRoot = PlaceablePrefabImporter.CategoriesRoot;
        const string ForestPath = CategoriesRoot + "/Forest.asset";
        const string MushroomPath = CategoriesRoot + "/Mushroom.asset";
        const string UncategorizedPath = PlaceablePrefabImporter.UncategorizedAssetPath;
        const string CatalogPath = MapEditorPaths.PlaceableCatalog;
        const string PlacementConfigPath = MapEditorPaths.PlacementConfig;
        static string SlotPrefabPath => MapEditorPaths.WarehouseSlotPrefab;
        static string PanelPrefabPath => MapEditorPaths.BackpackPanelPrefab;
        static string FlyerPath => MapEditorPaths.FlyerPrefab;

        public static string Setup()
        {
            EnsureFolder(CategoriesRoot);
            EnsureFolder(MapEditorPaths.UserPrefabRoot + "/Placement");

            var forest = EnsureCategory(ForestPath, "森林", 10, "森林");
            var mushroom = EnsureCategory(MushroomPath, "蘑菇", 20, "蘑菇");
            var uncategorized = EnsureCategory(UncategorizedPath, "未分类", 1000, string.Empty);
            WirePlacementConfig();
            var assigned = PlaceablePrefabImporter.AssignCategoriesFromFolders();
            var panelPrefab = EnsureBackpackPrefab(uncategorized);
            WireFlyer(panelPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return $"背包面板已配置。分类：{forest.name}/{mushroom.name}/{uncategorized.name}。已写入 {assigned} 条物品定义。";
        }

        static PlaceableCategoryDef EnsureCategory(string path, string displayName, int sortOrder, string folderName)
        {
            var category = AssetDatabase.LoadAssetAtPath<PlaceableCategoryDef>(path);
            if (category == null)
            {
                category = ScriptableObject.CreateInstance<PlaceableCategoryDef>();
                AssetDatabase.CreateAsset(category, path);
            }

            var so = new SerializedObject(category);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("sortOrder").intValue = sortOrder;
            so.FindProperty("prefabFolderName").stringValue = folderName;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(category);
            return category;
        }

        static void WirePlacementConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlacementConfig>(PlacementConfigPath);
            if (config == null)
                return;

            var so = new SerializedObject(config);
            so.FindProperty("toggleBackpackKey").intValue = (int)Key.B;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        static BackpackPanelView EnsureBackpackPrefab(PlaceableCategoryDef uncategorized)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            if (existing != null)
            {
                var view = existing.GetComponent<BackpackPanelView>();
                if (view != null)
                {
                    WirePanelView(existing, uncategorized);
                    return view;
                }
            }

            var root = CreatePanelRoot();
            PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
            Object.DestroyImmediate(root);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
            WirePanelView(prefab, uncategorized);
            return prefab.GetComponent<BackpackPanelView>();
        }

        static void WirePanelView(GameObject prefab, PlaceableCategoryDef uncategorized)
        {
            var so = new SerializedObject(prefab.GetComponent<BackpackPanelView>());
            so.FindProperty("catalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(CatalogPath);
            so.FindProperty("uncategorized").objectReferenceValue = uncategorized;
            so.FindProperty("slotPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<WarehouseSlotView>(SlotPrefabPath);
            so.FindProperty("columns").intValue = 8;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
        }

        static GameObject CreatePanelRoot()
        {
            var font = UiTmpFontSetup.LoadProjectFont();
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            var root = new GameObject("BackpackPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(BackpackPanelView));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
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
            dimmer.AddComponent<BackpackDimmerClick>().Bind(root.GetComponent<BackpackPanelView>());

            var window = CreateUi("Window", root.transform);
            var windowRt = window.GetComponent<RectTransform>();
            windowRt.anchorMin = new Vector2(0.5f, 0.5f);
            windowRt.anchorMax = new Vector2(0.5f, 0.5f);
            windowRt.pivot = new Vector2(0.5f, 0.5f);
            windowRt.sizeDelta = new Vector2(980f, 720f);
            var windowImage = window.AddComponent<Image>();
            windowImage.sprite = sprite;
            windowImage.color = new Color(0.1f, 0.1f, 0.12f, 0.96f);
            windowImage.raycastTarget = true;

            var title = CreateText(window.transform, "Title", "背包", font, 32, TextAlignmentOptions.Center);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -12f);
            titleRt.sizeDelta = new Vector2(-40f, 44f);

            var hint = CreateText(window.transform, "Hint", "点击物品取出 · B 关闭", font, 16, TextAlignmentOptions.Center);
            hint.color = new Color(0.75f, 0.75f, 0.78f, 1f);
            var hintRt = hint.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 1f);
            hintRt.anchorMax = new Vector2(1f, 1f);
            hintRt.pivot = new Vector2(0.5f, 1f);
            hintRt.anchoredPosition = new Vector2(0f, -52f);
            hintRt.sizeDelta = new Vector2(-40f, 28f);

            var scroll = CreateUi("Scroll", window.transform);
            var scrollRt = scroll.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(24f, 24f);
            scrollRt.offsetMax = new Vector2(-24f, -88f);
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
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.spacing = 18f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
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
            var section = CreateCategorySection(templates.transform, font);

            var viewSo = new SerializedObject(root.GetComponent<BackpackPanelView>());
            viewSo.FindProperty("sectionTemplate").objectReferenceValue = section.GetComponent<BackpackCategorySectionView>();
            viewSo.FindProperty("contentRoot").objectReferenceValue = contentRt;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        static GameObject CreateCategorySection(Transform parent, TMP_FontAsset font)
        {
            var section = CreateUi("CategorySection", parent);
            section.AddComponent<BackpackCategorySectionView>();
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 4);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = section.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var header = CreateText(section.transform, "Header", "分类", font, 22, TextAlignmentOptions.Left);
            header.color = new Color(1f, 0.85f, 0.35f, 1f);
            var headerElement = header.gameObject.AddComponent<LayoutElement>();
            headerElement.minHeight = 32f;
            headerElement.preferredHeight = 32f;

            var items = CreateUi("ItemsRoot", section.transform);
            var grid = items.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(80f, 96f);
            grid.spacing = new Vector2(10f, 10f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            var itemsFitter = items.AddComponent<ContentSizeFitter>();
            itemsFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            itemsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sectionSo = new SerializedObject(section.GetComponent<BackpackCategorySectionView>());
            sectionSo.FindProperty("header").objectReferenceValue = header;
            sectionSo.FindProperty("itemsRoot").objectReferenceValue = items.GetComponent<RectTransform>();
            sectionSo.ApplyModifiedPropertiesWithoutUndo();
            return section;
        }

        static void WireFlyer(BackpackPanelView panelPrefab)
        {
            if (panelPrefab == null)
                return;

            var contents = PrefabUtility.LoadPrefabContents(FlyerPath);
            try
            {
                EnsureEventSystem(contents.transform);

                var existing = contents.GetComponentInChildren<BackpackPanelView>(true);
                if (existing == null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab.gameObject, contents.transform);
                    instance.name = "BackpackPanel";
                    instance.SetActive(false);
                    existing = instance.GetComponent<BackpackPanelView>();
                }
                else
                {
                    existing.gameObject.SetActive(false);
                }

                var controller = contents.GetComponent<PlacementController>();
                var so = new SerializedObject(controller);
                so.FindProperty("backpack").objectReferenceValue = existing;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, FlyerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void EnsureEventSystem(Transform flyer)
        {
            var existing = flyer.GetComponentInChildren<EventSystem>(true);
            if (existing != null)
            {
                var standalone = existing.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                    Object.DestroyImmediate(standalone);

                var module = existing.GetComponent<InputSystemUIInputModule>();
                if (module == null)
                    module = existing.gameObject.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
                return;
            }

            var go = new GameObject("EventSystem");
            go.transform.SetParent(flyer, false);
            go.AddComponent<EventSystem>();
            var inputModule = go.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
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
