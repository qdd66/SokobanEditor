using UnityEditor;
using UnityEngine;

namespace DZDMapEditor
{
    public static class PlacementGridSetup
    {
        const string ShaderName = "Hidden/DZDMapEditor/PlacementGrid";
        const string ShaderPath = MapEditorPaths.SampleRoot + "/Scripts/Shaders/PlacementGrid.shader";
        const string MaterialPath = MapEditorPaths.PlacementGridMaterial;
        static string FlyerPath
        {
            get
            {
                var user = MapEditorPaths.UserPrefabRoot + "/Player/FirstPersonFlyer.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(user) != null)
                    return user;
                return MapEditorPaths.FlyerPrefab;
            }
        }

        [MenuItem(MapEditorInfo.ToolsMenu + "/Setup Placement Grid", false, 42)]
        public static void SetupFromMenu()
        {
            Debug.Log(Setup());
        }

        public static string Setup()
        {
            EnsureFolder(MapEditorPaths.UserMaterialsRoot);
            var material = EnsureMaterial();
            WireFlyer(material);
            AssetDatabase.SaveAssets();
            return "网格放置已接到飞行角色，材质：" + MaterialPath;
        }

        static Material EnsureMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
                return existing;

            var shader = Shader.Find(ShaderName);
            if (shader == null)
                shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
            if (shader == null)
                throw new System.InvalidOperationException("找不到网格着色器：" + ShaderName);

            var material = new Material(shader)
            {
                name = "PlacementGrid",
                color = Color.white
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        static void WireFlyer(Material material)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FlyerPath);
            if (prefab == null)
                throw new System.InvalidOperationException("找不到飞行角色预制体：\n" + FlyerPath);

            var contents = PrefabUtility.LoadPrefabContents(FlyerPath);
            try
            {
                var visual = contents.GetComponent<PlacementGridVisual>();
                if (visual == null)
                    visual = contents.AddComponent<PlacementGridVisual>();

                var visualSo = new SerializedObject(visual);
                visualSo.FindProperty("material").objectReferenceValue = material;
                visualSo.ApplyModifiedPropertiesWithoutUndo();

                var placement = contents.GetComponent<PlacementController>();
                if (placement != null)
                {
                    var placementSo = new SerializedObject(placement);
                    placementSo.FindProperty("gridVisual").objectReferenceValue = visual;
                    placementSo.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(contents, FlyerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
