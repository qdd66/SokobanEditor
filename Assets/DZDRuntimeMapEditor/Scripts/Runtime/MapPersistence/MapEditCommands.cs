using System.Collections.Generic;

namespace DZDMapEditor
{
    public interface IMapEditCommand
    {
        void Undo();
        void Redo();
    }

    public interface IPlacedItemWorld
    {
        void Register(PlacedItem item);
        void Unregister(string instanceId);
        PlacedItem Spawn(MapItemRecord record);
        void Destroy(string instanceId);
        void SetPose(string instanceId, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation, UnityEngine.Vector3 scale);
        void ClearAll();
        List<MapItemRecord> CaptureAll();
    }

    public sealed class MapHistory
    {
        readonly int maxSteps;
        readonly List<IMapEditCommand> undo = new List<IMapEditCommand>();
        readonly List<IMapEditCommand> redo = new List<IMapEditCommand>();

        public MapHistory(int maxSteps)
        {
            this.maxSteps = UnityEngine.Mathf.Max(1, maxSteps);
        }

        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;

        public void Record(IMapEditCommand command)
        {
            if (command == null)
                return;

            undo.Add(command);
            while (undo.Count > maxSteps)
                undo.RemoveAt(0);
            redo.Clear();
        }

        public bool Undo()
        {
            if (undo.Count == 0)
                return false;

            var last = undo.Count - 1;
            var command = undo[last];
            undo.RemoveAt(last);
            command.Undo();
            redo.Add(command);
            return true;
        }

        public bool Redo()
        {
            if (redo.Count == 0)
                return false;

            var last = redo.Count - 1;
            var command = redo[last];
            redo.RemoveAt(last);
            command.Redo();
            undo.Add(command);
            return true;
        }

        public void Clear()
        {
            undo.Clear();
            redo.Clear();
        }
    }

    public sealed class PlaceItemCommand : IMapEditCommand
    {
        readonly IPlacedItemWorld world;
        readonly MapItemRecord record;

        public PlaceItemCommand(IPlacedItemWorld world, MapItemRecord record)
        {
            this.world = world;
            this.record = record;
        }

        public void Undo()
        {
            world.Destroy(record.InstanceId);
        }

        public void Redo()
        {
            world.Spawn(record);
        }
    }

    public sealed class DeleteItemCommand : IMapEditCommand
    {
        readonly IPlacedItemWorld world;
        readonly MapItemRecord record;

        public DeleteItemCommand(IPlacedItemWorld world, MapItemRecord record)
        {
            this.world = world;
            this.record = record;
        }

        public void Undo()
        {
            world.Spawn(record);
        }

        public void Redo()
        {
            world.Destroy(record.InstanceId);
        }
    }

    public sealed class TransformItemCommand : IMapEditCommand
    {
        readonly IPlacedItemWorld world;
        readonly string instanceId;
        readonly UnityEngine.Vector3 beforePos;
        readonly UnityEngine.Quaternion beforeRot;
        readonly UnityEngine.Vector3 beforeScale;
        readonly UnityEngine.Vector3 afterPos;
        readonly UnityEngine.Quaternion afterRot;
        readonly UnityEngine.Vector3 afterScale;

        public TransformItemCommand(
            IPlacedItemWorld world,
            string instanceId,
            UnityEngine.Vector3 beforePos,
            UnityEngine.Quaternion beforeRot,
            UnityEngine.Vector3 beforeScale,
            UnityEngine.Vector3 afterPos,
            UnityEngine.Quaternion afterRot,
            UnityEngine.Vector3 afterScale)
        {
            this.world = world;
            this.instanceId = instanceId;
            this.beforePos = beforePos;
            this.beforeRot = beforeRot;
            this.beforeScale = beforeScale;
            this.afterPos = afterPos;
            this.afterRot = afterRot;
            this.afterScale = afterScale;
        }

        public void Undo()
        {
            world.SetPose(instanceId, beforePos, beforeRot, beforeScale);
        }

        public void Redo()
        {
            world.SetPose(instanceId, afterPos, afterRot, afterScale);
        }
    }
}
