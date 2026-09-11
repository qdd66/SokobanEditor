using System.Collections.Generic;

namespace DZDMapEditor
{
    public readonly struct PlaceableCategoryGroup
    {
        public PlaceableCategoryGroup(PlaceableCategoryDef category, IReadOnlyList<PlaceableItemDef> items)
        {
            Category = category;
            Items = items;
        }

        public PlaceableCategoryDef Category { get; }
        public IReadOnlyList<PlaceableItemDef> Items { get; }
    }

    public static class PlaceableCategoryGrouping
    {
        public static List<PlaceableCategoryGroup> Build(
            IReadOnlyList<PlaceableItemDef> items,
            PlaceableCategoryDef uncategorized)
        {
            var groups = new List<PlaceableCategoryGroup>();
            if (items == null || items.Count == 0)
                return groups;

            var map = new Dictionary<PlaceableCategoryDef, List<PlaceableItemDef>>();
            var order = new List<PlaceableCategoryDef>();

            for (var i = 0; i < items.Count; i++)
            {
                var def = items[i];
                if (def == null)
                    continue;

                var category = def.Category != null ? def.Category : uncategorized;
                if (category == null)
                    continue;

                if (!map.TryGetValue(category, out var list))
                {
                    list = new List<PlaceableItemDef>();
                    map.Add(category, list);
                    order.Add(category);
                }

                list.Add(def);
            }

            order.Sort(CompareCategories);
            for (var i = 0; i < order.Count; i++)
                groups.Add(new PlaceableCategoryGroup(order[i], map[order[i]]));

            return groups;
        }

        static int CompareCategories(PlaceableCategoryDef a, PlaceableCategoryDef b)
        {
            var cmp = a.SortOrder.CompareTo(b.SortOrder);
            if (cmp != 0)
                return cmp;
            return string.CompareOrdinal(a.DisplayName, b.DisplayName);
        }
    }
}
