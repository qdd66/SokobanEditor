using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace DZDMapEditor
{
    [CustomEditor(typeof(PlaceableCatalog))]
    public sealed class PlaceableCatalogEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            if (GUILayout.Button("烘焙图标"))
                PlaceableIconBaker.BakeCatalog((PlaceableCatalog)target, false);
            if (GUILayout.Button("烘焙图标（强制覆盖）"))
                PlaceableIconBaker.BakeCatalog((PlaceableCatalog)target, true);
            if (GUILayout.Button("从选中预制体导入"))
                PlaceablePrefabImporter.ConvertFromMenu();
        }
    }

    [CustomEditor(typeof(PlaceableItemDef))]
    public sealed class PlaceableItemDefEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            if (GUILayout.Button("烘焙图标"))
                PlaceableIconBaker.BakeDef((PlaceableItemDef)target, false);
            if (GUILayout.Button("烘焙图标（强制覆盖）"))
                PlaceableIconBaker.BakeDef((PlaceableItemDef)target, true);
        }
    }
}
