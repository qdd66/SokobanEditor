using System.Collections.Generic;
using DZDMapEditor;
using Sokoban;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Sokoban.PlayEdit
{
    public static class SokobanPlayEditSetup
    {
        const string ScenePath = "Assets/Sokoban/Scenes/SokobanSample.unity";
        const string ConfigPath = "Assets/Sokoban/Data/SokobanConfig.asset";
        const string DataRoot = "Assets/Sokoban/Data";
        const string PlacementRoot = DataRoot + "/Placement";
        const string ItemsRoot = PlacementRoot + "/Items";
        const string CategoryPath = PlacementRoot + "/SokobanPieceCategory.asset";
        const string CatalogPath = PlacementRoot + "/SokobanPlaceableCatalog.asset";
        const string PlayerDefPath = ItemsRoot + "/SokobanPlayerDef.asset";
        const string WallDefPath = ItemsRoot + "/SokobanWallDef.asset";
        const string Wall2DefPath = ItemsRoot + "/SokobanWall2Def.asset";
        const string Wall3DefPath = ItemsRoot + "/SokobanWall3Def.asset";
        const string BoxDefPath = ItemsRoot + "/SokobanBoxDef.asset";
        const string GoalDefPath = ItemsRoot + "/SokobanGoalDef.asset";
        const string FloorDefPath = ItemsRoot + "/SokobanFloorDef.asset";
        const string PlayerPrefabPath = "Assets/Sokoban/Prefabs/SokobanPlayer.prefab";
        const string WallPrefabPath = "Assets/Sokoban/Prefabs/SokobanWall.prefab";
        const string Wall2PrefabPath = "Assets/Sokoban/Prefabs/SokobanWall2.prefab";
        const string Wall3PrefabPath = "Assets/Sokoban/Prefabs/SokobanWall3.prefab";
        const string BoxPrefabPath = "Assets/Sokoban/Prefabs/SokobanBox.prefab";
        const string GoalPrefabPath = "Assets/Sokoban/Prefabs/SokobanGoal.prefab";
        const string FloorPrefabPath = "Assets/Sokoban/Prefabs/SokobanFloor.prefab";
        const string UndoName = "接入推箱子地图编辑器";

        [MenuItem("Tools/推箱子/接入地图编辑器", false, 20)]
        public static void SetupFromMenu()
        {
            EditorUtility.DisplayDialog("推箱子", Setup(), "确定");
        }

        [MenuItem("Tools/推箱子/接入地图编辑器", true)]
        public static bool ValidateSetupFromMenu()
        {
            return !Application.isPlaying;
        }

        public static string Setup()
        {
            var assets = SetupAssets();
            if (assets.StartsWith("找不到") || assets.StartsWith("请先"))
                return assets;
            var scene = SetupScene();
            return scene;
        }

        public static string SetupAssets()
        {
            if (Application.isPlaying)
                return "请先退出 Play。";

            EnsureFolder(PlacementRoot);
            EnsureFolder(ItemsRoot);

            var config = AssetDatabase.LoadAssetAtPath<SokobanConfig>(ConfigPath);
            if (config == null)
                return "找不到玩法配置：\n" + ConfigPath;

            PatchSokobanConfig(config);
            var category = EnsureCategory();
            var playerDef = EnsureItemDef(
                PlayerDefPath, "sokoban-player", "玩家", PlayerPrefabPath, category,
                PlaceableOccupancyLayer.Solid, 1);
            var wallDef = EnsureItemDef(
                WallDefPath, "sokoban-wall", "墙1", WallPrefabPath, category,
                PlaceableOccupancyLayer.Solid, 0);
            var boxDef = EnsureItemDef(
                BoxDefPath, "sokoban-box", "箱子", BoxPrefabPath, category,
                PlaceableOccupancyLayer.Solid, 0);
            var goalDef = EnsureItemDef(
                GoalDefPath, "sokoban-goal", "目标点", GoalPrefabPath, category,
                PlaceableOccupancyLayer.Overlay, 0);
            var catalogItems = new List<PlaceableItemDef> { playerDef, wallDef };
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Wall2PrefabPath) != null)
            {
                catalogItems.Add(EnsureItemDef(
                    Wall2DefPath, "sokoban-wall-2", "墙2", Wall2PrefabPath, category,
                    PlaceableOccupancyLayer.Solid, 0));
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(Wall3PrefabPath) != null)
            {
                catalogItems.Add(EnsureItemDef(
                    Wall3DefPath, "sokoban-wall-3", "墙3", Wall3PrefabPath, category,
                    PlaceableOccupancyLayer.Solid, 0));
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(FloorPrefabPath) != null)
            {
                catalogItems.Add(EnsureItemDef(
                    FloorDefPath, "sokoban-floor", "地面", FloorPrefabPath, category,
                    PlaceableOccupancyLayer.Floor, 0));
            }

            catalogItems.Add(boxDef);
            catalogItems.Add(goalDef);
            EnsureCatalog(catalogItems.ToArray());
            BindPrefab(PlayerPrefabPath, playerDef);
            BindPrefab(WallPrefabPath, wallDef);
            BindPrefab(BoxPrefabPath, boxDef);
            BindPrefab(GoalPrefabPath, goalDef);

            var settings = AssetDatabase.LoadAssetAtPath<SettingsPanelConfig>(MapEditorPaths.SettingsPanelConfig);
            if (settings != null)
            {
                EnsureExtraSettingSource(settings, config);
                EnsureSokobanSettingsTab(settings);
            }

            AssetDatabase.SaveAssets();
            return "资源已就绪。";
        }

        public static string BindPlayCamera()
        {
            if (Application.isPlaying)
                return "请先退出 Play。";

            var config = AssetDatabase.LoadAssetAtPath<SokobanConfig>(ConfigPath);
            if (config == null)
                return "找不到玩法配置。";

            PatchSokobanConfig(config);
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var playCamera = Object.FindFirstObjectByType<SokobanSceneCamera>();
            if (playCamera == null)
                return "场景里找不到俯视相机。";

            var so = new SerializedObject(playCamera);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("padding").floatValue = 1.6f;
            so.FindProperty("fieldOfView").floatValue = 40f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(playCamera);
            playCamera.FrameBoard();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            return "俯视相机已接上滚轮调高度。";
        }

        public static string SetupScene()
        {
            if (Application.isPlaying)
                return "请先退出 Play。";

            var config = AssetDatabase.LoadAssetAtPath<SokobanConfig>(ConfigPath);
            var catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(CatalogPath);
            var playerDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(PlayerDefPath);
            var wallDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(WallDefPath);
            var boxDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(BoxDefPath);
            var goalDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(GoalDefPath);
            var floorDef = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(FloorDefPath);
            if (config == null || catalog == null)
                return "请先生成关卡棋子资源。";

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var board = FindNamed(scene, "Board");
            if (board == null)
                return "场景里找不到 Board。";

            var persistence = AssetDatabase.LoadAssetAtPath<MapPersistenceConfig>(MapEditorPaths.PersistenceConfig);
            var anchor = EnsureAnchor(scene, board.transform, catalog, persistence);
            var flyer = FirstPersonFlyerSetup.ApplyToScene(scene, out _, out var error, false);
            if (flyer == null)
                return string.IsNullOrEmpty(error) ? "无法放入 FirstPersonFlyer。" : error;

            Assign(anchor, "placedItemsRoot", board.transform);
            Assign(anchor, "catalog", catalog);
            Assign(flyer.GetComponent<PlacementController>(), "placedItemsRoot", board.transform);
            Assign(flyer.GetComponent<MapEditSession>(), "placedItemsRoot", board.transform);
            Assign(flyer.GetComponent<MapEditSession>(), "catalog", catalog);

            var warehouse = flyer.GetComponentInChildren<WarehouseHotbarView>(true);
            var backpack = flyer.GetComponentInChildren<BackpackPanelView>(true);
            Assign(warehouse, "catalog", catalog);
            Assign(backpack, "catalog", catalog);

            StampBoard(board.transform, playerDef, wallDef, boxDef, goalDef, floorDef);
            EnsurePlayEdit(scene, flyer, config);
            Assign(anchor, "catalog", catalog);
            Assign(flyer.GetComponent<MapEditSession>(), "catalog", catalog);
            Assign(flyer.GetComponentInChildren<WarehouseHotbarView>(true), "catalog", catalog);
            Assign(flyer.GetComponentInChildren<BackpackPanelView>(true), "catalog", catalog);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            return "已接入地图编辑器。Play 后默认推箱子，F1 进入编辑，Esc 设置里有推箱子页签。";
        }

        public static string BindSceneCatalog()
        {
            if (Application.isPlaying)
                return "请先退出 Play。";

            var catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(CatalogPath);
            if (catalog == null)
                return "找不到关卡棋子目录。";

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var board = FindNamed(scene, "Board");
            if (board == null)
                return "场景里找不到 Board。";

            var persistence = AssetDatabase.LoadAssetAtPath<MapPersistenceConfig>(MapEditorPaths.PersistenceConfig);
            var anchor = EnsureAnchor(scene, board.transform, catalog, persistence);
            Assign(anchor, "catalog", catalog);
            Assign(anchor, "placedItemsRoot", board.transform);
            Assign(anchor, "persistenceConfig", persistence);

            var flyers = Object.FindObjectsByType<FirstPersonFlyController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            GameObject flyer = null;
            for (var i = 0; i < flyers.Length; i++)
            {
                if (flyers[i] != null && flyers[i].gameObject.scene == scene)
                {
                    flyer = flyers[i].gameObject;
                    break;
                }
            }

            if (flyer == null)
                return "场景里找不到 FirstPersonFlyer。";

            Assign(flyer.GetComponent<MapEditSession>(), "catalog", catalog);
            Assign(flyer.GetComponent<MapEditSession>(), "placedItemsRoot", board.transform);
            Assign(flyer.GetComponent<PlacementController>(), "placedItemsRoot", board.transform);
            Assign(flyer.GetComponentInChildren<WarehouseHotbarView>(true), "catalog", catalog);
            Assign(flyer.GetComponentInChildren<BackpackPanelView>(true), "catalog", catalog);
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            var so = new SerializedObject(anchor);
            so.Update();
            var bound = so.FindProperty("catalog");
            var boundName = bound != null && bound.objectReferenceValue != null
                ? bound.objectReferenceValue.name
                : "空";
            var itemCount = catalog.Items != null ? catalog.Items.Count : 0;
            return saved
                ? "已绑定目录 " + boundName + "，物品数 " + itemCount
                : "目录已写入内存但场景保存失败，当前引用 " + boundName;
        }

        static void PatchSokobanConfig(SokobanConfig config)
        {
            var so = new SerializedObject(config);
            var hint = so.FindProperty("hintText");
            if (hint != null && (string.IsNullOrEmpty(hint.stringValue) || !hint.stringValue.Contains("滚轮")))
                hint.stringValue = "WASD 移动    滚轮 高度    Z 撤回    R 重置    F1 编辑";
            var toggle = so.FindProperty("toggleEditKey");
            if (toggle != null && toggle.intValue == (int)UnityEngine.InputSystem.Key.None)
                toggle.intValue = (int)UnityEngine.InputSystem.Key.F1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        static void EnsureExtraSettingSource(SettingsPanelConfig settings, ScriptableObject source)
        {
            if (settings == null || source == null)
                return;

            var so = new SerializedObject(settings);
            var extras = so.FindProperty("extraSettingSources");
            if (extras == null)
                return;

            for (var i = 0; i < extras.arraySize; i++)
            {
                if (extras.GetArrayElementAtIndex(i).objectReferenceValue == source)
                    return;
            }

            extras.arraySize++;
            extras.GetArrayElementAtIndex(extras.arraySize - 1).objectReferenceValue = source;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        static void EnsureSokobanSettingsTab(SettingsPanelConfig config)
        {
            var path = MapEditorPaths.PauseMenuPrefab;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || config == null)
                return;

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view = contents.GetComponentInChildren<SettingsPanelView>(true);
                if (view == null)
                    return;

                var so = new SerializedObject(view);
                var tabProp = so.FindProperty("sokobanTab");
                var labelProp = so.FindProperty("sokobanTabLabel");
                if (tabProp != null && tabProp.objectReferenceValue != null)
                    return;

                var placement = so.FindProperty("placementTab");
                var source = placement != null ? placement.objectReferenceValue as Button : null;
                if (source == null)
                    return;

                var clone = Object.Instantiate(source.gameObject, source.transform.parent);
                clone.name = "SokobanTab";
                clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
                var label = clone.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = config.SokobanTab;
                if (tabProp != null)
                    tabProp.objectReferenceValue = clone.GetComponent<Button>();
                if (labelProp != null)
                    labelProp.objectReferenceValue = label;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static PlaceableCategoryDef EnsureCategory()
        {
            var category = AssetDatabase.LoadAssetAtPath<PlaceableCategoryDef>(CategoryPath);
            if (category == null)
            {
                category = ScriptableObject.CreateInstance<PlaceableCategoryDef>();
                AssetDatabase.CreateAsset(category, CategoryPath);
            }

            var so = new SerializedObject(category);
            so.FindProperty("displayName").stringValue = "关卡棋子";
            so.FindProperty("sortOrder").intValue = 0;
            so.FindProperty("prefabFolderName").stringValue = "关卡棋子";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(category);
            return category;
        }

        static PlaceableItemDef EnsureItemDef(
            string path,
            string id,
            string displayName,
            string prefabPath,
            PlaceableCategoryDef category,
            PlaceableOccupancyLayer layer,
            int maxInstances)
        {
            var def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(path);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<PlaceableItemDef>();
                AssetDatabase.CreateAsset(def, path);
            }

            var so = new SerializedObject(def);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("category").objectReferenceValue = category;
            so.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            so.FindProperty("occupancyLayer").intValue = (int)layer;
            so.FindProperty("maxInstances").intValue = maxInstances;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);
            return def;
        }

        static PlaceableCatalog EnsureCatalog(IReadOnlyList<PlaceableItemDef> defs)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PlaceableCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var so = new SerializedObject(catalog);
            var items = so.FindProperty("items");
            items.arraySize = defs.Count;
            for (var i = 0; i < defs.Count; i++)
                items.GetArrayElementAtIndex(i).objectReferenceValue = defs[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static void BindPrefab(string prefabPath, PlaceableItemDef def)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null || def == null)
                return;

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                PlacedItem.Ensure(contents, def);
                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static MapSceneAnchor EnsureAnchor(
            Scene scene,
            Transform board,
            PlaceableCatalog catalog,
            MapPersistenceConfig persistence)
        {
            var found = Object.FindObjectsByType<MapSceneAnchor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            MapSceneAnchor anchor = null;
            for (var i = 0; i < found.Length; i++)
            {
                if (found[i] != null && found[i].gameObject.scene == scene)
                {
                    anchor = found[i];
                    break;
                }
            }

            if (anchor == null)
            {
                var go = new GameObject("MapSceneAnchor");
                Undo.RegisterCreatedObjectUndo(go, UndoName);
                SceneManager.MoveGameObjectToScene(go, scene);
                anchor = go.AddComponent<MapSceneAnchor>();
            }

            anchor.BindRuntimeRefs(board, catalog, persistence);
            EditorUtility.SetDirty(anchor);
            return anchor;
        }

        static void StampBoard(
            Transform board,
            PlaceableItemDef playerDef,
            PlaceableItemDef wallDef,
            PlaceableItemDef boxDef,
            PlaceableItemDef goalDef,
            PlaceableItemDef floorDef)
        {
            var pieces = board.GetComponentsInChildren<SokobanPiece>(true);
            for (var i = 0; i < pieces.Length; i++)
            {
                var piece = pieces[i];
                if (piece == null)
                    continue;
                var def = DefFor(piece.Role, playerDef, wallDef, boxDef, goalDef, floorDef);
                var item = PlacedItem.Ensure(piece.gameObject, def);
                item.EnsureInstanceId();
                EditorUtility.SetDirty(item);
            }
        }

        static PlaceableItemDef DefFor(
            SokobanRole role,
            PlaceableItemDef playerDef,
            PlaceableItemDef wallDef,
            PlaceableItemDef boxDef,
            PlaceableItemDef goalDef,
            PlaceableItemDef floorDef)
        {
            switch (role)
            {
                case SokobanRole.Player:
                    return playerDef;
                case SokobanRole.Box:
                    return boxDef;
                case SokobanRole.Goal:
                    return goalDef;
                case SokobanRole.Floor:
                    return floorDef != null ? floorDef : wallDef;
                default:
                    return wallDef;
            }
        }

        static void EnsurePlayEdit(
            Scene scene,
            GameObject flyer,
            SokobanConfig config)
        {
            var existing = Object.FindObjectsByType<SokobanPlayEditController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            SokobanPlayEditController controller = null;
            for (var i = 0; i < existing.Length; i++)
            {
                if (existing[i] != null && existing[i].gameObject.scene == scene)
                {
                    controller = existing[i];
                    break;
                }
            }

            if (controller == null)
            {
                var go = new GameObject("SokobanPlayEdit");
                Undo.RegisterCreatedObjectUndo(go, UndoName);
                SceneManager.MoveGameObjectToScene(go, scene);
                controller = go.AddComponent<SokobanPlayEditController>();
            }

            var session = Object.FindFirstObjectByType<SokobanSession>();
            var playCamera = Object.FindFirstObjectByType<SokobanSceneCamera>();
            var hud = Object.FindFirstObjectByType<SokobanHud>();
            var playView = playCamera != null ? playCamera.GetComponent<Camera>() : null;
            var playListener = playCamera != null ? playCamera.GetComponent<AudioListener>() : null;
            var editView = flyer.GetComponentInChildren<Camera>(true);
            var editListener = editView != null ? editView.GetComponent<AudioListener>() : flyer.GetComponentInChildren<AudioListener>(true);
            var toast = AssetDatabase.LoadAssetAtPath<ToastChannel>(MapEditorPaths.ToastChannel);

            var so = new SerializedObject(controller);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("session").objectReferenceValue = session;
            so.FindProperty("playCamera").objectReferenceValue = playCamera;
            so.FindProperty("playView").objectReferenceValue = playView;
            so.FindProperty("playListener").objectReferenceValue = playListener;
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("fly").objectReferenceValue = flyer.GetComponent<FirstPersonFlyController>();
            so.FindProperty("editView").objectReferenceValue = editView;
            so.FindProperty("editListener").objectReferenceValue = editListener;
            so.FindProperty("placement").objectReferenceValue = flyer.GetComponent<PlacementController>();
            so.FindProperty("gameplayCursor").objectReferenceValue = flyer.GetComponent<GameplayCursor>();
            so.FindProperty("crosshair").objectReferenceValue = flyer.GetComponent<ScreenCrosshair>();
            so.FindProperty("warehouse").objectReferenceValue = flyer.GetComponentInChildren<WarehouseHotbarView>(true);
            so.FindProperty("backpack").objectReferenceValue = flyer.GetComponentInChildren<BackpackPanelView>(true);
            so.FindProperty("pause").objectReferenceValue = flyer.GetComponent<PauseMenuController>();
            so.FindProperty("persistence").objectReferenceValue = flyer.GetComponent<MapPersistenceController>();
            so.FindProperty("toastChannel").objectReferenceValue = toast;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
            if (playCamera != null)
            {
                var camSo = new SerializedObject(playCamera);
                camSo.FindProperty("config").objectReferenceValue = config;
                camSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(playCamera);
            }

            DisableEditRigInScene(flyer, playCamera);
        }

        static void DisableEditRigInScene(GameObject flyer, SokobanSceneCamera playCamera)
        {
            SetBehaviourEnabled(flyer.GetComponent<FirstPersonFlyController>(), false);
            SetBehaviourEnabled(flyer.GetComponent<PlacementController>(), false);
            SetBehaviourEnabled(flyer.GetComponent<GameplayCursor>(), false);
            SetBehaviourEnabled(flyer.GetComponent<ScreenCrosshair>(), false);
            SetBehaviourEnabled(flyer.GetComponent<MapPersistenceController>(), false);
            var editView = flyer.GetComponentInChildren<Camera>(true);
            SetBehaviourEnabled(editView, false);
            SetBehaviourEnabled(editView != null ? editView.GetComponent<AudioListener>() : null, false);
            SetBehaviourEnabled(playCamera, true);
            SetBehaviourEnabled(playCamera != null ? playCamera.GetComponent<Camera>() : null, true);
            SetBehaviourEnabled(playCamera != null ? playCamera.GetComponent<AudioListener>() : null, true);
            var warehouse = flyer.GetComponentInChildren<WarehouseHotbarView>(true);
            if (warehouse != null)
                warehouse.gameObject.SetActive(false);
        }

        static void SetBehaviourEnabled(Behaviour behaviour, bool enabled)
        {
            if (behaviour == null)
                return;
            Undo.RecordObject(behaviour, UndoName);
            behaviour.enabled = enabled;
            EditorUtility.SetDirty(behaviour);
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

        static void Assign(Object target, string property, Object value)
        {
            if (target == null)
                return;

            var so = new SerializedObject(target);
            so.Update();
            var prop = so.FindProperty(property);
            if (prop == null || prop.propertyType != SerializedPropertyType.ObjectReference)
                return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            var root = target as Component;
            if (root != null)
            {
                var outer = PrefabUtility.GetOutermostPrefabInstanceRoot(root.gameObject);
                if (outer != null)
                    PrefabUtility.RecordPrefabInstancePropertyModifications(outer);
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
