using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DZDMapEditor
{
    public static class PlaceablePrefabImporter
    {
        const string DefaultCatalogPath = MapEditorPaths.PlaceableCatalog;
        const string ItemsPrefabRoot = MapEditorPaths.UserItemsPrefabRoot;
        const string ItemsDefRoot = MapEditorPaths.UserItemsDefRoot;
        public const string CategoriesRoot = MapEditorPaths.UserCategoriesRoot;
        public const string UncategorizedAssetPath = CategoriesRoot + "/Uncategorized.asset";
        const uint DefaultRenderingLayer = 1u;

        [MenuItem(MapEditorInfo.AssetsMenu + "/Convert to Placeable", false, 80)]
        [MenuItem(MapEditorInfo.ToolsMenu + "/Convert to Placeable", false, 80)]
        public static void ConvertFromMenu()
        {
            Convert(CollectSelectedPrefabs(), true);
        }

        [MenuItem(MapEditorInfo.AssetsMenu + "/Convert to Placeable", true)]
        [MenuItem(MapEditorInfo.ToolsMenu + "/Convert to Placeable", true)]
        public static bool ValidateConvertFromMenu()
        {
            return HasConvertibleSelection();
        }

        public static void Convert(IReadOnlyList<GameObject> prefabs, bool showSummary)
        {
            if (prefabs == null || prefabs.Count == 0)
            {
                if (showSummary)
                    EditorUtility.DisplayDialog("转为可放置物品", "请先在 Project 里选中预制体，或选中包含预制体的文件夹。", "确定");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(DefaultCatalogPath);
            var created = 0;
            var updated = 0;
            var skipped = 0;
            var failed = 0;
            var defs = new List<PlaceableItemDef>();

            try
            {
                for (var i = 0; i < prefabs.Count; i++)
                {
                    var prefab = prefabs[i];
                    EditorUtility.DisplayProgressBar(
                        "转为可放置物品",
                        prefab != null ? prefab.name : "预制体",
                        prefabs.Count == 1 ? 0.5f : (float)i / prefabs.Count);

                    var result = ConvertOne(prefab, catalog);
                    if (result.Def != null)
                        defs.Add(result.Def);

                    if (result.Status == ConvertStatus.Created)
                        created++;
                    else if (result.Status == ConvertStatus.Updated)
                        updated++;
                    else if (result.Status == ConvertStatus.Skipped)
                        skipped++;
                    else
                        failed++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (defs.Count > 0)
                PlaceableIconBaker.BakeDefs(defs, false, false);

            if (showSummary)
            {
                var catalogHint = catalog != null
                    ? DefaultCatalogPath
                    : "未找到 PlaceableCatalog，定义已生成但没有写入目录。";
                EditorUtility.DisplayDialog(
                    "转为可放置物品",
                    $"新建 {created}，更新 {updated}，跳过 {skipped}，失败 {failed}。\n目录：{catalogHint}",
                    "确定");
            }
        }

        static ConvertResult ConvertOne(GameObject prefab, PlaceableCatalog catalog)
        {
            var result = new ConvertResult();
            if (prefab == null)
            {
                result.Status = ConvertStatus.Failed;
                return result;
            }

            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab"))
            {
                Debug.LogWarning($"PlaceablePrefabImporter: 不是预制体资产，已跳过 {prefab.name}。", prefab);
                result.Status = ConvertStatus.Skipped;
                return result;
            }

            if (PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Model)
            {
                Debug.LogWarning($"PlaceablePrefabImporter: 模型预制体请先做成 .prefab 再导入：{prefabPath}", prefab);
                result.Status = ConvertStatus.Skipped;
                return result;
            }

            if (!HasRenderableMesh(prefab))
            {
                Debug.LogWarning($"PlaceablePrefabImporter: 没有网格，已跳过 {prefabPath}", prefab);
                result.Status = ConvertStatus.Skipped;
                return result;
            }

            var defPath = ResolveDefPath(prefabPath);
            var existed = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(defPath);
            var def = existed != null ? existed : FindDefByPrefab(prefab);
            var created = def == null;
            if (created)
            {
                EnsureFolder(Path.GetDirectoryName(defPath).Replace('\\', '/'));
                def = ScriptableObject.CreateInstance<PlaceableItemDef>();
                AssetDatabase.CreateAsset(def, defPath);
            }

            var defSo = new SerializedObject(def);
            if (created)
                defSo.FindProperty("displayName").stringValue = MakeDisplayName(prefab.name);
            var idProp = defSo.FindProperty("id");
            if (idProp != null && string.IsNullOrEmpty(idProp.stringValue))
                idProp.stringValue = Path.GetFileNameWithoutExtension(defPath);
            defSo.FindProperty("prefab").objectReferenceValue = prefab;
            defSo.FindProperty("category").objectReferenceValue = ResolveCategory(prefabPath);
            defSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(def);

            if (!PreparePrefab(prefabPath, def))
            {
                if (created)
                    AssetDatabase.DeleteAsset(defPath);
                result.Status = ConvertStatus.Failed;
                return result;
            }

            AddToCatalog(catalog, def);
            result.Def = def;
            result.Status = created ? ConvertStatus.Created : ConvertStatus.Updated;
            return result;
        }

        static bool PreparePrefab(string prefabPath, PlaceableItemDef def)
        {
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                ClearStaticFlags(contents);
                ResetRenderingLayers(contents);
                EnsureColliders(contents);

                var placed = contents.GetComponent<PlacedItem>();
                if (placed == null)
                    placed = contents.AddComponent<PlacedItem>();

                var placedSo = new SerializedObject(placed);
                placedSo.FindProperty("definition").objectReferenceValue = def;
                placedSo.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"PlaceablePrefabImporter: 处理预制体失败 {prefabPath}\n{exception}", def);
                return false;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static void EnsureColliders(GameObject root)
        {
            if (root.GetComponentsInChildren<Collider>(true).Length > 0)
                return;

            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var added = 0;
            for (var i = 0; i < filters.Length; i++)
            {
                if (filters[i] == null || filters[i].sharedMesh == null)
                    continue;
                var collider = filters[i].gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filters[i].sharedMesh;
                added++;
            }

            if (added > 0)
                return;

            if (!TryGetLocalBounds(root, out var bounds))
                return;

            var box = root.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = bounds.size;
        }

        static void ResetRenderingLayers(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                if (!(renderers[i] is MeshRenderer) && !(renderers[i] is SkinnedMeshRenderer))
                    continue;
                renderers[i].renderingLayerMask = DefaultRenderingLayer;
            }
        }

        static void ClearStaticFlags(GameObject root)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
                GameObjectUtility.SetStaticEditorFlags(transforms[i].gameObject, 0);
        }

        static void AddToCatalog(PlaceableCatalog catalog, PlaceableItemDef def)
        {
            if (catalog == null || def == null)
                return;

            var so = new SerializedObject(catalog);
            var items = so.FindProperty("items");
            for (var i = 0; i < items.arraySize; i++)
            {
                if (items.GetArrayElementAtIndex(i).objectReferenceValue == def)
                    return;
            }

            items.arraySize++;
            items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = def;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        static List<GameObject> CollectSelectedPrefabs()
        {
            var collected = new List<GameObject>();
            var seen = new HashSet<string>();
            var objects = Selection.objects;
            for (var i = 0; i < objects.Length; i++)
                CollectFromObject(objects[i], collected, seen);
            collected.Sort((a, b) =>
                string.Compare(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b), StringComparison.Ordinal));
            return collected;
        }

        static void CollectFromObject(Object obj, List<GameObject> collected, HashSet<string> seen)
        {
            if (obj == null)
                return;

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                return;

            if (AssetDatabase.IsValidFolder(path))
            {
                var guids = AssetDatabase.FindAssets("t:Prefab", new[] { path });
                for (var i = 0; i < guids.Length; i++)
                {
                    var prefabPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    TryAddPrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), prefabPath, collected, seen);
                }

                return;
            }

            TryAddPrefab(obj as GameObject, path, collected, seen);
        }

        static bool HasConvertibleSelection()
        {
            var objects = Selection.objects;
            for (var i = 0; i < objects.Length; i++)
            {
                var path = AssetDatabase.GetAssetPath(objects[i]);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (AssetDatabase.IsValidFolder(path))
                    return true;
                if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
                    objects[i] is GameObject)
                    return true;
            }

            return false;
        }

        static void TryAddPrefab(GameObject prefab, string path, List<GameObject> collected, HashSet<string> seen)
        {
            if (prefab == null || string.IsNullOrEmpty(path) || !path.EndsWith(".prefab"))
                return;
            if (path.Contains("/Prefabs/Player/") || path.Contains("/Prefabs/Placement/"))
                return;
            if (!seen.Add(path))
                return;
            collected.Add(prefab);
        }

        public static int AssignCategoriesFromFolders()
        {
            var guids = AssetDatabase.FindAssets("t:PlaceableItemDef");
            var assigned = 0;
            for (var i = 0; i < guids.Length; i++)
            {
                var def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (def == null)
                    continue;

                var prefabPath = def.Prefab != null ? AssetDatabase.GetAssetPath(def.Prefab) : string.Empty;
                var so = new SerializedObject(def);
                so.FindProperty("category").objectReferenceValue = ResolveCategory(prefabPath);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(def);
                assigned++;
            }

            AssetDatabase.SaveAssets();
            return assigned;
        }

        public static PlaceableCategoryDef ResolveCategory(string prefabPath)
        {
            var folder = ReadItemsSubfolder(prefabPath);
            if (!string.IsNullOrEmpty(folder))
            {
                var guids = AssetDatabase.FindAssets("t:PlaceableCategoryDef");
                for (var i = 0; i < guids.Length; i++)
                {
                    var category = AssetDatabase.LoadAssetAtPath<PlaceableCategoryDef>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (category != null && category.PrefabFolderName == folder)
                        return category;
                }
            }

            return AssetDatabase.LoadAssetAtPath<PlaceableCategoryDef>(UncategorizedAssetPath);
        }

        static string ReadItemsSubfolder(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath))
                return string.Empty;

            var normalized = prefabPath.Replace('\\', '/');
            var prefix = ItemsPrefabRoot + "/";
            if (!normalized.StartsWith(prefix, StringComparison.Ordinal))
                return string.Empty;

            var relative = normalized.Substring(prefix.Length);
            var slash = relative.IndexOf('/');
            if (slash <= 0)
                return string.Empty;
            return relative.Substring(0, slash);
        }

        static PlaceableItemDef FindDefByPrefab(GameObject prefab)
        {
            var guids = AssetDatabase.FindAssets("t:PlaceableItemDef");
            for (var i = 0; i < guids.Length; i++)
            {
                var def = AssetDatabase.LoadAssetAtPath<PlaceableItemDef>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (def != null && def.Prefab == prefab)
                    return def;
            }

            return null;
        }

        static string ResolveDefPath(string prefabPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(prefabPath) + "Def.asset";
            if (prefabPath.StartsWith(ItemsPrefabRoot + "/") || prefabPath.StartsWith(ItemsPrefabRoot + "\\"))
            {
                var relative = prefabPath.Substring(ItemsPrefabRoot.Length)
                    .Replace('\\', '/');
                var relativeDir = Path.GetDirectoryName(relative);
                if (string.IsNullOrEmpty(relativeDir))
                    return ItemsDefRoot + "/" + fileName;
                relativeDir = relativeDir.Replace('\\', '/');
                if (relativeDir == "/")
                    return ItemsDefRoot + "/" + fileName;
                return ItemsDefRoot + relativeDir + "/" + fileName;
            }

            return ItemsDefRoot + "/" + fileName;
        }

        static string MakeDisplayName(string prefabName)
        {
            if (string.IsNullOrWhiteSpace(prefabName))
                return "Item";
            return ObjectNames.NicifyVariableName(prefabName.Replace('_', ' ')).Trim();
        }

        static bool HasRenderableMesh(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is MeshRenderer || renderers[i] is SkinnedMeshRenderer)
                    return true;
            }

            return false;
        }

        static bool TryGetLocalBounds(GameObject root, out Bounds bounds)
        {
            bounds = new Bounds();
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || renderer is ParticleSystemRenderer)
                    continue;

                EncapsulateWorldBoundsInLocal(root.transform, renderer.bounds, ref bounds, ref hasBounds);
            }

            return hasBounds && bounds.extents.sqrMagnitude > 0.0000001f;
        }

        static void EncapsulateWorldBoundsInLocal(
            Transform root,
            Bounds world,
            ref Bounds local,
            ref bool hasBounds)
        {
            var min = world.min;
            var max = world.max;
            for (var x = 0; x < 2; x++)
            for (var y = 0; y < 2; y++)
            for (var z = 0; z < 2; z++)
            {
                var corner = new Vector3(
                    x == 0 ? min.x : max.x,
                    y == 0 ? min.y : max.y,
                    z == 0 ? min.z : max.z);
                var point = root.InverseTransformPoint(corner);
                if (!hasBounds)
                {
                    local = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    local.Encapsulate(point);
                }
            }
        }

        static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
                return;

            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        struct ConvertResult
        {
            public ConvertStatus Status;
            public PlaceableItemDef Def;
        }

        enum ConvertStatus
        {
            Created,
            Updated,
            Skipped,
            Failed
        }
    }
}
