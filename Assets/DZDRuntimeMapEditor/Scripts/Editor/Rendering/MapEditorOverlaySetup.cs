using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DZDMapEditor
{
    public static class MapEditorOverlaySetup
    {
        const string HoverLayerName = "HoverOutline";
        const string GhostLayerName = "GhostFill";
        const string FeatureName = "Map Editor Overlay";
        const string SettingsPath = MapEditorPaths.OverlaySettings;
        const string RendererPath = MapEditorPaths.UrpRenderer;
        const string PlacementConfigPath = MapEditorPaths.PlacementConfig;

        [MenuItem(MapEditorInfo.ToolsMenu + "/Setup Overlay Ghost And Outline", false, 320)]
        public static void SetupFromMenu()
        {
            Debug.Log("[DZDRuntimeMapEditor] " + Setup());
        }

        public static string Setup()
        {
            var hoverBits = EnsureRenderingLayerBits(HoverLayerName);
            var ghostBits = EnsureRenderingLayerBits(GhostLayerName);
            var settings = EnsureOverlaySettings(hoverBits, ghostBits);
            EnsureOverlayFeature(settings);
            DisableLineworkFeatures();
            WirePlacementConfig(settings);
            WireFlyer(settings);
            AssetDatabase.SaveAssets();
            return "HoverOutline bits=" + hoverBits + "; GhostFill bits=" + ghostBits;
        }

        static MapEditorOverlaySettings EnsureOverlaySettings(uint hoverBits, uint ghostBits)
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapEditorOverlaySettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<MapEditorOverlaySettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var so = new SerializedObject(settings);
            AssignMask(so.FindProperty("hoverLayer"), hoverBits);
            AssignMask(so.FindProperty("ghostLayer"), ghostBits);
            so.FindProperty("outlineWidth").floatValue = 8f;
            so.FindProperty("scaleWithResolution").boolValue = true;
            so.FindProperty("referenceResolution").floatValue = 1080f;
            so.FindProperty("showInSceneView").boolValue = true;
            so.FindProperty("hoverOutlineColor").colorValue = new Color(1f, 0.85f, 0.2f, 1f);
            so.FindProperty("ghostOutlineColor").colorValue = new Color(0.4f, 0.9f, 1f, 1f);
            so.FindProperty("ghostFillColor").colorValue = new Color(0.4f, 0.9f, 1f, 0.18f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            return settings;
        }

        static void EnsureOverlayFeature(MapEditorOverlaySettings settings)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
                throw new System.InvalidOperationException("PC_Renderer.asset not found.");

            MapEditorOverlayFeature feature = null;
            for (var i = 0; i < renderer.rendererFeatures.Count; i++)
            {
                if (renderer.rendererFeatures[i] is MapEditorOverlayFeature existing)
                {
                    feature = existing;
                    break;
                }
            }

            if (feature == null)
            {
                feature = ScriptableObject.CreateInstance<MapEditorOverlayFeature>();
                feature.name = FeatureName;
                AssetDatabase.AddObjectToAsset(feature, renderer);
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId))
                    throw new System.InvalidOperationException("Could not get renderer feature local file ID.");

                var rendererSo = new SerializedObject(renderer);
                var features = rendererSo.FindProperty("m_RendererFeatures");
                var map = rendererSo.FindProperty("m_RendererFeatureMap");
                features.arraySize++;
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                rendererSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var featureSo = new SerializedObject(feature);
            featureSo.FindProperty("settings").objectReferenceValue = settings;
            featureSo.FindProperty("silhouetteShader").objectReferenceValue =
                Shader.Find("Hidden/DZDMapEditor/OverlaySilhouette");
            featureSo.FindProperty("compositeShader").objectReferenceValue =
                Shader.Find("Hidden/DZDMapEditor/OverlayComposite");
            featureSo.ApplyModifiedPropertiesWithoutUndo();
            feature.SetActive(true);
            renderer.SetDirty();
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(renderer);
        }

        static void DisableLineworkFeatures()
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
                return;

            var changed = false;
            for (var i = 0; i < renderer.rendererFeatures.Count; i++)
            {
                var feature = renderer.rendererFeatures[i];
                if (feature == null)
                    continue;

                var typeName = feature.GetType().FullName;
                if (typeName != "Linework.WideOutline.WideOutline" &&
                    typeName != "Linework.SurfaceFill.SurfaceFill")
                    continue;

                feature.SetActive(false);
                EditorUtility.SetDirty(feature);
                changed = true;
            }

            if (changed)
            {
                renderer.SetDirty();
                EditorUtility.SetDirty(renderer);
            }
        }

        static void WirePlacementConfig(MapEditorOverlaySettings settings)
        {
            var config = AssetDatabase.LoadAssetAtPath<PlacementConfig>(PlacementConfigPath);
            if (config == null)
                throw new System.InvalidOperationException("PlacementConfig.asset not found.");

            var so = new SerializedObject(config);
            so.FindProperty("overlaySettings").objectReferenceValue = settings;
            so.FindProperty("ghostTint").colorValue = new Color(0.4f, 0.9f, 1f, 1f);
            so.FindProperty("hoverColor").colorValue = new Color(1f, 0.85f, 0.2f, 0.9f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        static void WireFlyer(MapEditorOverlaySettings settings)
        {
            var flyer = AssetDatabase.LoadAssetAtPath<GameObject>(ResolveFlyerPath());
            if (flyer == null)
                throw new System.InvalidOperationException("FirstPersonFlyer.prefab not found.");

            var visual = flyer.GetComponentInChildren<OutlineSelectionVisual>(true);
            if (visual == null)
                throw new System.InvalidOperationException("OutlineSelectionVisual missing on FirstPersonFlyer.");

            var so = new SerializedObject(visual);
            so.FindProperty("overlaySettings").objectReferenceValue = settings;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(visual);
            EditorUtility.SetDirty(flyer);
        }

        static string ResolveFlyerPath()
        {
            var userPath = MapEditorPaths.UserPrefabRoot + "/Player/FirstPersonFlyer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(userPath) != null)
                return userPath;
            return MapEditorPaths.FlyerPrefab;
        }

        static uint EnsureRenderingLayerBits(string name)
        {
            if (TryGetDefinedLayerBits(name, out var existing))
                return existing;

            if (!RenderPipelineEditorUtility.TryAddRenderingLayerName(name))
                throw new System.InvalidOperationException("Could not add rendering layer: " + name);

            var tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagManager != null && tagManager.Length > 0)
            {
                EditorUtility.SetDirty(tagManager[0]);
                AssetDatabase.SaveAssetIfDirty(tagManager[0]);
            }

            if (TryGetDefinedLayerBits(name, out var added))
                return added;

            throw new System.InvalidOperationException("Rendering layer was added but not found: " + name);
        }

        static bool TryGetDefinedLayerBits(string name, out uint bits)
        {
            var names = RenderingLayerMask.GetDefinedRenderingLayerNames();
            var values = RenderingLayerMask.GetDefinedRenderingLayerValues();
            for (var i = 0; i < names.Length && i < values.Length; i++)
            {
                if (names[i] != name)
                    continue;
                bits = (uint)values[i];
                return true;
            }

            bits = 0;
            return false;
        }

        static void AssignMask(SerializedProperty property, uint bits)
        {
            if (property == null)
                throw new System.InvalidOperationException("RenderingLayerMask property missing.");

            var bitsProp = property.FindPropertyRelative("m_Bits");
            if (bitsProp != null)
            {
                bitsProp.longValue = bits;
                return;
            }

            property.longValue = bits;
        }
    }
}
