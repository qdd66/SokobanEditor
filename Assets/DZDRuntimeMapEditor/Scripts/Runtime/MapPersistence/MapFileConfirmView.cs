using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public sealed class MapFileConfirmView : MonoBehaviour
    {
        [LabelText("标题")]
        [SerializeField]
        TMP_Text title;

        [LabelText("说明")]
        [SerializeField]
        TMP_Text message;

        [LabelText("确认")]
        [SerializeField]
        Button confirmButton;

        [LabelText("取消")]
        [SerializeField]
        Button cancelButton;

        [LabelText("确认文字")]
        [SerializeField]
        TMP_Text confirmLabel;

        [LabelText("取消文字")]
        [SerializeField]
        TMP_Text cancelLabel;

        Action confirmed;

        public bool IsOpen => gameObject.activeSelf;

        void OnEnable()
        {
            Bind(confirmButton, HandleConfirm);
            Bind(cancelButton, HandleCancel);
        }

        public void Show(
            string titleText,
            string messageText,
            string confirmText,
            string cancelText,
            Action onConfirm)
        {
            confirmed = onConfirm;
            if (title != null)
                title.text = titleText ?? string.Empty;
            if (message != null)
                message.text = messageText ?? string.Empty;
            if (confirmLabel != null)
                confirmLabel.text = confirmText ?? string.Empty;
            if (cancelLabel != null)
                cancelLabel.text = cancelText ?? string.Empty;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            confirmed = null;
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        void HandleConfirm()
        {
            var action = confirmed;
            Hide();
            action?.Invoke();
        }

        void HandleCancel()
        {
            Hide();
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
