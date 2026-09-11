using System.IO;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DZDMapEditor
{
    [DefaultExecutionOrder(-50)]
    [InfoBox("开始界面：打开存档或新建地图进入默认编辑场景。")]
    public sealed class StartMenuController : MonoBehaviour
    {
        [LabelText("开始配置")]
        [SerializeField, Required("请指定开始界面配置"), AssetsOnly, InlineEditor]
        StartMenuConfig config;

        [LabelText("存档配置")]
        [Tooltip("列出本地地图文件。")]
        [SerializeField, Required("请指定存档配置"), AssetsOnly]
        MapPersistenceConfig persistenceConfig;

        [LabelText("会话请求")]
        [Tooltip("把新建或读档意图带进编辑场景。")]
        [SerializeField, Required("请指定地图会话请求"), AssetsOnly]
        MapSessionRequest session;

        [LabelText("开始菜单")]
        [SerializeField, Required("请指定开始菜单")]
        StartMenuView view;

        [LabelText("读取面板")]
        [SerializeField, Required("请指定读取面板")]
        MapLoadPanelView loadPanel;

        void OnEnable()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (view != null)
            {
                view.OpenSaveClicked += OpenSaves;
                view.NewMapClicked += StartNewMap;
                view.ApplyConfig(config);
            }

            MapEditorLocale.Changed += HandleLocaleChanged;
        }

        void OnDisable()
        {
            MapEditorLocale.Changed -= HandleLocaleChanged;
            if (view != null)
            {
                view.OpenSaveClicked -= OpenSaves;
                view.NewMapClicked -= StartNewMap;
            }
        }

        void HandleLocaleChanged()
        {
            if (view != null)
                view.ApplyConfig(config);
            if (loadPanel != null && loadPanel.IsOpen)
                OpenSaves();
        }

        void Update()
        {
            if (config == null || loadPanel == null || !loadPanel.IsOpen)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (config.CloseLoadPanelKey == Key.None)
                return;
            if (keyboard[config.CloseLoadPanelKey].wasPressedThisFrame)
            {
                if (loadPanel.ConsumeEscape())
                    return;
                loadPanel.SetOpen(false);
            }
        }

        void OpenSaves()
        {
            if (loadPanel == null || persistenceConfig == null)
                return;

            var files = MapFileStore.ListMaps(persistenceConfig);
            loadPanel.Bind(files, HandleFilePicked, HandleRename, HandleDelete, persistenceConfig);
            loadPanel.SetOpen(true);
        }

        bool HandleRename(MapFileInfo info, string newName)
        {
            if (info == null || persistenceConfig == null)
                return false;
            if (string.IsNullOrWhiteSpace(newName))
            {
                RaiseToast(persistenceConfig.EmptyRenameHint);
                return false;
            }

            if (!MapFileStore.TryRename(info.Path, newName, out var newPath, out var error))
            {
                RaiseToast(string.IsNullOrEmpty(error) ? persistenceConfig.RenameFailedHint : error);
                return false;
            }

            RaiseToast(string.Format(persistenceConfig.RenamedHint, Path.GetFileNameWithoutExtension(newPath)));
            OpenSaves();
            return true;
        }

        void HandleDelete(MapFileInfo info)
        {
            if (info == null || persistenceConfig == null)
                return;
            var label = string.IsNullOrEmpty(info.DisplayName) ? info.FileName : info.DisplayName;
            if (!MapFileStore.TryDelete(info.Path, out var error))
            {
                RaiseToast(string.IsNullOrEmpty(error) ? persistenceConfig.DeleteFailedHint : error);
                return;
            }

            RaiseToast(string.Format(persistenceConfig.DeletedHint, label));
            OpenSaves();
        }

        void HandleFilePicked(MapFileInfo info)
        {
            if (info == null || !info.IsReadable)
            {
                RaiseToast(info != null && !string.IsNullOrEmpty(info.Error)
                    ? info.Error
                    : persistenceConfig != null ? persistenceConfig.LoadFailedHint : MapEditorLocale.Pick("Load failed", "加载失败"));
                return;
            }

            if (!MapFileStore.TryRead(info.Path, out _, out var error))
            {
                RaiseToast(string.IsNullOrEmpty(error)
                    ? persistenceConfig.LoadFailedHint
                    : error);
                return;
            }

            if (session == null)
                return;

            session.RequestLoad(info.Path);
            EnterEditor();
        }

        void StartNewMap()
        {
            if (session == null)
                return;
            session.RequestNew();
            EnterEditor();
        }

        void EnterEditor()
        {
            Time.timeScale = 1f;
            var sceneName = config != null ? config.EditorSceneName : "SampleScene";
            SceneManager.LoadScene(sceneName);
        }

        void RaiseToast(string message)
        {
            if (persistenceConfig != null && persistenceConfig.ToastChannel != null)
                persistenceConfig.ToastChannel.Raise(message);
        }
    }
}
