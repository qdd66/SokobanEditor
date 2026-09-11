using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DZDMapEditor
{
    [DefaultExecutionOrder(-110)]
    [InfoBox("Esc 打开暂停。再次 Esc 或点回到游戏继续。保存可覆盖已有档或输入名称新建。加载打开地图列表。")]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [LabelText("暂停配置")]
        [SerializeField, Required("请指定暂停配置"), AssetsOnly, InlineEditor]
        PauseMenuConfig config;

        [LabelText("存档配置")]
        [Tooltip("保存页列出本地地图、空列表文案。")]
        [SerializeField, Required("请指定存档配置"), AssetsOnly]
        MapPersistenceConfig persistenceConfig;

        [LabelText("暂停面板")]
        [SerializeField, Required("请指定暂停面板")]
        PauseMenuView view;

        [LabelText("设置配置")]
        [SerializeField, Required("请指定设置配置"), AssetsOnly]
        SettingsPanelConfig settingsConfig;

        [LabelText("保存页")]
        [SerializeField, Required("请指定保存页")]
        MapSavePanelView savePanel;

        [LabelText("存档")]
        [SerializeField, Required("请指定存档组件")]
        MapPersistenceController persistence;

        [LabelText("鼠标锁定")]
        [SerializeField]
        GameplayCursor gameplayCursor;

        [LabelText("放置")]
        [SerializeField]
        PlacementController placement;

        [LabelText("背包")]
        [SerializeField]
        BackpackPanelView backpack;

        float savedTimeScale = 1f;
        bool pausedTime;

        public bool IsOpen => view != null && view.IsOpen;

        void OnEnable()
        {
            if (view != null)
            {
                view.ResumeClicked += Resume;
                view.SaveClicked += OpenSave;
                view.LoadClicked += OpenLoad;
                view.SettingsClicked += OpenSettings;
                view.ApplyConfig(config);
                if (view.SettingsView != null)
                {
                    view.SettingsView.BackClicked += CloseSettings;
                    view.SettingsView.ReturnToStartClicked += ReturnToStart;
                }
            }

            if (savePanel != null)
                savePanel.ApplyConfig(config);

            MapEditorLocale.Changed += HandleLocaleChanged;
        }

        void OnDisable()
        {
            MapEditorLocale.Changed -= HandleLocaleChanged;
            if (view != null)
            {
                view.ResumeClicked -= Resume;
                view.SaveClicked -= OpenSave;
                view.LoadClicked -= OpenLoad;
                view.SettingsClicked -= OpenSettings;
                if (view.SettingsView != null)
                {
                    view.SettingsView.BackClicked -= CloseSettings;
                    view.SettingsView.ReturnToStartClicked -= ReturnToStart;
                }
            }

            RestoreTimeScale();
        }

        void HandleLocaleChanged()
        {
            if (view != null)
                view.ApplyConfig(config);
            if (savePanel != null)
                savePanel.ApplyConfig(config);
            if (view != null && view.IsSavePageOpen)
                BindSaveList();
        }

        void Update()
        {
            if (config == null)
                return;

            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (config.PauseKey == Key.None || !keyboard[config.PauseKey].wasPressedThisFrame)
                return;

            if (view != null && view.IsSettingsPageOpen && view.SettingsView != null && view.SettingsView.IsListening)
            {
                view.SettingsView.CancelListen();
                ConsumeUnlock();
                return;
            }

            if (view != null && view.IsSettingsPageOpen)
            {
                view.ShowMain();
                ConsumeUnlock();
                return;
            }

            if (view != null && view.IsSavePageOpen)
            {
                if (savePanel != null && savePanel.ConsumeEscape())
                {
                    ConsumeUnlock();
                    return;
                }

                view.ShowMain();
                ConsumeUnlock();
                return;
            }

            if (persistence != null && persistence.IsLoadPanelOpen)
            {
                if (persistence.ConsumeLoadPanelEscape())
                {
                    ConsumeUnlock();
                    return;
                }

                persistence.HideLoadPanel();
                ConsumeUnlock();
                return;
            }

            if (IsOpen)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            if (IsOpen)
                return;

            if (placement != null)
                placement.AbortSession();
            if (backpack != null && backpack.IsOpen)
                backpack.SetOpen(false);

            if (config != null && config.PauseTimeScale)
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                pausedTime = true;
            }

            if (view != null)
            {
                view.ApplyConfig(config);
                view.SetOpen(true);
            }

            if (gameplayCursor != null)
                gameplayCursor.SetUiHold(true);
        }

        public void Resume()
        {
            if (persistence != null && persistence.IsLoadPanelOpen)
                persistence.HideLoadPanel();

            if (view != null)
                view.SetOpen(false);

            RestoreTimeScale();
            ConsumeUnlock();
            if (gameplayCursor != null)
                gameplayCursor.SetUiHold(false);
        }

        void OpenSave()
        {
            if (view == null || savePanel == null || persistenceConfig == null)
                return;

            BindSaveList();
            savePanel.ApplyConfig(config);
            savePanel.ClearName();
            view.ShowSave();
            savePanel.FocusName();
        }

        void BindSaveList()
        {
            if (savePanel == null || persistenceConfig == null || view == null)
                return;

            var files = MapFileStore.ListMaps(persistenceConfig);
            savePanel.Bind(
                files,
                HandleOverwrite,
                HandleCreate,
                view.ShowMain,
                HandleRename,
                HandleDelete,
                persistenceConfig);
        }

        void OpenLoad()
        {
            if (persistence != null)
                persistence.ShowLoadPanel();
        }

        void OpenSettings()
        {
            if (view == null)
                return;
            view.ShowSettings();
            if (view.SettingsView != null)
                view.SettingsView.Show(settingsConfig);
        }

        void CloseSettings()
        {
            if (view != null)
                view.ShowMain();
        }

        void ReturnToStart()
        {
            var sceneName = settingsConfig != null ? settingsConfig.StartSceneName : "StartScene";
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                var hint = settingsConfig != null
                    ? settingsConfig.MissingStartSceneHint
                    : MapEditorLocale.Pick(
                        "Start scene is not in Build Settings",
                        "开始场景未加入 Build Settings");
                if (persistenceConfig != null && persistenceConfig.ToastChannel != null)
                    persistenceConfig.ToastChannel.Raise(hint);
                return;
            }

            MapSessionRequest.Clear();
            RestoreTimeScale();
            Time.timeScale = 1f;
            if (gameplayCursor != null)
                gameplayCursor.SetUiHold(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(sceneName);
        }

        void HandleOverwrite(MapFileInfo info)
        {
            if (info == null || persistence == null)
                return;
            if (persistence.TrySaveToPath(info.Path))
                OpenSave();
        }

        bool HandleRename(MapFileInfo info, string newName)
        {
            if (persistence == null)
                return false;
            var ok = persistence.TryRenameMap(info, newName);
            if (ok)
                BindSaveList();
            return ok;
        }

        void HandleDelete(MapFileInfo info)
        {
            if (persistence == null)
                return;
            persistence.DeleteMap(info);
            BindSaveList();
        }

        void HandleCreate(string name)
        {
            if (persistence == null || config == null)
                return;
            if (string.IsNullOrWhiteSpace(name))
            {
                if (persistenceConfig != null && persistenceConfig.ToastChannel != null)
                    persistenceConfig.ToastChannel.Raise(config.EmptyNameHint);
                return;
            }

            if (persistence.TrySaveAs(name.Trim()))
                OpenSave();
        }

        void RestoreTimeScale()
        {
            if (!pausedTime)
                return;
            Time.timeScale = savedTimeScale <= 0f ? 1f : savedTimeScale;
            pausedTime = false;
        }

        void ConsumeUnlock()
        {
            if (gameplayCursor != null)
                gameplayCursor.IgnoreUnlockThisFrame();
        }
    }
}
