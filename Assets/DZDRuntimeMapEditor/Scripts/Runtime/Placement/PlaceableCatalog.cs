using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "PlaceableCatalog",
        menuName = MapEditorInfo.CreateMenu + "/Placeable Catalog")]
    public sealed class PlaceableCatalog : ScriptableObject
    {
        [LabelText("物品列表")]
        [Tooltip("仓库热键栏展示的可放置物品，顺序即滚轮切换顺序。")]
        [ListDrawerSettings(DraggableItems = true, ShowFoldout = false)]
        [SerializeField]
        List<PlaceableItemDef> items = new List<PlaceableItemDef>();

        public IReadOnlyList<PlaceableItemDef> Items => items;

        public bool TryGet(string id, out PlaceableItemDef def)
        {
            def = null;
            if (string.IsNullOrEmpty(id) || items == null)
                return false;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null)
                    continue;
                if (string.Equals(item.Id, id, StringComparison.Ordinal) ||
                    string.Equals(item.name, id, StringComparison.Ordinal))
                {
                    def = item;
                    return true;
                }
            }

            return false;
        }
    }
}
