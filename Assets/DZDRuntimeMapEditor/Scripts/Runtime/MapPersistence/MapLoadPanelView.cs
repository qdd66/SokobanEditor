using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class MapLoadPanelView : MonoBehaviour
    {
        [LabelText("行预制体")]
        [Tooltip("列表里每一项的模板。运行时会复制。")]
        [SerializeField, Required("请指定行模板")]
        MapLoadRowView rowTemplate;

        [LabelText("内容根节点")]
        [Tooltip("滚动区内生成文件行的父节点。")]
        [SerializeField, Required("请指定内容根节点")]
        RectTransform contentRoot;

        [LabelText("空状态")]
        [Tooltip("没有地图文件时显示。")]
        [SerializeField]
        GameObject emptyState;

        [LabelText("空状态文本")]
        [SerializeField]
        TMP_Text emptyLabel;

        [LabelText("列表提示")]
        [SerializeField]
        TMP_Text hint;

        [LabelText("删除确认")]
        [SerializeField]
        MapFileConfirmView confirm;

        readonly List<GameObject> spawned = new List<GameObject>();
        IReadOnlyList<MapFileInfo> files;
        Action<MapFileInfo> picked;
        Func<MapFileInfo, string, bool> renamed;
        Action<MapFileInfo> deleted;
        MapPersistenceConfig texts;

        public event Action Closed;

        public bool IsOpen => gameObject.activeSelf;

        public void Bind(
            IReadOnlyList<MapFileInfo> list,
            Action<MapFileInfo> onPicked,
            Func<MapFileInfo, string, bool> onRename,
            Action<MapFileInfo> onDelete,
            MapPersistenceConfig config)
        {
            files = list;
            picked = onPicked;
            renamed = onRename;
            deleted = onDelete;
            texts = config;
            if (emptyLabel != null)
                emptyLabel.text = config != null ? config.NoMapsHint : string.Empty;
            if (hint != null && config != null)
                hint.text = config.LoadListHint;
            if (confirm != null)
                confirm.Hide();
            if (IsOpen)
                Rebuild();
        }

        public void SetOpen(bool open)
        {
            if (gameObject.activeSelf == open)
            {
                if (open)
                    Rebuild();
                return;
            }

            gameObject.SetActive(open);
            if (open)
                Rebuild();
            else
            {
                if (confirm != null)
                    confirm.Hide();
                Closed?.Invoke();
            }
        }

        public void CloseFromUi()
        {
            if (ConsumeEscape())
                return;
            SetOpen(false);
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
            var empty = count == 0;
            if (emptyState != null)
                emptyState.SetActive(empty);
            if (empty || rowTemplate == null || contentRoot == null)
                return;

            for (var i = 0; i < count; i++)
            {
                var row = Instantiate(rowTemplate, contentRoot);
                row.gameObject.SetActive(true);
                row.Bind(files[i], HandlePicked, HandleRename, HandleDeleteRequest, texts);
                spawned.Add(row.gameObject);
            }
        }

        void HandlePicked(MapFileInfo info)
        {
            if (confirm != null && confirm.IsOpen)
                return;
            picked?.Invoke(info);
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
