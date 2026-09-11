using UnityEngine;

namespace DZDMapEditor
{
    public static class PlacementGrid
    {
        public static Vector2Int WorldToCell(Vector3 world, PlacementConfig config)
        {
            var size = CellSize(config);
            var origin = Origin(config);
            return new Vector2Int(
                Mathf.FloorToInt((world.x - origin.x) / size),
                Mathf.FloorToInt((world.z - origin.y) / size));
        }

        public static Vector3 CellCenter(Vector2Int cell, float y, PlacementConfig config)
        {
            var size = CellSize(config);
            var origin = Origin(config);
            return new Vector3(
                origin.x + (cell.x + 0.5f) * size,
                y,
                origin.y + (cell.y + 0.5f) * size);
        }

        public static Vector3 CellMin(Vector2Int cell, float y, PlacementConfig config)
        {
            var size = CellSize(config);
            var origin = Origin(config);
            return new Vector3(
                origin.x + cell.x * size,
                y,
                origin.y + cell.y * size);
        }

        public static float BaseY(PlacementConfig config)
        {
            return SnapY(0f, config);
        }

        public static float SnapY(float y, PlacementConfig config)
        {
            var size = CellSize(config);
            return Mathf.Floor(y / size + 1e-4f) * size;
        }

        public static PlacementHit SnapHit(in PlacementHit hit, PlacementConfig config)
        {
            if (!hit.HasHit || config == null)
                return hit;

            var cell = WorldToCell(hit.Point, config);
            return new PlacementHit(true, CellCenter(cell, hit.Point.y, config), Vector3.up, hit.PlacedItem);
        }

        public static float CellSize(PlacementConfig config)
        {
            return config != null ? Mathf.Max(0.05f, config.GridCellSize) : 1f;
        }

        static Vector2 Origin(PlacementConfig config)
        {
            return config != null ? config.GridOrigin : Vector2.zero;
        }
    }
}
