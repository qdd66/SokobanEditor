using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class BackpackCategorySectionView : MonoBehaviour
    {
        [LabelText("标题")]
        [Tooltip("分类名称。")]
        [SerializeField]
        TMP_Text header;

        [LabelText("格子根节点")]
        [Tooltip("这一分类下的物品格子挂在这里。")]
        [SerializeField, Required("请指定格子根节点")]
        RectTransform itemsRoot;

        public RectTransform ItemsRoot => itemsRoot;

        public void SetTitle(string title)
        {
            if (header != null)
                header.text = title ?? string.Empty;
        }
    }
}
