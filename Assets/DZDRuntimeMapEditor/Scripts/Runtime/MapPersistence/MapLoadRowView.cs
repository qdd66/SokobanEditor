using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public sealed class MapLoadRowView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [LabelText("标题")]
        [SerializeField, Required("请指定标题文本")]
        TMP_Text title;

        [LabelText("详情")]
        [SerializeField, Required("请指定详情文本")]
        TMP_Text details;

        [LabelText("背景")]
        [SerializeField]
        Image background;

        [LabelText("改名按钮")]
        [SerializeField]
        Button renameButton;

        [LabelText("删除按钮")]
        [SerializeField]
        Button deleteButton;

        [LabelText("改名文字")]
        [SerializeField]
        TMP_Text renameLabel;

        [LabelText("删除文字")]
        [SerializeField]
        TMP_Text deleteLabel;

        [LabelText("改名输入")]
        [SerializeField]
        TMP_InputField nameField;

        [LabelText("正常颜色")]
        [SerializeField]
        Color normalColor = new Color(1f, 1f, 1f, 0.06f);

        [LabelText("悬停颜色")]
        [SerializeField]
        Color hoverColor = new Color(0.35f, 0.7f, 1f, 0.28f);

        [LabelText("不可用颜色")]
        [SerializeField]
        Color disabledColor = new Color(1f, 0.4f, 0.35f, 0.12f);

        MapFileInfo bound;
        Action<MapFileInfo> clicked;
        Func<MapFileInfo, string, bool> renamed;
        Action<MapFileInfo> deleted;
        bool renaming;

        public bool IsRenaming => renaming;

        void OnEnable()
        {
            BindButton(renameButton, HandleRenameClicked);
            BindButton(deleteButton, HandleDeleteClicked);
            if (nameField != null)
            {
                nameField.onSubmit.RemoveListener(HandleSubmit);
                nameField.onSubmit.AddListener(HandleSubmit);
            }
        }

        void OnDisable()
        {
            if (nameField != null)
                nameField.onSubmit.RemoveListener(HandleSubmit);
            SetRenaming(false);
        }

        public void Bind(
            MapFileInfo info,
            Action<MapFileInfo> onClicked,
            Func<MapFileInfo, string, bool> onRename,
            Action<MapFileInfo> onDelete,
            MapPersistenceConfig config)
        {
            bound = info;
            clicked = onClicked;
            renamed = onRename;
            deleted = onDelete;
            SetRenaming(false);

            var readable = info != null && info.IsReadable;
            if (title != null)
                title.text = info != null ? info.DisplayName : string.Empty;
            if (details != null)
                details.text = info != null ? info.DetailsLine : string.Empty;
            if (background != null)
            {
                background.raycastTarget = true;
                background.color = readable ? normalColor : disabledColor;
            }

            if (renameLabel != null && config != null)
                renameLabel.text = config.RenameLabel;
            if (deleteLabel != null && config != null)
                deleteLabel.text = config.DeleteLabel;
            if (nameField != null && nameField.placeholder is TMP_Text placeholder && config != null)
                placeholder.text = config.RenamePlaceholder;

            var canManage = info != null && onRename != null && onDelete != null;
            if (renameButton != null)
                renameButton.gameObject.SetActive(canManage);
            if (deleteButton != null)
                deleteButton.gameObject.SetActive(canManage);
        }

        public void CancelRename()
        {
            SetRenaming(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (renaming)
                return;
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;
            if (bound == null || !bound.IsReadable || clicked == null)
                return;
            var hit = eventData.pointerCurrentRaycast.gameObject;
            if (hit != null)
            {
                if (hit.GetComponentInParent<Button>() != null)
                    return;
                if (hit.GetComponentInParent<TMP_InputField>() != null)
                    return;
            }
            clicked.Invoke(bound);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (background == null || bound == null || !bound.IsReadable || renaming)
                return;
            background.color = hoverColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (background == null || bound == null)
                return;
            background.color = bound.IsReadable ? normalColor : disabledColor;
        }

        void HandleRenameClicked()
        {
            if (bound == null || renamed == null)
                return;
            SetRenaming(true);
            if (nameField == null)
                return;
            nameField.text = bound.DisplayName;
            nameField.ActivateInputField();
            nameField.Select();
        }

        void HandleDeleteClicked()
        {
            if (bound == null || deleted == null)
                return;
            deleted.Invoke(bound);
        }

        void HandleSubmit(string value)
        {
            if (!renaming || bound == null)
                return;
            if (renamed != null && !renamed.Invoke(bound, value))
            {
                if (nameField != null)
                    nameField.ActivateInputField();
                return;
            }

            SetRenaming(false);
        }

        void SetRenaming(bool value)
        {
            renaming = value;
            if (title != null)
                title.gameObject.SetActive(!value);
            if (nameField != null)
                nameField.gameObject.SetActive(value);
            if (renameButton != null)
                renameButton.gameObject.SetActive(!value && renamed != null);
            if (deleteButton != null)
                deleteButton.gameObject.SetActive(!value && deleted != null);
        }

        static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}
