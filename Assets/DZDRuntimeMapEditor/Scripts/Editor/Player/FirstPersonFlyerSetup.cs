using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace DZDMapEditor
{
    public static class FirstPersonFlyerSetup
    {
        static string FlyerPath => MapEditorPaths.FlyerPrefab;
        const string UrpLitGuid = "31321ba15b8f8eb4c954353edc038b1d";
        const string UndoName = "Add FirstPersonFlyer";

        [MenuItem(MapEditorInfo.ToolsMenu + "/Add FirstPersonFlyer", false, 12)]
        [MenuItem(MapEditorInfo.GameObjectMenu + "/FirstPersonFlyer", false, 12)]
        public static void AddFromMenu()
        {
            var scene = SceneManager.GetActiveScene();
            if (!MapSceneAnchorMenu.CanEditScene(scene, "FirstPersonFlyer"))
                return;

            var flyer = ApplyToScene(scene, out var created, out var error);
            if (flyer == null)
            {
                EditorUtility.DisplayDialog("FirstPersonFlyer", error, "确定");
                return;
            }

            Selection.activeGameObject = flyer;
            EditorGUIUtility.PingObject(flyer);
            EditorUtility.DisplayDialog(
                "FirstPersonFlyer",
                created
                    ? "已添加到当前场景并完成接线。按 Play 即可飞行和摆放。"
                    : "场景里已有 FirstPersonFlyer，已重新接线。按 Play 即可使用。",
                "确定");
        }

        public static GameObject ApplyToScene(Scene scene, out bool created, out string error)
        {
            return ApplyToScene(scene, out created, out error, true);
        }

        public static GameObject ApplyToScene(Scene scene, out bool created, out string error, bool yieldOtherCameras)
        {
            created = false;
            error = null;
            if (Application.isPlaying)
            {
                error = "请先退出 Play。";
                return null;
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                error = "请先打开一个场景。";
                return null;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FlyerPath);
            if (prefab == null)
            {
                error = "找不到预制体：\n" + FlyerPath;
                return null;
            }

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            var flyer = FindFlyer(scene);
            if (flyer == null)
            {
                flyer = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                if (flyer == null)
                {
                    error = "无法实例化 FirstPersonFlyer。";
                    return null;
                }

                Undo.RegisterCreatedObjectUndo(flyer, UndoName);
                flyer.name = "FirstPersonFlyer";
                created = true;
            }

            var previous = SceneManager.GetActiveScene();
            if (previous != scene)
                SceneManager.SetActiveScene(scene);

            var anchor = MapSceneAnchorMenu.EnsureInActiveScene();
            Wire(flyer, anchor, scene, created);
            if (yieldOtherCameras)
                YieldGameplayCamera(flyer);
            YieldEventSystem(flyer);
            EnsureDirectionalLight(scene);

            if (previous.IsValid() && previous.isLoaded && previous != scene)
                SceneManager.SetActiveScene(previous);

            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(group);
            return flyer;
        }

        [MenuItem(MapEditorInfo.ToolsMenu + "/Add FirstPersonFlyer", true)]
        [MenuItem(MapEditorInfo.GameObjectMenu + "/FirstPersonFlyer", true)]
        public static bool ValidateAddFromMenu()
        {
            return !Application.isPlaying;
        }

        static GameObject FindFlyer(Scene scene)
        {
            var flyers = Object.FindObjectsByType<FirstPersonFlyController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < flyers.Length; i++)
            {
                if (flyers[i] != null && flyers[i].gameObject.scene == scene)
                    return flyers[i].gameObject;
            }

            return null;
        }

        static void Wire(GameObject flyer, MapSceneAnchor anchor, Scene scene, bool created)
        {
            var itemsRoot = ResolveItemsRoot(anchor, scene);
            var ground = ResolveSeedGround(scene);
            var placement = flyer.GetComponent<PlacementController>();
            var session = flyer.GetComponent<MapEditSession>();

            if (anchor != null)
            {
                Undo.RecordObject(anchor, UndoName);
                anchor.BindContentRoot(itemsRoot);
                PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);
            }

            Assign(placement, "placedItemsRoot", itemsRoot);
            Assign(session, "placedItemsRoot", itemsRoot);

            if (created)
                PlaceFlyer(flyer, ground);
        }

        static void Assign(Object target, string property, Object value)
        {
            if (target == null)
                return;

            var so = new SerializedObject(target);
            so.Update();
            var prop = so.FindProperty(property);
            if (prop == null)
                return;

            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        static Transform ResolveItemsRoot(MapSceneAnchor anchor, Scene scene)
        {
            if (anchor != null && anchor.PlacedItemsRoot != null)
                return anchor.PlacedItemsRoot;
            var existing = FindNamed(scene, "PlacedItems");
            if (existing != null)
                return existing.transform;
            return CreateChild(anchor != null ? anchor.transform : null, "PlacedItems");
        }

        static GameObject ResolveSeedGround(Scene scene)
        {
            var named = FindNamed(scene, "Ground");
            if (named != null)
                return named;

            var terrains = Object.FindObjectsByType<Terrain>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < terrains.Length; i++)
            {
                if (terrains[i] != null && terrains[i].gameObject.scene == scene)
                    return terrains[i].gameObject;
            }

            return CreateGround(scene);
        }

        static GameObject CreateGround(Scene scene)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Undo.RegisterCreatedObjectUndo(ground, UndoName);
            ground.name = "Ground";
            ground.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            SceneManager.MoveGameObjectToScene(ground, scene);

            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                var litPath = AssetDatabase.GUIDToAssetPath(UrpLitGuid);
                var lit = string.IsNullOrEmpty(litPath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath<Material>(litPath);
                if (lit != null)
                    renderer.sharedMaterial = lit;
            }

            return ground;
        }

        static void EnsureDirectionalLight(Scene scene)
        {
            var lights = Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null || lights[i].gameObject.scene != scene)
                    continue;
                if (lights[i].type == LightType.Directional && lights[i].enabled)
                    return;
            }

            var go = new GameObject("Directional Light");
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(50f, -30f, 0f));
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Color.white;
            light.intensity = 1f;
            light.shadows = LightShadows.Soft;
        }

        static Transform CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go.transform;
        }

        static GameObject FindNamed(Scene scene, string name)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = FindChildNamed(roots[i].transform, name);
                if (found != null)
                    return found.gameObject;
            }

            return null;
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

        static void PlaceFlyer(GameObject flyer, GameObject ground)
        {
            var position = new Vector3(0f, 6f, -8f);
            if (ground != null)
            {
                var col = ground.GetComponentInChildren<Collider>();
                if (col != null)
                    position = col.bounds.center + Vector3.up * (col.bounds.extents.y + 4f);
            }
            else
            {
                var view = SceneView.lastActiveSceneView;
                if (view != null)
                    position = view.pivot + Vector3.up * 2f;
            }

            Undo.RecordObject(flyer.transform, UndoName);
            flyer.transform.position = position;
            flyer.transform.rotation = Quaternion.identity;
        }

        static void YieldGameplayCamera(GameObject flyer)
        {
            var keepCamera = flyer.GetComponentInChildren<Camera>(true);
            var keepListener = flyer.GetComponentInChildren<AudioListener>(true);

            var cameras = Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera == null || camera == keepCamera || !camera.enabled)
                    continue;
                if (camera.GetComponentInParent<FirstPersonFlyController>() != null)
                    continue;

                Undo.RecordObject(camera, UndoName);
                camera.enabled = false;
                var listener = camera.GetComponent<AudioListener>();
                if (listener != null && listener.enabled)
                {
                    Undo.RecordObject(listener, UndoName);
                    listener.enabled = false;
                }
            }

            if (keepListener == null)
                return;

            var listeners = Object.FindObjectsByType<AudioListener>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < listeners.Length; i++)
            {
                var listener = listeners[i];
                if (listener == null || listener == keepListener || !listener.enabled)
                    continue;
                Undo.RecordObject(listener, UndoName);
                listener.enabled = false;
            }
        }

        static void YieldEventSystem(GameObject flyer)
        {
            var keep = flyer.GetComponentInChildren<EventSystem>(true);
            if (keep == null)
                return;

            var systems = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (var i = 0; i < systems.Length; i++)
            {
                var system = systems[i];
                if (system == null || system == keep || !system.enabled)
                    continue;
                Undo.RecordObject(system, UndoName);
                system.enabled = false;
            }
        }
    }
}
