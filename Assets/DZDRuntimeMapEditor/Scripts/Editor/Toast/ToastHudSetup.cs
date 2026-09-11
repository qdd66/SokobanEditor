using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public static class ToastHudSetup
    {
        const string ChannelPath = MapEditorPaths.ToastChannel;
        const string HudConfigPath = MapEditorPaths.ToastHudConfig;
        static string PrefabPath => MapEditorPaths.ToastHudPrefab;
        const string PlacementConfigPath = MapEditorPaths.PlacementConfig;
        static string FlyerPath => MapEditorPaths.FlyerPrefab;

        public static string Setup()
        {
            EnsureFolder(MapEditorPaths.UserDataRoot + "/Toast");
            EnsureFolder(MapEditorPaths.UserPrefabRoot + "/UI");

            var channel = EnsureChannel();
            var hudConfig = EnsureHudConfig();
            WirePlacementConfig(channel);
            var prefab = EnsurePrefab(channel, hudConfig);
            WireFlyer(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "提示条已配置。";
        }

        static ToastChannel EnsureChannel()
        {
            var channel = AssetDatabase.LoadAssetAtPath<ToastChannel>(ChannelPath);
            if (channel != null)
                return channel;

            channel = ScriptableObject.CreateInstance<ToastChannel>();
            AssetDatabase.CreateAsset(channel, ChannelPath);
            return channel;
        }

        static ToastHudConfig EnsureHudConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ToastHudConfig>(HudConfigPath);
            if (config != null)
                return config;

            config = ScriptableObject.CreateInstance<ToastHudConfig>();
            AssetDatabase.CreateAsset(config, HudConfigPath);
            return config;
        }

        static void WirePlacementConfig(ToastChannel channel)
        {
            var config = AssetDatabase.LoadAssetAtPath<PlacementConfig>(PlacementConfigPath);
            if (config == null)
                return;

            var so = new SerializedObject(config);
            so.FindProperty("toastChannel").objectReferenceValue = channel;
            so.FindProperty("heightAdjustOnHint").stringValue = "高度调节：开";
            so.FindProperty("heightAdjustOffHint").stringValue = "高度调节：关";
            so.FindProperty("continuousOnHint").stringValue = "连续摆放：开";
            so.FindProperty("continuousOffHint").stringValue = "连续摆放：关";
            so.FindProperty("snapToSurfaceOnHint").stringValue = "贴合表面：开";
            so.FindProperty("snapToSurfaceOffHint").stringValue = "贴合表面：关";
            so.FindProperty("rotateAdjustOnHint.english").stringValue = "Rotate Adjust: On";
            so.FindProperty("rotateAdjustOnHint.chinese").stringValue = "旋转调节：开";
            so.FindProperty("rotateAdjustOffHint.english").stringValue = "Rotate Adjust: Off";
            so.FindProperty("rotateAdjustOffHint.chinese").stringValue = "旋转调节：关";
            so.FindProperty("gridPlaceOnHint.english").stringValue = "Grid Place: On";
            so.FindProperty("gridPlaceOnHint.chinese").stringValue = "网格放置：开";
            so.FindProperty("gridPlaceOffHint.english").stringValue = "Grid Place: Off";
            so.FindProperty("gridPlaceOffHint.chinese").stringValue = "网格放置：关";
            so.FindProperty("gridOccupiedHint.english").stringValue = "Cell occupied";
            so.FindProperty("gridOccupiedHint.chinese").stringValue = "此格已有物品";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        static GameObject EnsurePrefab(ToastChannel channel, ToastHudConfig hudConfig)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
            {
                WireView(existing, channel, hudConfig);
                return existing;
            }

            var root = CreateHudRoot();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            WireView(prefab, channel, hudConfig);
            return prefab;
        }

        static void WireView(GameObject prefab, ToastChannel channel, ToastHudConfig hudConfig)
        {
            var path = prefab != null ? AssetDatabase.GetAssetPath(prefab) : PrefabPath;
            if (string.IsNullOrEmpty(path))
                path = PrefabPath;
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view = contents.GetComponent<ToastHudView>();
                if (view == null)
                    return;

                var so = new SerializedObject(view);
                so.FindProperty("channel").objectReferenceValue = channel;
                so.FindProperty("config").objectReferenceValue = hudConfig;
                var label = contents.GetComponentInChildren<TextMeshProUGUI>(true);
                var group = contents.GetComponent<CanvasGroup>();
                if (label != null)
                    so.FindProperty("label").objectReferenceValue = label;
                if (group != null)
                    so.FindProperty("group").objectReferenceValue = group;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static GameObject CreateHudRoot()
        {
            var font = UiTmpFontSetup.LoadProjectFont();
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");

            var root = new GameObject("ToastHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(CanvasGroup), typeof(ToastHudView));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 150;
            UiTmpFontSetup.EnableTmpShaderChannels(canvas);

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var banner = new GameObject("Banner", typeof(RectTransform));
            banner.transform.SetParent(root.transform, false);
            var bannerRt = banner.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 1f);
            bannerRt.anchorMax = new Vector2(0.5f, 1f);
            bannerRt.pivot = new Vector2(0.5f, 1f);
            bannerRt.anchoredPosition = new Vector2(0f, -36f);
            bannerRt.sizeDelta = new Vector2(720f, 56f);
            var bannerImage = banner.AddComponent<Image>();
            bannerImage.sprite = sprite;
            bannerImage.color = new Color(0.08f, 0.08f, 0.1f, 0.72f);
            bannerImage.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(banner.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(16f, 4f);
            labelRt.offsetMax = new Vector2(-16f, -4f);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 28;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.text = string.Empty;

            var viewSo = new SerializedObject(root.GetComponent<ToastHudView>());
            viewSo.FindProperty("label").objectReferenceValue = label;
            viewSo.FindProperty("group").objectReferenceValue = group;
            viewSo.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        static void WireFlyer(GameObject prefab)
        {
            if (prefab == null)
                return;

            var contents = PrefabUtility.LoadPrefabContents(FlyerPath);
            try
            {
                var existing = contents.GetComponentInChildren<ToastHudView>(true);
                if (existing == null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, contents.transform);
                    instance.name = "ToastHud";
                    instance.SetActive(true);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, FlyerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
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
