using System.Collections.Generic;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class PlacedItemWorld : IPlacedItemWorld
    {
        readonly PlaceableCatalog catalog;
        readonly Transform root;
        readonly Dictionary<string, PlacedItem> byId = new Dictionary<string, PlacedItem>();
        readonly List<string> missingIds = new List<string>();

        public PlacedItemWorld(PlaceableCatalog catalog, Transform root)
        {
            this.catalog = catalog;
            this.root = root;
        }

        public IReadOnlyList<string> LastMissingIds => missingIds;

        public void Register(PlacedItem item)
        {
            if (item == null)
                return;
            item.EnsureInstanceId();
            byId[item.InstanceId] = item;
        }

        public void Unregister(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;
            byId.Remove(instanceId);
        }

        public PlacedItem Spawn(MapItemRecord record)
        {
            if (!record.IsValid || catalog == null)
                return null;
            if (!catalog.TryGet(record.DefId, out var def) || def.Prefab == null)
                return null;

            var go = Object.Instantiate(def.Prefab, root);
            go.name = def.Prefab.name;
            var item = PlacedItem.Ensure(go, def);
            item.BindInstanceId(record.InstanceId);
            go.transform.SetPositionAndRotation(record.Position, record.Rotation);
            go.transform.localScale = record.Scale;
            byId[record.InstanceId] = item;
            return item;
        }

        public void Destroy(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;
            if (!byId.TryGetValue(instanceId, out var item))
                return;

            byId.Remove(instanceId);
            if (item != null)
                Object.DestroyImmediate(item.gameObject);
        }

        public void SetPose(string instanceId, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!byId.TryGetValue(instanceId, out var item) || item == null)
                return;

            item.transform.SetPositionAndRotation(position, rotation);
            item.transform.localScale = scale;
        }

        public void ClearAll()
        {
            if (root != null)
            {
                for (var i = root.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
            else
            {
                foreach (var pair in byId)
                {
                    if (pair.Value != null)
                        Object.DestroyImmediate(pair.Value.gameObject);
                }
            }

            byId.Clear();
        }

        public List<MapItemRecord> CaptureAll()
        {
            RebuildFromRoot();
            var list = new List<MapItemRecord>(byId.Count);
            foreach (var pair in byId)
            {
                var item = pair.Value;
                if (item == null || item.Definition == null)
                    continue;
                item.EnsureInstanceId();
                list.Add(MapItemRecord.FromItem(item));
            }

            return list;
        }

        public int ApplyAll(IReadOnlyList<MapItemRecord> records)
        {
            missingIds.Clear();
            ClearAll();
            if (records == null)
                return 0;

            var spawned = 0;
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (!record.IsValid)
                    continue;
                if (Spawn(record) != null)
                {
                    spawned++;
                    continue;
                }

                missingIds.Add(record.DefId);
            }

            return spawned;
        }

        void RebuildFromRoot()
        {
            byId.Clear();
            if (root == null)
                return;

            var items = root.GetComponentsInChildren<PlacedItem>(true);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null || item.Definition == null)
                    continue;
                item.EnsureInstanceId();
                byId[item.InstanceId] = item;
            }
        }
    }
}
