using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public static class MapFileListUiSetup
    {
        static string LoadPanelPath => MapEditorPaths.MapLoadPanelPrefab;
        static string PauseMenuPath => MapEditorPaths.PauseMenuPrefab;

        public static void UpgradePrefabs()
        {
            var font = UiTmpFontSetup.LoadProjectFont();
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            UpgradePrefab(LoadPanelPath, font, sprite);
            UpgradePrefab(PauseMenuPath, font, sprite);
        }

        public static GameObject CreateRow(Transform parent, TMP_FontAsset font, Sprite sprite)
        {
            var row = CreateUi("MapLoadRow", parent);
            var image = row.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(1f, 1f, 1f, 0.06f);
            image.raycastTarget = true;
            var layout = row.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 10, 10);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var element = row.AddComponent<LayoutElement>();
            element.minHeight = 80f;
            element.preferredHeight = 80f;
            row.AddComponent<MapLoadRowView>();

            var header = CreateUi("Header", row.transform);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;
            var headerElement = header.AddComponent<LayoutElement>();
            headerElement.minHeight = 32f;
            headerElement.preferredHeight = 32f;

            var title = CreateText(header.transform, "Title", "地图名", font, 24, TextAlignmentOptions.Left);
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.minHeight = 28f;
            titleElement.preferredHeight = 28f;
            titleElement.flexibleWidth = 1f;
            titleElement.minWidth = 80f;

            var nameField = CreateNameField(header.transform, font, sprite);
            nameField.gameObject.SetActive(false);

            var rename = CreateRowButton(header.transform, "Rename", "改名", font, sprite);
            var delete = CreateRowButton(header.transform, "Delete", "删除", font, sprite);

            var details = CreateText(row.transform, "Details", "时间 · 大小 · 物体 · 地形", font, 16, TextAlignmentOptions.Left);
            details.color = new Color(0.78f, 0.78f, 0.8f, 1f);
            details.enableWordWrapping = true;
            details.overflowMode = TextOverflowModes.Ellipsis;
            var detailsElement = details.gameObject.AddComponent<LayoutElement>();
            detailsElement.minHeight = 22f;
            detailsElement.preferredHeight = 22f;

            WireRow(
                row.GetComponent<MapLoadRowView>(),
                title,
                details,
                image,
                rename.GetComponent<Button>(),
                delete.GetComponent<Button>(),
                rename.GetComponentInChildren<TMP_Text>(),
                delete.GetComponentInChildren<TMP_Text>(),
                nameField);
            return row;
        }

        static void UpgradePrefab(string path, TMP_FontAsset font, Sprite sprite)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                return;

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var rows = contents.GetComponentsInChildren<MapLoadRowView>(true);
                for (var i = 0; i < rows.Length; i++)
                    EnsureRow(rows[i], font, sprite);

                var load = contents.GetComponent<MapLoadPanelView>()
                    ?? contents.GetComponentInChildren<MapLoadPanelView>(true);
                if (load != null)
                {
                    var confirm = EnsureConfirm(load.transform, font, sprite);
                    var hint = FindNamedText(load.transform, "Hint");
                    var so = new SerializedObject(load);
                    so.FindProperty("confirm").objectReferenceValue = confirm;
                    if (hint != null)
                        so.FindProperty("hint").objectReferenceValue = hint;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    if (hint != null)
                        hint.text = "点击一项加载 · 右侧可改名或删除 · Esc 关闭";
                }

                var save = contents.GetComponent<MapSavePanelView>()
                    ?? contents.GetComponentInChildren<MapSavePanelView>(true);
                if (save != null)
                {
                    var confirm = EnsureConfirm(save.transform, font, sprite);
                    var so = new SerializedObject(save);
                    so.FindProperty("confirm").objectReferenceValue = confirm;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    var saveHint = FindNamedText(save.transform, "Hint");
                    if (saveHint != null)
                        saveHint.text = "点击已有存档覆盖 · 右侧可改名或删除 · 输入名称后建立新存档";
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void EnsureRow(MapLoadRowView row, TMP_FontAsset font, Sprite sprite)
        {
            if (row == null)
                return;

            var so = new SerializedObject(row);
            if (so.FindProperty("renameButton").objectReferenceValue != null &&
                so.FindProperty("nameField").objectReferenceValue != null)
            {
                var layout = row.GetComponent<LayoutElement>();
                if (layout != null)
                {
                    layout.minHeight = 80f;
                    layout.preferredHeight = 80f;
                }

                return;
            }

            var title = so.FindProperty("title").objectReferenceValue as TMP_Text;
            var details = so.FindProperty("details").objectReferenceValue as TMP_Text;
            var background = so.FindProperty("background").objectReferenceValue as Image;
            if (title == null)
            {
                var titleTransform = row.transform.Find("Title") ?? row.transform.Find("Header/Title");
                if (titleTransform != null)
                    title = titleTransform.GetComponent<TMP_Text>();
            }

            var header = row.transform.Find("Header");
            if (header == null)
            {
                var headerGo = CreateUi("Header", row.transform);
                header = headerGo.transform;
                header.SetSiblingIndex(0);
                var headerLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
                headerLayout.spacing = 8f;
                headerLayout.childAlignment = TextAnchor.MiddleLeft;
                headerLayout.childControlWidth = true;
                headerLayout.childControlHeight = true;
                headerLayout.childForceExpandWidth = false;
                headerLayout.childForceExpandHeight = true;
                var headerElement = headerGo.AddComponent<LayoutElement>();
                headerElement.minHeight = 32f;
                headerElement.preferredHeight = 32f;
            }

            if (title != null && title.transform.parent != header)
                title.transform.SetParent(header, false);

            if (title != null)
            {
                var titleElement = title.GetComponent<LayoutElement>();
                if (titleElement == null)
                    titleElement = title.gameObject.AddComponent<LayoutElement>();
                titleElement.flexibleWidth = 1f;
                titleElement.minWidth = 80f;
                titleElement.minHeight = 28f;
                titleElement.preferredHeight = 28f;
            }

            var nameField = header.GetComponentInChildren<TMP_InputField>(true);
            if (nameField == null)
            {
                nameField = CreateNameField(header, font, sprite);
                nameField.gameObject.SetActive(false);
            }

            var rename = FindButton(header, "Rename");
            if (rename == null)
                rename = CreateRowButton(header, "Rename", "改名", font, sprite).GetComponent<Button>();

            var delete = FindButton(header, "Delete");
            if (delete == null)
                delete = CreateRowButton(header, "Delete", "删除", font, sprite).GetComponent<Button>();

            var rowLayout = row.GetComponent<LayoutElement>();
            if (rowLayout != null)
            {
                rowLayout.minHeight = 80f;
                rowLayout.preferredHeight = 80f;
            }

            WireRow(
                row,
                title,
                details,
                background,
                rename,
                delete,
                rename.GetComponentInChildren<TMP_Text>(),
                delete.GetComponentInChildren<TMP_Text>(),
                nameField);
        }

        static MapFileConfirmView EnsureConfirm(Transform parent, TMP_FontAsset font, Sprite sprite)
        {
            var existing = parent.GetComponentInChildren<MapFileConfirmView>(true);
            if (existing != null)
            {
                existing.transform.SetAsLastSibling();
                existing.gameObject.SetActive(false);
                return existing;
            }

            var root = CreateUi("FileConfirm", parent);
            Stretch(root);
            root.transform.SetAsLastSibling();
            var dimmer = root.AddComponent<Image>();
            dimmer.sprite = sprite;
            dimmer.color = new Color(0f, 0f, 0f, 0.55f);
            dimmer.raycastTarget = true;
            var confirm = root.AddComponent<MapFileConfirmView>();

            var window = CreateUi("Window", root.transform);
            var windowRt = window.GetComponent<RectTransform>();
            windowRt.anchorMin = new Vector2(0.5f, 0.5f);
            windowRt.anchorMax = new Vector2(0.5f, 0.5f);
            windowRt.pivot = new Vector2(0.5f, 0.5f);
            windowRt.sizeDelta = new Vector2(520f, 260f);
            var windowImage = window.AddComponent<Image>();
            windowImage.sprite = sprite;
            windowImage.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);
            windowImage.raycastTarget = true;

            var title = CreateText(window.transform, "Title", "删除存档", font, 28, TextAlignmentOptions.Center);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -20f);
            titleRt.sizeDelta = new Vector2(-40f, 40f);

            var message = CreateText(window.transform, "Message", "确定删除？", font, 20, TextAlignmentOptions.Center);
            message.enableWordWrapping = true;
            message.overflowMode = TextOverflowModes.Ellipsis;
            var messageRt = message.rectTransform;
            messageRt.anchorMin = new Vector2(0f, 0.5f);
            messageRt.anchorMax = new Vector2(1f, 1f);
            messageRt.offsetMin = new Vector2(24f, 20f);
            messageRt.offsetMax = new Vector2(-24f, -64f);

            var delete = CreateRowButton(window.transform, "Confirm", "删除", font, sprite);
            var deleteRt = delete.GetComponent<RectTransform>();
            deleteRt.anchorMin = new Vector2(0.5f, 0f);
            deleteRt.anchorMax = new Vector2(0.5f, 0f);
            deleteRt.pivot = new Vector2(1f, 0f);
            deleteRt.anchoredPosition = new Vector2(-12f, 24f);
            deleteRt.sizeDelta = new Vector2(140f, 48f);
            delete.GetComponent<Image>().color = new Color(0.45f, 0.16f, 0.16f, 1f);

            var cancel = CreateRowButton(window.transform, "Cancel", "取消", font, sprite);
            var cancelRt = cancel.GetComponent<RectTransform>();
            cancelRt.anchorMin = new Vector2(0.5f, 0f);
            cancelRt.anchorMax = new Vector2(0.5f, 0f);
            cancelRt.pivot = new Vector2(0f, 0f);
            cancelRt.anchoredPosition = new Vector2(12f, 24f);
            cancelRt.sizeDelta = new Vector2(140f, 48f);

            var so = new SerializedObject(confirm);
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("message").objectReferenceValue = message;
            so.FindProperty("confirmButton").objectReferenceValue = delete.GetComponent<Button>();
            so.FindProperty("cancelButton").objectReferenceValue = cancel.GetComponent<Button>();
            so.FindProperty("confirmLabel").objectReferenceValue = delete.GetComponentInChildren<TMP_Text>();
            so.FindProperty("cancelLabel").objectReferenceValue = cancel.GetComponentInChildren<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return confirm;
        }

        static void WireRow(
            MapLoadRowView row,
            TMP_Text title,
            TMP_Text details,
            Image background,
            Button rename,
            Button delete,
            TMP_Text renameLabel,
            TMP_Text deleteLabel,
            TMP_InputField nameField)
        {
            var so = new SerializedObject(row);
            so.FindProperty("title").objectReferenceValue = title;
            so.FindProperty("details").objectReferenceValue = details;
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("renameButton").objectReferenceValue = rename;
            so.FindProperty("deleteButton").objectReferenceValue = delete;
            so.FindProperty("renameLabel").objectReferenceValue = renameLabel;
            so.FindProperty("deleteLabel").objectReferenceValue = deleteLabel;
            so.FindProperty("nameField").objectReferenceValue = nameField;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject CreateRowButton(
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
            image.raycastTarget = true;
            var button = go.AddComponent<Button>();
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.75f, 0.88f, 1f, 1f);
            colors.pressedColor = new Color(0.6f, 0.72f, 0.9f, 1f);
            button.colors = colors;
            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = 76f;
            element.minWidth = 76f;
            element.preferredHeight = 32f;
            element.minHeight = 32f;
            element.flexibleWidth = 0f;
            var text = CreateText(go.transform, "Label", label, font, 18, TextAlignmentOptions.Center);
            Stretch(text.gameObject);
            text.raycastTarget = false;
            return go;
        }

        static TMP_InputField CreateNameField(Transform parent, TMP_FontAsset font, Sprite sprite)
        {
            var go = CreateUi("NameField", parent);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = new Color(0.06f, 0.06f, 0.08f, 1f);
            image.raycastTarget = true;
            var element = go.AddComponent<LayoutElement>();
            element.flexibleWidth = 1f;
            element.minWidth = 80f;
            element.preferredHeight = 32f;
            element.minHeight = 32f;
            var input = go.AddComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;

            var area = CreateUi("TextArea", go.transform);
            Stretch(area);
            var areaRt = area.GetComponent<RectTransform>();
            areaRt.offsetMin = new Vector2(10f, 4f);
            areaRt.offsetMax = new Vector2(-10f, -4f);
            area.AddComponent<RectMask2D>();

            var placeholder = CreateText(area.transform, "Placeholder", "输入新名称", font, 18, TextAlignmentOptions.Left);
            placeholder.color = new Color(1f, 1f, 1f, 0.35f);
            Stretch(placeholder.gameObject);

            var text = CreateText(area.transform, "Text", string.Empty, font, 18, TextAlignmentOptions.Left);
            Stretch(text.gameObject);

            input.textViewport = area.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.fontAsset = font;
            input.pointSize = 18;
            input.caretColor = Color.white;
            input.customCaretColor = true;
            return input;
        }

        static Button FindButton(Transform parent, string name)
        {
            var child = parent.Find(name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        static TMP_Text FindNamedText(Transform root, string name)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name != name)
                    continue;
                var text = transforms[i].GetComponent<TMP_Text>();
                if (text != null)
                    return text;
            }

            return null;
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
    }
}
