using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    [DefaultExecutionOrder(-90)]
    public sealed class MapEditSession : MonoBehaviour
    {
        [LabelText("物品目录")]
        [Tooltip("加载和撤回时按编号找回预制体。")]
        [SerializeField, Required("请指定物品目录"), AssetsOnly]
        PlaceableCatalog catalog;

        [LabelText("已放物体根节点")]
        [Tooltip("保存和加载都只处理这个节点下的物体。")]
        [SerializeField]
        Transform placedItemsRoot;

        [LabelText("存档配置")]
        [Tooltip("撤回步数等。")]
        [SerializeField, Required("请指定存档配置"), AssetsOnly]
        MapPersistenceConfig config;

        MapHistory history;
        PlacedItemWorld world;

        public IPlacedItemWorld Items => world;
        public PlacedItemWorld ItemWorld => world;
        public bool CanUndo => history != null && history.CanUndo;
        public bool CanRedo => history != null && history.CanRedo;
        public PlaceableCatalog Catalog => catalog;
        public Transform PlacedItemsRoot => placedItemsRoot;

        void Awake()
        {
            var steps = config != null ? config.MaxUndoSteps : 32;
            history = new MapHistory(steps);
            world = new PlacedItemWorld(catalog, placedItemsRoot);
        }

        public void HandlePlacement(in PlacementMutation mutation)
        {
            if (history == null || world == null || !mutation.HasValue)
                return;

            switch (mutation.Type)
            {
                case PlacementMutation.Kind.PlacedNew:
                    if (mutation.Item == null)
                        return;
                    world.Register(mutation.Item);
                    history.Record(new PlaceItemCommand(world, MapItemRecord.FromItem(mutation.Item)));
                    break;
                case PlacementMutation.Kind.Transformed:
                    if (mutation.Item == null)
                        return;
                    if (PoseUnchanged(mutation))
                    {
                        world.Register(mutation.Item);
                        return;
                    }
                    world.Register(mutation.Item);
                    history.Record(new TransformItemCommand(
                        world,
                        mutation.Item.InstanceId,
                        mutation.BeforePosition,
                        mutation.BeforeRotation,
                        mutation.BeforeScale,
                        mutation.AfterPosition,
                        mutation.AfterRotation,
                        mutation.AfterScale));
                    break;
                case PlacementMutation.Kind.Deleted:
                    if (!mutation.DeletedRecord.IsValid)
                        return;
                    world.Unregister(mutation.DeletedRecord.InstanceId);
                    history.Record(new DeleteItemCommand(world, mutation.DeletedRecord));
                    break;
            }
        }

        public bool Undo()
        {
            return history != null && history.Undo();
        }

        public bool Redo()
        {
            return history != null && history.Redo();
        }

        public void ClearHistory()
        {
            history?.Clear();
        }

        static bool PoseUnchanged(in PlacementMutation mutation)
        {
            return (mutation.BeforePosition - mutation.AfterPosition).sqrMagnitude < 0.0001f &&
                   (mutation.BeforeScale - mutation.AfterScale).sqrMagnitude < 0.0001f &&
                   Quaternion.Angle(mutation.BeforeRotation, mutation.AfterRotation) < 0.05f;
        }
    }
}
