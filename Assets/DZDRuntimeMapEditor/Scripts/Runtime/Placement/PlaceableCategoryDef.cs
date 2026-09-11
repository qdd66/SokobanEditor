using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "PlaceableCategoryDef",
        menuName = MapEditorInfo.CreateMenu + "/Placeable Category")]
    public sealed class PlaceableCategoryDef : ScriptableObject
    {
        [LabelText("显示名称")]
        [Tooltip("背包分组标题。")]
        [SerializeField]
        string displayName = "分类";

        [LabelText("排序")]
        [Tooltip("背包分组从上到下的顺序，数值越小越靠前。")]
        [SerializeField]
        int sortOrder;

        [LabelText("预制体文件夹")]
        [Tooltip("导入时匹配 Prefabs/Items/ 下这一层文件夹名（当前在 Assets/DZDRuntimeMapEditor/Prefabs/Items）。留空表示未分类。")]
        [SerializeField]
        string prefabFolderName;

        public string DisplayName => displayName;
        public int SortOrder => sortOrder;
        public string PrefabFolderName => prefabFolderName;
    }
}
