using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public sealed class MapSavePanelView : MonoBehaviour
    {
        [LabelText("行模板")]
        [SerializeField, Required("请指定行模板")]
        MapLoadRowView rowTemplate;

        [LabelText("内容根节点")]
        [SerializeField, Required("请指定内容根节点")]
        RectTransform contentRoot;

        [LabelText("空状态")]
        [SerializeField]
        GameObject emptyState;

        [LabelText("空状态文本")]
        [SerializeField]
        TMP_Text emptyLabel;

        [LabelText("标题")]
        [SerializeField]
        TMP_Text title;

        [LabelText("提示")]
        [SerializeField]
        TMP_Text hint;

        [LabelText("名称输入")]
        [SerializeField]
        TMP_InputField nameField;

        [LabelText("建立新存档")]
        [SerializeField]
        Button createButton;

        [LabelText("返回")]
        [SerializeField]
        Button backButton;

        [LabelText("建立新存档文字")]
        [SerializeField]
        TMP_Text createLabel;

        [LabelText("返回文字")]
        [SerializeField]
        TMP_Text backLabel;

        [LabelText("删除确认")]
        [SerializeField]
        MapFileConfirmView confirm;

        readonly List<GameObject> spawned = new List<GameObject>();
        IReadOnlyList<MapFileInfo> files;
        Action<MapFileInfo> overwrite;
        Action<string> createNew;
        Action back;
        Func<MapFileInfo, string, bool> renamed;
        Action<MapFileInfo> deleted;
        MapPersistenceConfig texts;

        void Awake()
        {
            if (createButton != null)
            {
                createButton.onClick.RemoveListener(HandleCreate);
                createButton.onClick.AddListener(HandleCreate);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBack);
                backButton.onClick.AddListener(HandleBack);
            }

            if (nameField != null)
                nameField.onSubmit.AddListener(HandleSubmit);
        }

        void OnDestroy()
        {
            if (nameField != null)
                nameField.onSubmit.RemoveListener(HandleSubmit);
        }

        public void ApplyConfig(PauseMenuConfig config)
        {
            if (config == null)
                return;
            if (title != null)
                title.text = config.SaveTitle;
            if (hint != null)
                hint.text = config.SaveHint;
            if (createLabel != null)
                createLabel.text = config.CreateLabel;
            if (backLabel != null)
                backLabel.text = config.BackLabel;
            if (nameField != null && nameField.placeholder is TMP_Text placeholder)
                placeholder.text = config.NamePlaceholder;
        }

        public void Bind(
            IReadOnlyList<MapFileInfo> list,
            Action<MapFileInfo> onOverwrite,
            Action<string> onCreate,
            Action onBack,
            Func<MapFileInfo, string, bool> onRename,
            Action<MapFileInfo> onDelete,
            MapPersistenceConfig config)
        {
            files = list;
            overwrite = onOverwrite;
            createNew = onCreate;
            back = onBack;
            renamed = onRename;
            deleted = onDelete;
            texts = config;
            if (emptyLabel != null)
                emptyLabel.text = config != null ? config.NoMapsHint : string.Empty;
            if (confirm != null)
                confirm.Hide();
            Rebuild();
        }

        public void FocusName()
        {
            if (nameField != null)
                nameField.ActivateInputField();
        }

        public string ReadName()
        {
            return nameField != null ? nameField.text : string.Empty;
        }

        public void ClearName()
        {
            if (nameField != null)
                nameField.text = string.Empty;
        }

        public bool ConsumeEscape()
        {
            if (confirm != null && confirm.IsOpen)
            {
                confirm.Hide();
                return true;
            }

            for (var i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] == null)
                    continue;
                var row = spawned[i].GetComponent<MapLoadRowView>();
                if (row == null || !row.IsRenaming)
                    continue;
                row.CancelRename();
                return true;
            }

            return false;
        }

        void OnEnable()
        {
            Rebuild();
        }

        void OnDisable()
        {
            if (confirm != null)
                confirm.Hide();
        }

        void Rebuild()
        {
            ClearSpawned();
            if (confirm != null)
                confirm.Hide();
            var count = files != null ? files.Count : 0;
            if (emptyState != null)
                emptyState.SetActive(count == 0);
            if (count == 0 || rowTemplate == null || contentRoot == null)
                return;

            for (var i = 0; i < count; i++)
            {
                var row = Instantiate(rowTemplate, contentRoot);
                row.gameObject.SetActive(true);
                row.Bind(files[i], HandleOverwrite, HandleRename, HandleDeleteRequest, texts);
                spawned.Add(row.gameObject);
            }
        }

        void HandleOverwrite(MapFileInfo info)
        {
            if (confirm != null && confirm.IsOpen)
                return;
            overwrite?.Invoke(info);
        }

        bool HandleRename(MapFileInfo info, string newName)
        {
            return renamed != null && renamed.Invoke(info, newName);
        }

        void HandleDeleteRequest(MapFileInfo info)
        {
            if (info == null || deleted == null)
                return;
            if (confirm == null)
            {
                deleted.Invoke(info);
                return;
            }

            var name = string.IsNullOrEmpty(info.DisplayName) ? info.FileName : info.DisplayName;
            confirm.Show(
                texts != null ? texts.ConfirmDeleteTitle : MapEditorLocale.Pick("Delete Save", "删除存档"),
                string.Format(texts != null ? texts.ConfirmDeleteHint : MapEditorLocale.Pick("Delete \"{0}\"?", "确定删除「{0}」？"), name),
                texts != null ? texts.ConfirmDeleteLabel : MapEditorLocale.Pick("Delete", "删除"),
                texts != null ? texts.CancelLabel : MapEditorLocale.Pick("Cancel", "取消"),
                () => deleted.Invoke(info));
        }

        void HandleCreate()
        {
            createNew?.Invoke(ReadName());
        }

        void HandleSubmit(string value)
        {
            createNew?.Invoke(value);
        }

        void HandleBack()
        {
            if (ConsumeEscape())
                return;
            back?.Invoke();
        }

        void ClearSpawned()
        {
            for (var i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null)
                    Destroy(spawned[i]);
            }

            spawned.Clear();
        }
    }
}
