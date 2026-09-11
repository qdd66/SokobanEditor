using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public sealed class PauseMenuView : MonoBehaviour
    {
        [LabelText("主页")]
        [SerializeField, Required("请指定主页")]
        GameObject mainPage;

        [LabelText("保存页")]
        [SerializeField, Required("请指定保存页")]
        GameObject savePage;

        [LabelText("标题")]
        [SerializeField]
        TMP_Text title;

        [LabelText("提示")]
        [SerializeField]
        TMP_Text hint;

        [LabelText("回到游戏")]
        [SerializeField]
        Button resumeButton;

        [LabelText("保存场景")]
        [SerializeField]
        Button saveButton;

        [LabelText("加载场景")]
        [SerializeField]
        Button loadButton;

        [LabelText("设置")]
        [SerializeField]
        Button settingsButton;

        [LabelText("回到游戏文字")]
        [SerializeField]
        TMP_Text resumeLabel;

        [LabelText("保存场景文字")]
        [SerializeField]
        TMP_Text saveLabel;

        [LabelText("加载场景文字")]
        [SerializeField]
        TMP_Text loadLabel;

        [LabelText("设置文字")]
        [SerializeField]
        TMP_Text settingsLabel;

        [LabelText("菜单窗口")]
        [SerializeField]
        GameObject menuWindow;

        [LabelText("设置页")]
        [SerializeField]
        GameObject settingsPage;

        [LabelText("设置面板")]
        [SerializeField]
        SettingsPanelView settingsView;

        public event Action ResumeClicked;
        public event Action SaveClicked;
        public event Action LoadClicked;
        public event Action SettingsClicked;

        public bool IsOpen => gameObject.activeSelf;
        public bool IsSavePageOpen => savePage != null && savePage.activeSelf;
        public bool IsSettingsPageOpen => settingsPage != null && settingsPage.activeSelf;
        public SettingsPanelView SettingsView => settingsView;

        void OnEnable()
        {
            Bind(resumeButton, HandleResume);
            Bind(saveButton, HandleSave);
            Bind(loadButton, HandleLoad);
            Bind(settingsButton, HandleSettings);
        }

        public void ApplyConfig(PauseMenuConfig config)
        {
            if (config == null)
                return;
            if (title != null)
                title.text = config.Title;
            if (hint != null)
                hint.text = config.Hint;
            if (resumeLabel != null)
                resumeLabel.text = config.ResumeLabel;
            if (saveLabel != null)
                saveLabel.text = config.SaveLabel;
            if (loadLabel != null)
                loadLabel.text = config.LoadLabel;
            if (settingsLabel != null)
                settingsLabel.text = config.SettingsLabel;
        }

        public void SetOpen(bool open)
        {
            gameObject.SetActive(open);
            if (open)
                ShowMain();
        }

        public void ShowMain()
        {
            if (menuWindow != null)
                menuWindow.SetActive(true);
            if (mainPage != null)
                mainPage.SetActive(true);
            if (savePage != null)
                savePage.SetActive(false);
            if (settingsPage != null)
                settingsPage.SetActive(false);
            if (settingsView != null)
                settingsView.CancelListen();
        }

        public void ShowSave()
        {
            if (menuWindow != null)
                menuWindow.SetActive(true);
            if (mainPage != null)
                mainPage.SetActive(false);
            if (savePage != null)
                savePage.SetActive(true);
            if (settingsPage != null)
                settingsPage.SetActive(false);
            if (settingsView != null)
                settingsView.CancelListen();
        }

        public void ShowSettings()
        {
            if (menuWindow != null)
                menuWindow.SetActive(false);
            if (savePage != null)
                savePage.SetActive(false);
            if (settingsPage != null)
                settingsPage.SetActive(true);
        }

        public void CloseFromUi()
        {
            ResumeClicked?.Invoke();
        }

        void HandleResume()
        {
            ResumeClicked?.Invoke();
        }

        void HandleSave()
        {
            SaveClicked?.Invoke();
        }

        void HandleLoad()
        {
            LoadClicked?.Invoke();
        }

        void HandleSettings()
        {
            SettingsClicked?.Invoke();
        }

        static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}
