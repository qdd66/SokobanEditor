using UnityEngine;

namespace DZDMapEditor
{
    public readonly struct PlacementHit
    {
        public PlacementHit(bool hasHit, Vector3 point, Vector3 normal, PlacedItem placedItem)
        {
            HasHit = hasHit;
            Point = point;
            Normal = normal;
            PlacedItem = placedItem;
        }

        public bool HasHit { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public PlacedItem PlacedItem { get; }
    }
}
