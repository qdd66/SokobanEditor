using System;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    [DefaultExecutionOrder(-30)]
    [InfoBox("Ctrl+S 快速保存。Ctrl+O 打开读取列表。Esc 由暂停界面处理。Ctrl+Z 撤回，Ctrl+Y 重做。")]
    public sealed class MapPersistenceController : MonoBehaviour
    {
        [LabelText("存档配置")]
        [SerializeField, Required("请指定存档配置"), AssetsOnly, InlineEditor]
        MapPersistenceConfig config;

        [LabelText("编辑会话")]
        [SerializeField, Required("请指定地图编辑会话")]
        MapEditSession editSession;

        [LabelText("飞行")]
        [Tooltip("保存和恢复相机。可空则不处理相机。")]
        [SerializeField]
        FirstPersonFlyController fly;

        [LabelText("鼠标锁定")]
        [SerializeField]
        GameplayCursor gameplayCursor;

        [LabelText("放置")]
        [Tooltip("保存或打开列表前取消手里的幽灵预览。")]
        [SerializeField]
        PlacementController placement;

        [LabelText("背包")]
        [Tooltip("打开读取列表时先关掉背包。")]
        [SerializeField]
        BackpackPanelView backpack;

        [LabelText("读取面板")]
        [SerializeField, Required("请指定读取面板")]
        MapLoadPanelView loadPanel;

        [LabelText("暂停界面")]
        [Tooltip("打开时关闭读取列表不要把鼠标锁回去。可空。")]
        [SerializeField]
        PauseMenuController pauseMenu;

        [LabelText("会话请求")]
        [Tooltip("开始界面带过来的新建或读档。可空则进场景后保持空图。")]
        [SerializeField, AssetsOnly]
        MapSessionRequest session;

        string currentPath;

        public event Action MapApplied;

        public bool IsLoadPanelOpen => loadPanel != null && loadPanel.IsOpen;

        void Start()
        {
            if (Time.timeScale == 0f)
                Time.timeScale = 1f;
            ApplyPendingSession();
        }

        public void ApplyPendingSession()
        {
            if (session == null || !session.TryConsume(out var isNewMap, out var path))
                return;
            if (isNewMap || string.IsNullOrEmpty(path))
                return;
            LoadFromPath(path);
        }

        void OnEnable()
        {
            if (loadPanel != null)
                loadPanel.Closed += HandleLoadPanelClosed;
            MapEditorLocale.Changed += HandleLocaleChanged;
        }

        void OnDisable()
        {
            MapEditorLocale.Changed -= HandleLocaleChanged;
            if (loadPanel != null)
                loadPanel.Closed -= HandleLoadPanelClosed;
        }

        void HandleLocaleChanged()
        {
            if (loadPanel != null && loadPanel.IsOpen)
                RefreshLoadPanelList();
        }

        void Update()
        {
            if (config == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (ChordPressed(keyboard, config.SaveKey, config.SaveRequiresCtrl))
            {
                Save();
                return;
            }

            if (ChordPressed(keyboard, config.LoadKey, config.LoadRequiresCtrl))
            {
                ToggleLoadPanel();
                return;
            }

            if (loadPanel != null && loadPanel.IsOpen &&
                (Pressed(keyboard, config.CloseLoadPanelKey) || Pressed(keyboard, Key.Escape)))
            {
                if (loadPanel.ConsumeEscape())
                    return;
                if (pauseMenu == null)
                    HideLoadPanel();
                return;
            }

            if (pauseMenu != null && pauseMenu.IsOpen)
                return;

            if (gameplayCursor != null && gameplayCursor.IsUiHold)
                return;

            if (ChordPressed(keyboard, config.UndoKey, config.UndoRequiresCtrl))
                Undo();
            else if (ChordPressed(keyboard, config.RedoKey, config.RedoRequiresCtrl))
                Redo();
        }

        public bool ConsumeLoadPanelEscape()
        {
            return loadPanel != null && loadPanel.ConsumeEscape();
        }

        public void NotifyRenamed(string fromPath, string toPath)
        {
            if (MapFileStore.IsSamePath(currentPath, fromPath))
                currentPath = toPath;
        }

        public void NotifyDeleted(string path)
        {
            if (MapFileStore.IsSamePath(currentPath, path))
                currentPath = null;
        }

        public bool TryRenameMap(MapFileInfo info, string newName)
        {
            if (info == null || config == null)
                return false;
            if (string.IsNullOrWhiteSpace(newName))
            {
                Toast(config.EmptyRenameHint);
                return false;
            }

            if (!MapFileStore.TryRename(info.Path, newName, out var newPath, out var error))
            {
                Toast(string.IsNullOrEmpty(error) ? config.RenameFailedHint : error);
                return false;
            }

            NotifyRenamed(info.Path, newPath);
            Toast(string.Format(config.RenamedHint, Path.GetFileNameWithoutExtension(newPath)));
            RefreshLoadPanelList();
            return true;
        }

        public void DeleteMap(MapFileInfo info)
        {
            if (info == null || config == null)
                return;
            var label = string.IsNullOrEmpty(info.DisplayName) ? info.FileName : info.DisplayName;
            if (!MapFileStore.TryDelete(info.Path, out var error))
            {
                Toast(string.IsNullOrEmpty(error) ? config.DeleteFailedHint : error);
                return;
            }

            NotifyDeleted(info.Path);
            Toast(string.Format(config.DeletedHint, label));
            RefreshLoadPanelList();
        }

        public void Save()
        {
            var path = string.IsNullOrEmpty(currentPath)
                ? MapFileStore.ResolvePath(config, config.DefaultFileName)
                : currentPath;
            TrySaveToPath(path);
        }

        public bool TrySaveAs(string fileName)
        {
            if (config == null)
                return false;
            return TrySaveToPath(MapFileStore.ResolvePath(config, fileName));
        }

        public bool TrySaveToPath(string path)
        {
            PrepareWorld();
            if (editSession == null || editSession.ItemWorld == null || config == null)
            {
                Toast(config != null ? config.SaveFailedHint : "保存失败");
                return false;
            }

            try
            {
                var items = editSession.ItemWorld.CaptureAll();
                var json = new MapDocumentJson
                {
                    formatVersion = MapPersistenceConfig.CurrentFormatVersion,
                    mapName = Path.GetFileNameWithoutExtension(path),
                    items = ToJson(items)
                };

                if (config.SaveCamera && fly != null)
                {
                    var pose = fly.CapturePose();
                    json.hasCamera = true;
                    json.cameraPosition = pose.Position;
                    json.cameraYaw = pose.Yaw;
                    json.cameraPitch = pose.Pitch;
                }

                MapFileStore.Write(path, json);
                currentPath = path;
                Toast(string.Format(config.SavedHint, Path.GetFileName(path)));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Toast(config.SaveFailedHint);
                return false;
            }
        }

        public void LoadFromPath(string path)
        {
            PrepareWorld();
            if (!MapFileStore.TryRead(path, out var json, out var error))
            {
                Toast(string.IsNullOrEmpty(error) ? config.LoadFailedHint : error);
                return;
            }

            try
            {
                ApplyDocument(json);
                currentPath = path;
                editSession.ClearHistory();
                HideLoadPanel();
                if (pauseMenu != null && pauseMenu.IsOpen)
                    pauseMenu.Resume();

                var name = string.IsNullOrEmpty(json.mapName)
                    ? Path.GetFileNameWithoutExtension(path)
                    : json.mapName;
                var message = string.Format(config.LoadedHint, name);
                var missing = editSession.ItemWorld.LastMissingIds;
                if (missing != null && missing.Count > 0)
                    message += "（" + string.Format(config.MissingItemsHint, missing.Count) + "）";
                Toast(message);
                MapApplied?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Toast(config.LoadFailedHint);
            }
        }

        void ApplyDocument(MapDocumentJson json)
        {
            var records = new List<MapItemRecord>();
            if (json.items != null)
            {
                for (var i = 0; i < json.items.Length; i++)
                    records.Add(MapItemRecord.FromJson(json.items[i]));
            }

            editSession.ItemWorld.ApplyAll(records);

            if (json.hasCamera && config.SaveCamera && fly != null)
                fly.ApplyPose(new CameraPose(json.cameraPosition, json.cameraYaw, json.cameraPitch));
        }

        public void ShowLoadPanel()
        {
            OpenLoadPanel();
        }

        public void HideLoadPanel()
        {
            if (loadPanel != null && loadPanel.IsOpen)
                loadPanel.SetOpen(false);
            else
                ReleaseUiHold();
        }

        void ToggleLoadPanel()
        {
            if (loadPanel == null)
                return;
            if (loadPanel.IsOpen)
            {
                if (loadPanel.ConsumeEscape())
                    return;
                HideLoadPanel();
                return;
            }

            OpenLoadPanel();
        }

        void OpenLoadPanel()
        {
            PrepareWorld();
            if (backpack != null && backpack.IsOpen)
                backpack.SetOpen(false);

            RefreshLoadPanelList();
            loadPanel.SetOpen(true);
            if (gameplayCursor != null && !gameplayCursor.IsUiHold)
                gameplayCursor.SetUiHold(true);
        }

        void RefreshLoadPanelList()
        {
            if (loadPanel == null || config == null)
                return;
            var files = MapFileStore.ListMaps(config);
            loadPanel.Bind(files, HandleFilePicked, TryRenameMap, DeleteMap, config);
        }

        void HandleLoadPanelClosed()
        {
            ReleaseUiHold();
        }

        void HandleFilePicked(MapFileInfo info)
        {
            if (info == null || !info.IsReadable)
                return;
            LoadFromPath(info.Path);
        }

        void Undo()
        {
            PrepareWorld();
            if (editSession == null || !editSession.Undo())
            {
                Toast(config.NothingToUndoHint);
                return;
            }

            Toast(config.UndoneHint);
        }

        void Redo()
        {
            PrepareWorld();
            if (editSession == null || !editSession.Redo())
                return;
            Toast(config.RedoneHint);
        }

        void PrepareWorld()
        {
            if (placement != null)
                placement.AbortSession();
        }

        void ReleaseUiHold()
        {
            if (gameplayCursor == null)
                return;
            if (pauseMenu != null && pauseMenu.IsOpen)
                return;
            if (backpack != null && backpack.IsOpen)
                return;
            gameplayCursor.SetUiHold(false);
        }

        void Toast(string message)
        {
            if (config != null && config.ToastChannel != null)
                config.ToastChannel.Raise(message);
        }

        static MapItemJson[] ToJson(List<MapItemRecord> items)
        {
            var array = new MapItemJson[items.Count];
            for (var i = 0; i < items.Count; i++)
                array[i] = items[i].ToJson();
            return array;
        }

        static bool ChordPressed(Keyboard keyboard, Key key, bool requireCtrl)
        {
            if (key == Key.None || !keyboard[key].wasPressedThisFrame)
                return false;
            if (!requireCtrl)
                return true;
            return keyboard.leftCtrlKey.isPressed ||
                   keyboard.rightCtrlKey.isPressed ||
                   keyboard.leftCommandKey.isPressed ||
                   keyboard.rightCommandKey.isPressed;
        }

        static bool Pressed(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }
    }
}
