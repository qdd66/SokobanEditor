using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DZDMapEditor
{
    public static class MapSceneAnchorMenu
    {
        const string CatalogPath = MapEditorPaths.PlaceableCatalog;
        const string PersistencePath = MapEditorPaths.PersistenceConfig;

        [MenuItem(MapEditorInfo.GameObjectMenu + "/Map Scene Anchor", false, 10)]
        [MenuItem(MapEditorInfo.ToolsMenu + "/Create Map Scene Anchor", false, 11)]
        public static void CreateFromMenu()
        {
            var scene = SceneManager.GetActiveScene();
            if (!CanEditScene(scene, "Map Scene Anchor"))
                return;

            var existing = FindInScene(scene);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing);
                EditorUtility.DisplayDialog("Map Scene Anchor", "当前场景已经有 Map Scene Anchor。", "确定");
                return;
            }

            var created = CreateInScene(scene);
            Selection.activeGameObject = created.gameObject;
            EditorGUIUtility.PingObject(created);
        }

        public static MapSceneAnchor EnsureInActiveScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            var existing = FindInScene(scene);
            return existing != null ? existing : CreateInScene(scene);
        }

        public static bool CanEditScene(Scene scene, string title)
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog(title, "请先退出 Play。", "确定");
                return false;
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog(title, "请先打开一个场景。", "确定");
                return false;
            }

            return true;
        }

        static MapSceneAnchor FindInScene(Scene scene)
        {
            var selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<MapSceneAnchor>()
                : null;
            if (selected != null && selected.gameObject.scene == scene)
                return selected;

            var found = Object.FindObjectsByType<MapSceneAnchor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            MapSceneAnchor first = null;
            for (var i = 0; i < found.Length; i++)
            {
                if (found[i] == null || found[i].gameObject.scene != scene)
                    continue;
                if (first == null)
                    first = found[i];
            }

            return first;
        }

        static MapSceneAnchor CreateInScene(Scene scene)
        {
            var root = new GameObject("MapSceneAnchor");
            Undo.RegisterCreatedObjectUndo(root, "Create Map Scene Anchor");
            var items = FindOrCreateRoot(scene, root.transform, "PlacedItems");
            var anchor = root.AddComponent<MapSceneAnchor>();
            var catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(CatalogPath);
            var persistence = AssetDatabase.LoadAssetAtPath<MapPersistenceConfig>(PersistencePath);
            anchor.BindRuntimeRefs(items, catalog, persistence);

            EditorSceneManager.MarkSceneDirty(scene);
            return anchor;
        }

        static Transform FindOrCreateRoot(Scene scene, Transform fallbackParent, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var match = FindChildNamed(roots[i].transform, name);
                if (match != null)
                    return match;
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create Map Scene Anchor");
            go.transform.SetParent(fallbackParent, false);
            return go.transform;
        }

        static Transform FindChildNamed(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChildNamed(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
