using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DZDMapEditor
{
    public static class SceneContentApplier
    {
        const string UndoName = "应用地图存档到场景";
        const string LastDirKey = "DZDMapEditor.LastMapDir";

        [MenuItem(MapEditorInfo.ToolsMenu + "/Apply Map File to Open Scene", false, 10)]
        public static void ApplyFromMenu()
        {
            var anchor = ResolveAnchor(true);
            if (anchor == null)
                return;
            ApplyFromFileDialog(anchor);
        }

        [MenuItem(MapEditorInfo.ToolsMenu + "/Apply Map File to Open Scene", true)]
        public static bool ValidateApplyFromMenu()
        {
            return !Application.isPlaying;
        }

        public static void ApplyFromFileDialog(MapSceneAnchor anchor)
        {
            if (!CanApply(anchor, out var block))
            {
                EditorUtility.DisplayDialog("应用到当前场景", block, "确定");
                return;
            }

            var dir = EditorPrefs.GetString(LastDirKey, ResolveDefaultDirectory(anchor));
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                dir = ResolveDefaultDirectory(anchor);

            var path = EditorUtility.OpenFilePanel(
                "选择地图存档",
                dir,
                MapPersistenceConfig.FileExtension.TrimStart('.'));
            if (string.IsNullOrEmpty(path))
                return;

            EditorPrefs.SetString(LastDirKey, Path.GetDirectoryName(path));
            var message = ApplyFile(anchor, path);
            EditorUtility.DisplayDialog("应用到当前场景", message, "确定");
        }

        public static string ApplyFile(MapSceneAnchor anchor, string path)
        {
            if (!CanApply(anchor, out var block))
                return block;
            if (!MapFileStore.IsMapFile(path))
                return "请选择 " + MapPersistenceConfig.FileExtension + " 存档";
            if (!MapFileStore.TryRead(path, out var json, out var error))
                return string.IsNullOrEmpty(error) ? "无法读取存档" : error;

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);

            EnsureContentRoots(anchor);
            var itemsRoot = anchor.PlacedItemsRoot;
            var catalog = anchor.Catalog;
            var missing = new List<string>();
            var spawned = 0;
            var updated = 0;
            var deleted = 0;

            var records = new List<MapItemRecord>();
            if (json.items != null)
            {
                for (var i = 0; i < json.items.Length; i++)
                    records.Add(MapItemRecord.FromJson(json.items[i]));
            }

            SyncItems(itemsRoot, catalog, records, missing, ref spawned, ref updated, ref deleted);

            EditorSceneManager.MarkSceneDirty(anchor.gameObject.scene);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(group);

            var name = string.IsNullOrEmpty(json.mapName)
                ? Path.GetFileNameWithoutExtension(path)
                : json.mapName;
            var summary = "已将「" + name + "」应用到场景。\n" +
                          "新增 " + spawned + " · 更新 " + updated + " · 删除 " + deleted;
            if (missing.Count > 0)
                summary += "\n有 " + missing.Count + " 个物体在目录里找不到，已跳过。";
            summary += "\n请再保存一次 Unity 场景。";
            return summary;
        }

        static bool CanApply(MapSceneAnchor anchor, out string error)
        {
            error = null;
            if (Application.isPlaying)
            {
                error = "请先退出 Play，再把存档写进场景。";
                return false;
            }

            if (anchor == null)
            {
                error = "当前场景没有 Map Scene Anchor。请先用菜单创建，或给场景加这个组件。";
                return false;
            }

            if (anchor.Catalog == null)
            {
                error = "请在 Anchor 上指定物品目录。";
                return false;
            }

            return true;
        }

        static MapSceneAnchor ResolveAnchor(bool showDialog)
        {
            var selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<MapSceneAnchor>()
                : null;
            if (selected != null)
                return selected;

            var found = UnityEngine.Object.FindObjectsByType<MapSceneAnchor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (found.Length == 1)
                return found[0];
            if (found.Length > 1)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog(
                        "应用到当前场景",
                        "场景里有多个 Map Scene Anchor，请先在 Hierarchy 里选中要用的那一个。",
                        "确定");
                return null;
            }

            if (showDialog)
                EditorUtility.DisplayDialog(
                    "应用到当前场景",
                    "当前打开的场景没有 Map Scene Anchor。\n用 GameObject / DZDRuntimeMapEditor / Map Scene Anchor 创建后再导入。",
                    "确定");
            return null;
        }

        static string ResolveDefaultDirectory(MapSceneAnchor anchor)
        {
            var dir = MapFileStore.ResolveDirectory(anchor != null ? anchor.PersistenceConfig : null);
            return Directory.Exists(dir) ? dir : Application.dataPath;
        }

        static void EnsureContentRoots(MapSceneAnchor anchor)
        {
            Undo.RecordObject(anchor, UndoName);
            var items = anchor.PlacedItemsRoot;
            if (items == null)
            {
                var go = new GameObject("PlacedItems");
                Undo.RegisterCreatedObjectUndo(go, UndoName);
                go.transform.SetParent(anchor.transform, false);
                items = go.transform;
            }

            anchor.BindContentRoot(items);
            PrefabUtility.RecordPrefabInstancePropertyModifications(anchor);
        }

        static void SyncItems(
            Transform root,
            PlaceableCatalog catalog,
            List<MapItemRecord> records,
            List<string> missing,
            ref int spawned,
            ref int updated,
            ref int deleted)
        {
            var existing = CollectTopLevelItems(root);

            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (!record.IsValid)
                    continue;
                if (!catalog.TryGet(record.DefId, out var def) || def.Prefab == null)
                {
                    missing.Add(record.DefId);
                    continue;
                }

                if (existing.TryGetValue(record.InstanceId, out var item) && item != null)
                {
                    Undo.RecordObject(item.transform, UndoName);
                    Undo.RecordObject(item, UndoName);
                    item.Bind(def);
                    item.BindInstanceId(record.InstanceId);
                    item.transform.SetPositionAndRotation(record.Position, record.Rotation);
                    item.transform.localScale = record.Scale;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(item);
                    existing.Remove(record.InstanceId);
                    updated++;
                    continue;
                }

                if (Spawn(root, def, record) != null)
                    spawned++;
                else
                    missing.Add(record.DefId);
            }

            foreach (var pair in existing)
            {
                if (pair.Value == null)
                    continue;
                Undo.DestroyObjectImmediate(pair.Value.gameObject);
                deleted++;
            }
        }

        static Dictionary<string, PlacedItem> CollectTopLevelItems(Transform root)
        {
            var map = new Dictionary<string, PlacedItem>(StringComparer.Ordinal);
            if (root == null)
                return map;

            var items = root.GetComponentsInChildren<PlacedItem>(true);
            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (item == null || !IsTopLevel(item, root))
                    continue;
                item.EnsureInstanceId();
                if (map.ContainsKey(item.InstanceId))
                    continue;
                map.Add(item.InstanceId, item);
            }

            return map;
        }

        static bool IsTopLevel(PlacedItem item, Transform root)
        {
            var parent = item.transform.parent;
            while (parent != null && parent != root)
            {
                if (parent.GetComponent<PlacedItem>() != null)
                    return false;
                parent = parent.parent;
            }

            return parent == root;
        }

        static PlacedItem Spawn(Transform root, PlaceableItemDef def, MapItemRecord record)
        {
            var prefab = def.Prefab;
            if (prefab == null)
                return null;

            GameObject go;
            if (PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.NotAPrefab)
                go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            else
                go = UnityEngine.Object.Instantiate(prefab, root);

            if (go == null)
                return null;

            Undo.RegisterCreatedObjectUndo(go, UndoName);
            go.name = prefab.name;
            go.transform.SetPositionAndRotation(record.Position, record.Rotation);
            go.transform.localScale = record.Scale;

            var item = go.GetComponent<PlacedItem>();
            if (item == null)
                item = Undo.AddComponent<PlacedItem>(go);
            Undo.RecordObject(item, UndoName);
            item.Bind(def);
            item.BindInstanceId(record.InstanceId);
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            return item;
        }
    }
}
