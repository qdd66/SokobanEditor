using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public sealed class StartMenuView : MonoBehaviour
    {
        [LabelText("标题")]
        [SerializeField]
        TMP_Text title;

        [LabelText("提示")]
        [SerializeField]
        TMP_Text hint;

        [LabelText("打开存档")]
        [SerializeField]
        Button openSaveButton;

        [LabelText("新建地图")]
        [SerializeField]
        Button newMapButton;

        [LabelText("打开存档文字")]
        [SerializeField]
        TMP_Text openSaveLabel;

        [LabelText("新建地图文字")]
        [SerializeField]
        TMP_Text newMapLabel;

        public event Action OpenSaveClicked;
        public event Action NewMapClicked;

        void OnEnable()
        {
            Bind(openSaveButton, HandleOpenSave);
            Bind(newMapButton, HandleNewMap);
        }

        public void ApplyConfig(StartMenuConfig config)
        {
            if (config == null)
                return;
            if (title != null)
                title.text = config.Title;
            if (hint != null)
                hint.text = config.Hint;
            if (openSaveLabel != null)
                openSaveLabel.text = config.OpenSaveLabel;
            if (newMapLabel != null)
                newMapLabel.text = config.NewMapLabel;
        }

        void HandleOpenSave()
        {
            OpenSaveClicked?.Invoke();
        }

        void HandleNewMap()
        {
            NewMapClicked?.Invoke();
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
