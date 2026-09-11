using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DZDMapEditor
{
    public sealed class WarehouseSlotView : MonoBehaviour, IPointerClickHandler
    {
        [LabelText("背景")]
        [Tooltip("格子背景图。可空。")]
        [SerializeField]
        Image background;

        [LabelText("图标")]
        [Tooltip("显示物品图标的 Image。")]
        [SerializeField]
        Image icon;

        [LabelText("名称")]
        [Tooltip("显示物品名称的 TMP 文本。")]
        [SerializeField]
        TMP_Text label;

        [LabelText("高亮")]
        [Tooltip("当前选中时显示的高亮物体。")]
        [SerializeField]
        GameObject highlight;

        PlaceableItemDef bound;
        Action<PlaceableItemDef> clicked;

        public void Bind(PlaceableItemDef def, bool selected)
        {
            bound = def;
            BindVisual(def != null ? def.DisplayName : string.Empty, def != null ? def.Icon : null, selected);
        }

        public void BindVisual(string displayName, Sprite sprite, bool selected)
        {
            var hasDef = !string.IsNullOrEmpty(displayName) || sprite != null;
            if (label != null)
            {
                label.text = displayName ?? string.Empty;
                label.enabled = hasDef;
            }

            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }

            if (highlight != null)
                highlight.SetActive(selected);

            transform.localScale = selected ? Vector3.one * 1.12f : Vector3.one;
        }

        public void SetClickedHandler(Action<PlaceableItemDef> handler)
        {
            clicked = handler;
            if (background != null)
                background.raycastTarget = handler != null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;
            if (bound == null || clicked == null)
                return;
            clicked.Invoke(bound);
        }
    }
}
