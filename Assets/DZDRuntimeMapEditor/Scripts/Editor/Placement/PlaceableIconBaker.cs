using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DZDMapEditor
{
    public static class PlaceableIconBaker
    {
        const string DefaultCatalogPath = MapEditorPaths.PlaceableCatalog;
        const string IconFolder = MapEditorPaths.UserIconFolder;
        const int Size = 256;
        const float FieldOfView = 24f;
        const float BoundsPadding = 1.25f;
        static readonly Vector3 ViewDirection = new Vector3(1f, 1f, 1f);

        [MenuItem(MapEditorInfo.ToolsMenu + "/Bake Placeable Icons", false, 200)]
        public static void BakeFromMenu()
        {
            BakeCatalog(ResolveCatalog(), false);
        }

        [MenuItem(MapEditorInfo.ToolsMenu + "/Bake Placeable Icons (Force Overwrite)", false, 201)]
        public static void BakeFromMenuForce()
        {
            if (!EditorUtility.DisplayDialog(
                    "Bake Placeable Icons",
                    "Overwrite all icons, including ones you assigned by hand?",
                    "Overwrite",
                    "Cancel"))
                return;

            BakeCatalog(ResolveCatalog(), true);
        }

        [MenuItem(MapEditorInfo.AssetsMenu + "/Bake Placeable Icons", false, 200)]
        public static void BakeFromSelection()
        {
            BakeFromSelection(false);
        }

        [MenuItem(MapEditorInfo.AssetsMenu + "/Bake Placeable Icons (Force Overwrite)", false, 201)]
        public static void BakeFromSelectionForce()
        {
            BakeFromSelection(true);
        }

        [MenuItem(MapEditorInfo.AssetsMenu + "/Bake Placeable Icons", true)]
        [MenuItem(MapEditorInfo.AssetsMenu + "/Bake Placeable Icons (Force Overwrite)", true)]
        public static bool ValidateBakeFromSelection()
        {
            return Selection.GetFiltered<PlaceableCatalog>(SelectionMode.Assets).Length > 0 ||
                   Selection.GetFiltered<PlaceableItemDef>(SelectionMode.Assets).Length > 0;
        }

        public static void BakeCatalog(PlaceableCatalog catalog, bool forceOverwrite)
        {
            BakeCatalog(catalog, forceOverwrite, true);
        }

        public static void BakeCatalog(PlaceableCatalog catalog, bool forceOverwrite, bool showSummary)
        {
            if (catalog == null)
            {
                if (showSummary)
                {
                    EditorUtility.DisplayDialog(
                        "Bake Placeable Icons",
                        "No PlaceableCatalog found. Select one, or keep " + MapEditorPaths.PlaceableCatalog + ".",
                        "OK");
                }
                return;
            }

            BakeDefs(catalog.Items, forceOverwrite, showSummary);
        }

        public static void BakeDef(PlaceableItemDef def, bool forceOverwrite)
        {
            if (def == null)
                return;

            BakeDefs(new[] { def }, forceOverwrite, true);
        }

        static void BakeFromSelection(bool forceOverwrite)
        {
            var catalogs = Selection.GetFiltered<PlaceableCatalog>(SelectionMode.Assets);
            if (catalogs.Length > 0)
            {
                for (var i = 0; i < catalogs.Length; i++)
                    BakeCatalog(catalogs[i], forceOverwrite);
                return;
            }

            BakeDefs(Selection.GetFiltered<PlaceableItemDef>(SelectionMode.Assets), forceOverwrite, true);
        }

        static PlaceableCatalog ResolveCatalog()
        {
            if (Selection.activeObject is PlaceableCatalog selected)
                return selected;

            return AssetDatabase.LoadAssetAtPath<PlaceableCatalog>(DefaultCatalogPath);
        }

        public static void BakeDefs(IReadOnlyList<PlaceableItemDef> defs, bool forceOverwrite, bool showSummary)
        {
            if (defs == null || defs.Count == 0)
            {
                if (showSummary)
                    EditorUtility.DisplayDialog("Bake Placeable Icons", "Nothing to bake.", "OK");
                return;
            }

            EnsureFolder(IconFolder);

            var baked = 0;
            var skipped = 0;
            var failed = 0;

            try
            {
                for (var i = 0; i < defs.Count; i++)
                {
                    var def = defs[i];
                    EditorUtility.DisplayProgressBar(
                        "Bake Placeable Icons",
                        def != null ? def.name : "Item",
                        defs.Count == 1 ? 0.5f : (float)i / defs.Count);

                    var result = BakeOne(def, forceOverwrite);
                    if (result == BakeResult.Baked)
                        baked++;
                    else if (result == BakeResult.Skipped)
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

            if (showSummary)
            {
                EditorUtility.DisplayDialog(
                    "Bake Placeable Icons",
                    $"Baked {baked}, skipped {skipped}, failed {failed}.\nIcons: {IconFolder}",
                    "OK");
            }
        }

        static BakeResult BakeOne(PlaceableItemDef def, bool forceOverwrite)
        {
            if (def == null)
                return BakeResult.Failed;

            if (def.Prefab == null)
            {
                Debug.LogWarning($"PlaceableIconBaker: '{def.name}' has no prefab.", def);
                return BakeResult.Failed;
            }

            if (!forceOverwrite && ShouldPreserveManualIcon(def.Icon))
                return BakeResult.Skipped;

            Texture2D preview = null;
            try
            {
                preview = RenderPrefab(def.Prefab);
                if (preview == null)
                {
                    Debug.LogWarning($"PlaceableIconBaker: '{def.name}' has no mesh to preview.", def);
                    return BakeResult.Failed;
                }

                var readable = CopyToReadableRgba(preview);
                UnityEngine.Object.DestroyImmediate(preview);
                preview = readable;
                PunchBackground(preview);

                var assetPath = $"{IconFolder}/{SanitizeFileName(def.name)}.png";
                var png = preview.EncodeToPNG();
                File.WriteAllBytes(ToFullPath(assetPath), png);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                ConfigureSpriteImporter(assetPath);

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite == null)
                {
                    Debug.LogError($"PlaceableIconBaker: failed to load sprite at {assetPath}.", def);
                    return BakeResult.Failed;
                }

                var so = new SerializedObject(def);
                so.FindProperty("icon").objectReferenceValue = sprite;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(def);
                return BakeResult.Baked;
            }
            catch (Exception exception)
            {
                Debug.LogError($"PlaceableIconBaker: '{def.name}' failed: {exception.Message}", def);
                return BakeResult.Failed;
            }
            finally
            {
                if (preview != null)
                    UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        static Texture2D RenderPrefab(GameObject prefab)
        {
            var preview = new PreviewRenderUtility(true);
            try
            {
                preview.cameraFieldOfView = FieldOfView;
                preview.ambientColor = new Color(0.42f, 0.42f, 0.46f);
                preview.camera.nearClipPlane = 0.01f;
                preview.camera.farClipPlane = 500f;
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = Color.clear;
                preview.camera.allowHDR = false;
                preview.camera.allowMSAA = true;
                preview.camera.orthographic = false;

                var view = ViewDirection.normalized;
                if (preview.lights != null && preview.lights.Length > 0)
                {
                    preview.lights[0].intensity = 1.55f;
                    preview.lights[0].color = Color.white;
                    preview.lights[0].shadows = LightShadows.None;
                    preview.lights[0].transform.rotation = Quaternion.LookRotation(-view);
                }

                if (preview.lights != null && preview.lights.Length > 1)
                {
                    preview.lights[1].intensity = 0.45f;
                    preview.lights[1].color = new Color(0.72f, 0.8f, 1f);
                    preview.lights[1].shadows = LightShadows.None;
                    preview.lights[1].transform.rotation = Quaternion.LookRotation(new Vector3(-1f, -0.2f, 0.4f));
                }

                var instance = UnityEngine.Object.Instantiate(prefab);
                instance.name = prefab.name;
                instance.hideFlags = HideFlags.HideAndDontSave;
                preview.AddSingleGO(instance);
                PreparePreviewInstance(instance);

                if (!TryGetBounds(instance, out var bounds))
                    return null;

                FrameCamera(preview.camera, bounds, view);
                preview.BeginStaticPreview(new Rect(0f, 0f, Size, Size));
                preview.Render(true);
                return preview.EndStaticPreview();
            }
            finally
            {
                preview.Cleanup();
            }
        }

        static void PreparePreviewInstance(GameObject instance)
        {
            var animators = instance.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
                animators[i].enabled = false;

            var particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particles.Length; i++)
                particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        static bool TryGetBounds(GameObject instance, out Bounds bounds)
        {
            bounds = new Bounds();
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            var hasBounds = false;
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;
                if (renderer is ParticleSystemRenderer)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds && bounds.extents.sqrMagnitude > 0.0000001f;
        }

        static void FrameCamera(Camera camera, Bounds bounds, Vector3 view)
        {
            var radius = Mathf.Max(bounds.extents.magnitude * BoundsPadding, 0.05f);
            var halfFov = FieldOfView * 0.5f * Mathf.Deg2Rad;
            var distance = radius / Mathf.Tan(halfFov);
            camera.transform.position = bounds.center + view * distance;
            camera.transform.LookAt(bounds.center, Vector3.up);
            camera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 3f);
            camera.farClipPlane = distance + radius * 3f;
        }

        static Texture2D CopyToReadableRgba(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
            copy.Apply(false, false);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        static void PunchBackground(Texture2D texture)
        {
            var pixels = texture.GetPixels32();
            var width = texture.width;
            var height = texture.height;
            if (pixels.Length == 0 || width < 2 || height < 2)
                return;

            if (pixels[0].a < 250 && pixels[width - 1].a < 250)
                return;

            var key = AverageCorners(pixels, width, height);
            const int hard = 24;
            const int soft = 54;
            for (var i = 0; i < pixels.Length; i++)
            {
                var distance = ColorDistance(pixels[i], key);
                if (distance <= hard)
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                }
                else if (distance < soft)
                {
                    var alpha = (byte)Mathf.Clamp((distance - hard) * 255 / (soft - hard), 0, 255);
                    pixels[i].a = alpha;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        static Color32 AverageCorners(Color32[] pixels, int width, int height)
        {
            var c0 = pixels[0];
            var c1 = pixels[width - 1];
            var c2 = pixels[(height - 1) * width];
            var c3 = pixels[height * width - 1];
            return new Color32(
                (byte)((c0.r + c1.r + c2.r + c3.r) / 4),
                (byte)((c0.g + c1.g + c2.g + c3.g) / 4),
                (byte)((c0.b + c1.b + c2.b + c3.b) / 4),
                255);
        }

        static int ColorDistance(Color32 a, Color32 b)
        {
            var dr = a.r - b.r;
            var dg = a.g - b.g;
            var db = a.b - b.b;
            return Mathf.Abs(dr) + Mathf.Abs(dg) + Mathf.Abs(db);
        }

        static bool ShouldPreserveManualIcon(Sprite icon)
        {
            if (icon == null)
                return false;

            var path = AssetDatabase.GetAssetPath(icon);
            if (string.IsNullOrEmpty(path))
                return true;

            return !path.StartsWith(IconFolder, StringComparison.OrdinalIgnoreCase);
        }

        static void ConfigureSpriteImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = Size;
            importer.SaveAndReimport();
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
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

        static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return string.IsNullOrWhiteSpace(name) ? "Item" : name.Trim();
        }

        static string ToFullPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
        }

        enum BakeResult
        {
            Baked,
            Skipped,
            Failed
        }
    }
}
