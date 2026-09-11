using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class WarehouseHotbarView : MonoBehaviour
    {
        [LabelText("物品目录")]
        [Tooltip("热键栏展示的可放置物品表。")]
        [SerializeField, Required("请指定物品目录"), AssetsOnly]
        PlaceableCatalog catalog;

        [LabelText("格子预制体")]
        [Tooltip("单个仓库格子的 UI 预制体。")]
        [SerializeField, Required("请指定格子预制体")]
        WarehouseSlotView slotPrefab;

        [LabelText("格子根节点")]
        [Tooltip("生成出来的格子挂在这个 RectTransform 下。")]
        [SerializeField, Required("请指定格子根节点")]
        RectTransform slotRoot;

        [LabelText("最多可见格数")]
        [Tooltip("同时显示的格子数量，超出部分靠滚轮循环。")]
        [SerializeField, MinValue(1)]
        int maxVisibleSlots = 9;

        readonly List<WarehouseSlotView> slots = new List<WarehouseSlotView>();
        int selectedIndex;

        public PlaceableItemDef Selected
        {
            get
            {
                var items = catalog != null ? catalog.Items : null;
                if (items == null || items.Count == 0)
                    return null;
                return items[Wrap(selectedIndex, items.Count)];
            }
        }

        public void BindCatalog(PlaceableCatalog value)
        {
            catalog = value;
            selectedIndex = 0;
            Rebuild();
        }

        void Awake()
        {
            Rebuild();
        }

        public void ApplyScroll(int steps)
        {
            var items = catalog != null ? catalog.Items : null;
            if (items == null || items.Count == 0 || steps == 0)
                return;

            selectedIndex = Wrap(selectedIndex - steps, items.Count);
            Refresh();
        }

        public bool Select(PlaceableItemDef def)
        {
            var items = catalog != null ? catalog.Items : null;
            if (def == null || items == null)
                return false;

            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] != def)
                    continue;
                selectedIndex = i;
                Refresh();
                return true;
            }

            return false;
        }

        public void Rebuild()
        {
            for (var i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i] != null)
                    Destroy(slots[i].gameObject);
            }

            slots.Clear();
            selectedIndex = 0;

            if (slotPrefab == null || slotRoot == null)
                return;

            var visible = VisibleCount();
            for (var i = 0; i < visible; i++)
            {
                var slot = Instantiate(slotPrefab, slotRoot);
                slot.gameObject.SetActive(true);
                slots.Add(slot);
            }

            Refresh();
        }

        void Refresh()
        {
            var items = catalog != null ? catalog.Items : null;
            var count = items != null ? items.Count : 0;
            var visible = slots.Count;
            if (visible == 0)
                return;

            var start = 0;
            if (count > visible)
                start = Wrap(selectedIndex - visible / 2, count);

            for (var i = 0; i < visible; i++)
            {
                if (count == 0)
                {
                    slots[i].Bind(null, false);
                    continue;
                }

                var index = count > visible ? Wrap(start + i, count) : i;
                var def = index < count ? items[index] : null;
                slots[i].Bind(def, def != null && index == Wrap(selectedIndex, count));
            }
        }

        int VisibleCount()
        {
            var count = catalog != null && catalog.Items != null ? catalog.Items.Count : 0;
            if (count <= 0)
                return 0;
            return Mathf.Min(maxVisibleSlots, count);
        }

        static int Wrap(int value, int count)
        {
            if (count <= 0)
                return 0;
            var wrapped = value % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }
    }
}
