using UnityEngine;

namespace DZDMapEditor
{
    public readonly struct PlacementMutation
    {
        public enum Kind
        {
            None,
            PlacedNew,
            Transformed,
            Deleted
        }

        public PlacementMutation(
            Kind type,
            PlacedItem item,
            Vector3 beforePosition,
            Quaternion beforeRotation,
            Vector3 beforeScale,
            Vector3 afterPosition,
            Quaternion afterRotation,
            Vector3 afterScale,
            MapItemRecord deletedRecord)
        {
            Type = type;
            Item = item;
            BeforePosition = beforePosition;
            BeforeRotation = beforeRotation;
            BeforeScale = beforeScale;
            AfterPosition = afterPosition;
            AfterRotation = afterRotation;
            AfterScale = afterScale;
            DeletedRecord = deletedRecord;
        }

        public Kind Type { get; }
        public PlacedItem Item { get; }
        public Vector3 BeforePosition { get; }
        public Quaternion BeforeRotation { get; }
        public Vector3 BeforeScale { get; }
        public Vector3 AfterPosition { get; }
        public Quaternion AfterRotation { get; }
        public Vector3 AfterScale { get; }
        public MapItemRecord DeletedRecord { get; }
        public bool HasValue => Type != Kind.None;

        public static PlacementMutation PlacedNew(PlacedItem item)
        {
            if (item == null)
                return default;
            var t = item.transform;
            return new PlacementMutation(
                Kind.PlacedNew,
                item,
                t.position,
                t.rotation,
                t.localScale,
                t.position,
                t.rotation,
                t.localScale,
                default);
        }

        public static PlacementMutation Transformed(
            PlacedItem item,
            Vector3 beforePosition,
            Quaternion beforeRotation,
            Vector3 beforeScale)
        {
            if (item == null)
                return default;
            var t = item.transform;
            return new PlacementMutation(
                Kind.Transformed,
                item,
                beforePosition,
                beforeRotation,
                beforeScale,
                t.position,
                t.rotation,
                t.localScale,
                default);
        }

        public static PlacementMutation Deleted(MapItemRecord record)
        {
            if (!record.IsValid)
                return default;
            return new PlacementMutation(
                Kind.Deleted,
                null,
                record.Position,
                record.Rotation,
                record.Scale,
                record.Position,
                record.Rotation,
                record.Scale,
                record);
        }
    }
}
