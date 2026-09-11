using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    [DisallowMultipleComponent]
    [InfoBox("把本地地图存档应用到当前场景：只改物品根。灯光、地面、相机保持不动。")]
    public sealed class MapSceneAnchor : MonoBehaviour
    {
        [Title("内容")]
        [LabelText("物品根")]
        [Tooltip("存档里的物体都会挂在这下面。不要把编辑器 Rig 放进来。")]
        [SerializeField, Required("请指定物品根"), SceneObjectsOnly]
        Transform placedItemsRoot;

        [Title("数据")]
        [LabelText("物品目录")]
        [Tooltip("用存档编号找回预制体。")]
        [SerializeField, Required("请指定物品目录"), AssetsOnly]
        PlaceableCatalog catalog;

        [LabelText("存档配置")]
        [Tooltip("用来定位工程 Maps 目录。可空则从工程根打开文件框。")]
        [SerializeField, AssetsOnly]
        MapPersistenceConfig persistenceConfig;

        public Transform PlacedItemsRoot => placedItemsRoot;
        public PlaceableCatalog Catalog => catalog;
        public MapPersistenceConfig PersistenceConfig => persistenceConfig;

        public void BindRuntimeRefs(
            Transform itemsRoot,
            PlaceableCatalog items,
            MapPersistenceConfig persistence)
        {
            placedItemsRoot = itemsRoot;
            catalog = items;
            persistenceConfig = persistence;
        }

        public void BindContentRoot(Transform itemsRoot)
        {
            if (itemsRoot != null)
                placedItemsRoot = itemsRoot;
        }
    }
}
