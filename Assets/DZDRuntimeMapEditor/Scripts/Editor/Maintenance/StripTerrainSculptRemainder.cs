using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DZDMapEditor
{
    public static class StripTerrainSculptRemainder
    {
        const string PrefKey = "DZDMapEditor.StripTerrainSculpt.v1";
        const string DefaultScenePath = "Assets/DZDRuntimeMapEditor/Scenes/DefaultScene.unity";

        static readonly string[] RemovedTypeNames =
        {
            "TerrainSculptController",
            "EditorToolState",
            "EditorToolController",
            "TerrainBrushHotbarView",
            "TerrainChunkPresenter"
        };

        static readonly string[] RemovedObjectNames =
        {
            "SculptedTerrain",
            "TerrainChunks",
            "TerrainBrushHotbar",
            "TerrainTab"
        };

        static readonly string[] DeletedAssetPaths =
        {
            "Assets/DZDRuntimeMapEditor/Data/Terrain",
            "Assets/DZDRuntimeMapEditor/Data/EditorTools",
            "Assets/DZDRuntimeMapEditor/Prefabs/Terrain",
            "Assets/DZDRuntimeMapEditor/Materials/SculptedTerrain.mat",
            "Assets/DZDRuntimeMapEditor/Scripts/Shaders/TerrainShadow.shader"
        };

        [InitializeOnLoadMethod]
        static void AutoRun()
        {
            if (EditorPrefs.GetBool(PrefKey, false))
                return;
            EditorApplication.delayCall += TryRun;
        }

        static void TryRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryRun;
                return;
            }

            Run();
            EditorPrefs.SetBool(PrefKey, true);
        }

        [MenuItem(MapEditorInfo.ToolsMenu + "/Strip Terrain Sculpt Remainder", false, 50)]
        public static void RunFromMenu()
        {
            var message = Run();
            EditorPrefs.SetBool(PrefKey, true);
            EditorUtility.DisplayDialog("Strip Terrain Sculpt", message, "确定");
        }

        public static string Run()
        {
            ApplyPlacementDefaults();
            var flyer = StripPrefabAsset(MapEditorPaths.UserPrefabRoot + "/Player/FirstPersonFlyer.prefab");
            var pause = StripPrefabAsset(MapEditorPaths.UserPrefabRoot + "/UI/PauseMenu.prefab");
            StripSceneAsset(DefaultScenePath);
            DeleteLeftoverAssets();
            AssetDatabase.SaveAssets();
            return "已去掉地形塑造接线，并打开默认网格放置和连续摆放。flyer=" + flyer + " pause=" + pause;
        }

        static void ApplyPlacementDefaults()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlacementConfig>(MapEditorPaths.PlacementConfig);
            if (config == null)
                return;

            var so = new SerializedObject(config);
            var continuous = so.FindProperty("defaultContinuousPlace");
            var grid = so.FindProperty("defaultGridPlace");
            if (continuous != null)
                continuous.boolValue = true;
            if (grid != null)
                grid.boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        static string StripPrefabAsset(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return path + ": missing";

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                StripHierarchy(contents);
                var saved = PrefabUtility.SaveAsPrefabAsset(contents, path);
                return saved != null ? "ok" : "save-failed";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void StripSceneAsset(string path)
        {
            if (!System.IO.File.Exists(path))
                return;

            var wasLoaded = false;
            var scene = default(Scene);
            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var loaded = EditorSceneManager.GetSceneAt(i);
                if (loaded.path != path)
                    continue;
                wasLoaded = true;
                scene = loaded;
                break;
            }

            if (!wasLoaded)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
                StripHierarchy(roots[i]);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!wasLoaded)
                EditorSceneManager.CloseScene(scene, true);
        }

        static void StripHierarchy(GameObject root)
        {
            if (root == null)
                return;

            DestroyNamed(root.transform);
            RemoveTypedComponents(root);
            RemoveMissing(root);
        }

        static bool IsRemovedName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            for (var i = 0; i < RemovedObjectNames.Length; i++)
            {
                if (name == RemovedObjectNames[i] || name.IndexOf(RemovedObjectNames[i]) >= 0)
                    return true;
            }

            return name.IndexOf("Missing Prefab") >= 0 ||
                   name.IndexOf("Placeholder for referenced MonoBehaviour") >= 0;
        }

        static void DestroyNamed(Transform root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = transforms.Length - 1; i >= 0; i--)
            {
                var t = transforms[i];
                if (t == null || t == root)
                    continue;
                if (!IsRemovedName(t.name))
                    continue;

                var go = t.gameObject;
                if (PrefabUtility.IsAnyPrefabInstanceRoot(go) &&
                    PrefabUtility.GetCorrespondingObjectFromSource(go) == null)
                    PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

                Object.DestroyImmediate(go);
            }
        }

        static void RemoveTypedComponents(GameObject root)
        {
            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                    continue;
                if (!IsRemovedType(behaviour.GetType().Name))
                    continue;
                Object.DestroyImmediate(behaviour);
            }
        }

        static bool IsRemovedType(string typeName)
        {
            for (var i = 0; i < RemovedTypeNames.Length; i++)
            {
                if (typeName == RemovedTypeNames[i])
                    return true;
            }

            return false;
        }

        static void RemoveMissing(GameObject root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                var go = transforms[i].gameObject;
                while (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                {
                    var removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    if (removed <= 0)
                        break;
                }
            }
        }

        static void DeleteLeftoverAssets()
        {
            for (var i = 0; i < DeletedAssetPaths.Length; i++)
            {
                var path = DeletedAssetPaths[i];
                if (AssetDatabase.LoadAssetAtPath<Object>(path) != null || AssetDatabase.IsValidFolder(path))
                    AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
