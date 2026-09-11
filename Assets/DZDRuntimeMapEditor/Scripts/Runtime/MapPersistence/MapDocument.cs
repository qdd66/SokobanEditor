using System;
using UnityEngine;

namespace DZDMapEditor
{
    [Serializable]
    public sealed class MapDocumentJson
    {
        public int formatVersion = 1;
        public string mapName;
        public bool hasCamera;
        public Vector3 cameraPosition;
        public float cameraYaw;
        public float cameraPitch;
        public MapItemJson[] items;
    }

    [Serializable]
    public sealed class MapItemJson
    {
        public string instanceId;
        public string defId;
        public Vector3 position;
        public Vector3 euler;
        public Vector3 scale;
    }

    public readonly struct MapItemRecord
    {
        public MapItemRecord(
            string instanceId,
            string defId,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            InstanceId = instanceId;
            DefId = defId;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public string InstanceId { get; }
        public string DefId { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }

        public bool IsValid => !string.IsNullOrEmpty(InstanceId) && !string.IsNullOrEmpty(DefId);

        public static MapItemRecord FromItem(PlacedItem item)
        {
            if (item == null)
                return default;

            var t = item.transform;
            var defId = item.Definition != null ? item.Definition.Id : string.Empty;
            return new MapItemRecord(item.InstanceId, defId, t.position, t.rotation, t.localScale);
        }

        public MapItemJson ToJson()
        {
            return new MapItemJson
            {
                instanceId = InstanceId,
                defId = DefId,
                position = Position,
                euler = Rotation.eulerAngles,
                scale = Scale
            };
        }

        public static MapItemRecord FromJson(MapItemJson json)
        {
            if (json == null)
                return default;

            return new MapItemRecord(
                json.instanceId,
                json.defId,
                json.position,
                Quaternion.Euler(json.euler),
                json.scale);
        }
    }

    public sealed class MapFileInfo
    {
        public string Path;
        public string FileName;
        public string DisplayName;
        public DateTime LastWriteTimeUtc;
        public long ByteLength;
        public int ItemCount;
        public int FormatVersion;
        public bool IsReadable;
        public string Error;

        public string DetailsLine
        {
            get
            {
                if (!IsReadable)
                    return string.IsNullOrEmpty(Error)
                        ? MapEditorLocale.Pick("Can't read this file", "无法读取")
                        : Error;

                var local = LastWriteTimeUtc.ToLocalTime();
                var time = local.ToString("yyyy-MM-dd HH:mm");
                var size = FormatSize(ByteLength);
                var items = MapEditorLocale.Pick(
                    ItemCount == 1 ? "1 object" : ItemCount + " objects",
                    ItemCount + " 个物体");
                return time + "  ·  " + size + "  ·  " + items;
            }
        }

        static string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return bytes + " B";
            if (bytes < 1024 * 1024)
                return (bytes / 1024f).ToString("0.#") + " KB";
            return (bytes / (1024f * 1024f)).ToString("0.##") + " MB";
        }
    }

    public readonly struct CameraPose
    {
        public CameraPose(Vector3 position, float yaw, float pitch)
        {
            Position = position;
            Yaw = yaw;
            Pitch = pitch;
        }

        public Vector3 Position { get; }
        public float Yaw { get; }
        public float Pitch { get; }
    }
}
