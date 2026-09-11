using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public static class UiTmpFontSetup
    {
        public const string FontAssetPath = MapEditorPaths.LegacyChineseTmpFont;
        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        static string[] UiPrefabPaths => new[]
        {
            MapEditorPaths.WarehouseSlotPrefab,
            MapEditorPaths.WarehouseHotbarPrefab,
            MapEditorPaths.BackpackPanelPrefab,
            MapEditorPaths.ToastHudPrefab,
            MapEditorPaths.MapLoadPanelPrefab,
            MapEditorPaths.PauseMenuPrefab,
            MapEditorPaths.StartMenuPrefab,
            MapEditorPaths.FlyerPrefab
        };

        public static string Apply()
        {
            var font = LoadProjectFont();
            if (font == null)
                return "未找到 TMP 字体。请导入 TextMesh Pro 的 Liberation Sans，或把字体放到 " + FontAssetPath;

            ApplyDefaultTmpFont(font);
            var converted = 0;
            var paths = UiPrefabPaths;
            for (var i = 0; i < paths.Length; i++)
                converted += ConvertPrefab(paths[i], font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return "已将 UI 转为 TMP。转换文字 " + converted + " 处。";
        }

        public static string ApplySettingsFont()
        {
            var font = LoadProjectFont();
            if (font == null)
                return "找不到字制区喜脉体：" + FontAssetPath;

            var path = MapEditorPaths.PauseMenuPrefab;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return "找不到暂停菜单预制体。";

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var count = 0;
                var view = contents.GetComponentInChildren<SettingsPanelView>(true);
                if (view != null)
                {
                    count += ApplyFontToExistingTmp(view.gameObject, font);
                    count += ApplyInputFonts(view.gameObject, font);
                }

                var buttons = contents.GetComponentsInChildren<Button>(true);
                for (var i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == null || buttons[i].gameObject.name != "Settings")
                        continue;
                    count += ApplyFontToExistingTmp(buttons[i].gameObject, font);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                AssetDatabase.SaveAssets();
                return "设置已换成字制区喜脉体，更新 " + count + " 处。";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        public static TMP_FontAsset LoadProjectFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MapEditorPaths.LegacyChineseTmpFont);
            if (font != null)
                return font;

            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MapEditorPaths.LiberationSansTmpFont);
            if (font != null)
                return font;

            return TMP_Settings.defaultFontAsset;
        }

        public static void EnableTmpShaderChannels(Canvas canvas)
        {
            if (canvas == null)
                return;

            canvas.additionalShaderChannels |=
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.Normal |
                AdditionalCanvasShaderChannels.Tangent;
        }

        static void ApplyDefaultTmpFont(TMP_FontAsset font)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
                return;

            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        static int ConvertPrefab(string path, TMP_FontAsset font)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return 0;

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var converted = ConvertLegacyTexts(contents, font);
                converted += ApplyFontToExistingTmp(contents, font);
                var canvases = contents.GetComponentsInChildren<Canvas>(true);
                for (var i = 0; i < canvases.Length; i++)
                    EnableTmpShaderChannels(canvases[i]);

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                return converted;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static int ConvertLegacyTexts(GameObject root, TMP_FontAsset font)
        {
            var texts = root.GetComponentsInChildren<Text>(true);
            var converted = 0;
            for (var i = texts.Length - 1; i >= 0; i--)
            {
                if (ConvertOne(texts[i], font))
                    converted++;
            }

            return converted;
        }

        static int ApplyFontToExistingTmp(GameObject root, TMP_FontAsset font)
        {
            var labels = root.GetComponentsInChildren<TMP_Text>(true);
            var count = 0;
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] == null || labels[i].font == font)
                    continue;
                labels[i].font = font;
                EditorUtility.SetDirty(labels[i]);
                count++;
            }

            return count;
        }

        static int ApplyInputFonts(GameObject root, TMP_FontAsset font)
        {
            var inputs = root.GetComponentsInChildren<TMP_InputField>(true);
            var count = 0;
            for (var i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] == null || inputs[i].fontAsset == font)
                    continue;
                inputs[i].fontAsset = font;
                EditorUtility.SetDirty(inputs[i]);
                count++;
            }

            return count;
        }

        static bool ConvertOne(Text text, TMP_FontAsset font)
        {
            if (text == null)
                return false;

            var go = text.gameObject;
            var value = text.text;
            var color = text.color;
            var size = text.fontSize;
            var alignment = ToTmpAlignment(text.alignment);
            var raycast = text.raycastTarget;
            var enabled = text.enabled;

            Object.DestroyImmediate(text);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
                tmp = go.AddComponent<TextMeshProUGUI>();

            tmp.font = font;
            tmp.text = value;
            tmp.color = color;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.raycastTarget = raycast;
            tmp.enabled = enabled;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.richText = true;

            RebindParents(go, tmp);
            return true;
        }

        static void RebindParents(GameObject go, TextMeshProUGUI tmp)
        {
            var slot = go.GetComponentInParent<WarehouseSlotView>(true);
            if (slot != null)
            {
                var so = new SerializedObject(slot);
                so.FindProperty("label").objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var section = go.GetComponentInParent<BackpackCategorySectionView>(true);
            if (section != null)
            {
                var so = new SerializedObject(section);
                so.FindProperty("header").objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.Left;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.Center;
            }
        }
    }
}
