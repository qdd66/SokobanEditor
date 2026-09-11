using UnityEngine;

namespace Sokoban
{
    public static class SokobanGrid
    {
        public static Vector2Int WorldToCell(Vector3 world, SokobanConfig config)
        {
            var size = CellSize(config);
            var origin = Origin(config);
            return new Vector2Int(
                Mathf.FloorToInt((world.x - origin.x) / size),
                Mathf.FloorToInt((world.z - origin.y) / size));
        }

        public static Vector3 CellCenter(Vector2Int cell, float y, SokobanConfig config)
        {
            var size = CellSize(config);
            var origin = Origin(config);
            return new Vector3(
                origin.x + (cell.x + 0.5f) * size,
                y,
                origin.y + (cell.y + 0.5f) * size);
        }

        public static float CellSize(SokobanConfig config)
        {
            return config != null ? Mathf.Max(0.05f, config.CellSize) : 1f;
        }

        static Vector2 Origin(SokobanConfig config)
        {
            return config != null ? config.GridOrigin : Vector2.zero;
        }
    }
}
