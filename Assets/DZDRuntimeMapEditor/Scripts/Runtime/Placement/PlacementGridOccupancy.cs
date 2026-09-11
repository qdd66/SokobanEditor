using System.Collections.Generic;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class PlacementGridOccupancy
    {
        readonly List<PlacedItem> buffer = new List<PlacedItem>(64);

        public PlacementBlockReason Evaluate(
            Transform placedRoot,
            Vector2Int cell,
            Transform ignore,
            PlacementConfig config,
            PlaceableItemDef placing,
            bool relocatingExisting,
            bool checkCell)
        {
            if (config == null || placedRoot == null)
                return PlacementBlockReason.None;

            buffer.Clear();
            placedRoot.GetComponentsInChildren(false, buffer);

            if (!relocatingExisting && placing != null && placing.MaxInstances > 0)
            {
                var count = 0;
                for (var i = 0; i < buffer.Count; i++)
                {
                    var item = buffer[i];
                    if (item == null || ShouldIgnore(item.transform, ignore))
                        continue;
                    if (!SameDefinition(item, placing))
                        continue;
                    count++;
                    if (count >= placing.MaxInstances)
                        return PlacementBlockReason.InstanceCap;
                }
            }

            if (!checkCell || !config.GridOccupancy)
                return PlacementBlockReason.None;

            var placingLayer = placing != null ? placing.OccupancyLayer : PlaceableOccupancyLayer.Solid;
            for (var i = 0; i < buffer.Count; i++)
            {
                var item = buffer[i];
                if (item == null || ShouldIgnore(item.transform, ignore))
                    continue;
                if (PlacementGrid.WorldToCell(item.transform.position, config) != cell)
                    continue;
                if (Conflicts(placingLayer, LayerOf(item)))
                    return PlacementBlockReason.Occupied;
            }

            return PlacementBlockReason.None;
        }

        public bool IsOccupied(
            Transform placedRoot,
            Vector2Int cell,
            Transform ignore,
            PlacementConfig config)
        {
            return Evaluate(
                placedRoot,
                cell,
                ignore,
                config,
                null,
                false,
                true) != PlacementBlockReason.None;
        }

        static bool ShouldIgnore(Transform transform, Transform ignore)
        {
            return ignore != null && (transform == ignore || transform.IsChildOf(ignore));
        }

        static bool SameDefinition(PlacedItem item, PlaceableItemDef placing)
        {
            if (item == null || placing == null)
                return false;
            if (item.Definition == placing)
                return true;
            return item.Definition != null &&
                   string.Equals(item.Definition.Id, placing.Id, System.StringComparison.Ordinal);
        }

        static PlaceableOccupancyLayer LayerOf(PlacedItem item)
        {
            return item != null && item.Definition != null
                ? item.Definition.OccupancyLayer
                : PlaceableOccupancyLayer.Solid;
        }

        static bool Conflicts(PlaceableOccupancyLayer placing, PlaceableOccupancyLayer existing)
        {
            return placing == existing;
        }
    }
}
