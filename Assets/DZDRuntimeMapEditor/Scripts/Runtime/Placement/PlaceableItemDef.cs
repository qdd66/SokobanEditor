using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "PlaceableItemDef",
        menuName = MapEditorInfo.CreateMenu + "/Placeable Item")]
    public sealed class PlaceableItemDef : ScriptableObject
    {
        [Title("身份")]
        [LabelText("存档编号")]
        [Tooltip("地图文件用这个编号找回物品。不要随便改，改了旧档会找不到。")]
        [SerializeField]
        string id;

        [Title("展示")]
        [LabelText("显示名称")]
        [Tooltip("仓库格子上显示的名字。")]
        [SerializeField]
        string displayName = "Item";

        [LabelText("分类")]
        [Tooltip("背包面板按这个分类分组。")]
        [SerializeField, Required("请指定分类"), AssetsOnly]
        PlaceableCategoryDef category;

        [LabelText("预制体")]
        [Tooltip("实际生成到场景里的物体。")]
        [SerializeField, Required("请指定预制体"), AssetsOnly]
        GameObject prefab;

        [LabelText("图标")]
        [Tooltip("仓库格子图标。可空，再用菜单烘焙。")]
        [PreviewField(64)]
        [SerializeField, AssetsOnly]
        Sprite icon;

        [LabelText("贴合表面法线")]
        [Tooltip("勾选后放置时物体朝向会随地表法线倾斜。")]
        [SerializeField]
        bool alignToSurfaceNormal;

        [LabelText("额外表面偏移")]
        [Tooltip("在全局表面偏移之上再抬高一点，适合底座陷入地面的模型。")]
        [SuffixLabel("米", Overlay = true)]
        [SerializeField]
        float extraSurfaceOffset;

        [Title("占用")]
        [LabelText("占用层")]
        [Tooltip("实体与实体互斥，标记与标记互斥，地面与地面互斥。不同层可以同格，例如箱子叠在目标点上，地面垫在墙下面。")]
        [SerializeField]
        PlaceableOccupancyLayer occupancyLayer = PlaceableOccupancyLayer.Solid;

        [LabelText("最大数量")]
        [Tooltip("同一物品根下允许的实例数。0 表示不限制。")]
        [SerializeField, MinValue(0)]
        int maxInstances;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => displayName;
        public PlaceableCategoryDef Category => category;
        public GameObject Prefab => prefab;
        public Sprite Icon => icon;
        public bool AlignToSurfaceNormal => alignToSurfaceNormal;
        public float ExtraSurfaceOffset => extraSurfaceOffset;
        public PlaceableOccupancyLayer OccupancyLayer => occupancyLayer;
        public int MaxInstances => Mathf.Max(0, maxInstances);
    }
}
