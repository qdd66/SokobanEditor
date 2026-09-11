using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class BackpackPanelView : MonoBehaviour
    {
        [LabelText("物品目录")]
        [Tooltip("背包展示的可放置物品表。")]
        [SerializeField, Required("请指定物品目录"), AssetsOnly]
        PlaceableCatalog catalog;

        [LabelText("未分类")]
        [Tooltip("没有填分类的物品归到这一组。")]
        [SerializeField, Required("请指定未分类"), AssetsOnly]
        PlaceableCategoryDef uncategorized;

        [LabelText("格子预制体")]
        [Tooltip("单个物品格子。")]
        [SerializeField, Required("请指定格子预制体")]
        WarehouseSlotView slotPrefab;

        [LabelText("分类模板")]
        [Tooltip("分组标题和格子容器。运行时会复制。")]
        [SerializeField, Required("请指定分类模板")]
        BackpackCategorySectionView sectionTemplate;

        [LabelText("内容根节点")]
        [Tooltip("滚动区内生成分组的父节点。")]
        [SerializeField, Required("请指定内容根节点")]
        RectTransform contentRoot;

        [LabelText("每行列数")]
        [Tooltip("每个分类网格的列数。")]
        [SerializeField, MinValue(1)]
        int columns = 8;

        readonly List<GameObject> spawned = new List<GameObject>();

        public event Action<PlaceableItemDef> ItemPicked;
        public event Action Closed;

        public bool IsOpen => gameObject.activeSelf;

        public void BindCatalog(PlaceableCatalog value)
        {
            catalog = value;
            if (isActiveAndEnabled)
                Rebuild();
        }

        void OnEnable()
        {
            Rebuild();
        }

        public void SetOpen(bool open)
        {
            if (gameObject.activeSelf == open)
                return;

            gameObject.SetActive(open);
            if (!open)
                Closed?.Invoke();
        }

        public void CloseFromUi()
        {
            SetOpen(false);
        }

        public void Rebuild()
        {
            ClearSpawned();
            if (catalog == null || slotPrefab == null || sectionTemplate == null || contentRoot == null)
                return;

            var groups = PlaceableCategoryGrouping.Build(catalog.Items, uncategorized);
            for (var i = 0; i < groups.Count; i++)
                SpawnGroup(groups[i]);
        }

        void SpawnGroup(PlaceableCategoryGroup group)
        {
            var section = Instantiate(sectionTemplate, contentRoot);
            section.gameObject.SetActive(true);
            section.SetTitle(group.Category != null ? group.Category.DisplayName : "未分类");
            spawned.Add(section.gameObject);

            var itemsRoot = section.ItemsRoot != null ? section.ItemsRoot : section.transform as RectTransform;
            var items = group.Items;
            for (var i = 0; i < items.Count; i++)
            {
                var slot = Instantiate(slotPrefab, itemsRoot);
                slot.gameObject.SetActive(true);
                slot.Bind(items[i], false);
                slot.SetClickedHandler(HandleItemClicked);
                spawned.Add(slot.gameObject);
            }

            ApplyColumnCount(itemsRoot);
        }

        void HandleItemClicked(PlaceableItemDef def)
        {
            ItemPicked?.Invoke(def);
            SetOpen(false);
        }

        void ApplyColumnCount(RectTransform itemsRoot)
        {
            if (itemsRoot == null)
                return;
            var grid = itemsRoot.GetComponent<UnityEngine.UI.GridLayoutGroup>();
            if (grid == null)
                return;
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, columns);
        }

        void ClearSpawned()
        {
            for (var i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null)
                    Destroy(spawned[i]);
            }

            spawned.Clear();
        }
    }
}
