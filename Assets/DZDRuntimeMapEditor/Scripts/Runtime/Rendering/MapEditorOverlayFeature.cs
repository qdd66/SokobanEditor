using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace DZDMapEditor
{
    static class OverlayShaderIds
    {
        public const string SilhouetteShaderName = "Hidden/DZDMapEditor/OverlaySilhouette";
        public const string CompositeShaderName = "Hidden/DZDMapEditor/OverlayComposite";
        public const string ScaleKeyword = "SCALE_WITH_RESOLUTION";

        public static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly int ZTestId = Shader.PropertyToID("_ZTest");
        public static readonly int AxisWidthId = Shader.PropertyToID("_AxisWidth");
        public static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        public static readonly int ReferenceResolutionId = Shader.PropertyToID("_ReferenceResolution");
        public static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        public static readonly int FillMaskId = Shader.PropertyToID("_FillMask");
        public static readonly int SilhouetteTexId = Shader.PropertyToID("_DZDOverlaySilhouette");

        public static readonly List<ShaderTagId> ShaderTags = new List<ShaderTagId>
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit")
        };
    }

    [ExcludeFromPreset]
    [DisallowMultipleRendererFeature("Map Editor Overlay")]
    [SupportedOnRenderer(typeof(UniversalRendererData))]
    [Tooltip("屏幕空间剪影填充与等宽描边，用于悬停高亮和幽灵预览。")]
    public sealed class MapEditorOverlayFeature : ScriptableRendererFeature
    {
        [SerializeField]
        MapEditorOverlaySettings settings;

        [SerializeField]
        Shader silhouetteShader;

        [SerializeField]
        Shader compositeShader;

        Material hoverSilhouette;
        Material ghostSilhouette;
        Material composite;
        OverlayPass pass;

        public MapEditorOverlaySettings Settings => settings;

        public override void Create()
        {
            pass ??= new OverlayPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings == null || pass == null)
                return;

            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection)
                return;
            if (Application.isPlaying && cameraType != CameraType.Game)
                return;
            if (cameraType == CameraType.SceneView && !settings.ShowInSceneView)
                return;
            if (UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;
            if (!settings.HoverActive && !settings.GhostActive)
                return;
            if (!EnsureMaterials())
                return;

            ApplyMaterialState();
            pass.Setup(settings, hoverSilhouette, ghostSilhouette, composite);
            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(hoverSilhouette);
            CoreUtils.Destroy(ghostSilhouette);
            CoreUtils.Destroy(composite);
            hoverSilhouette = null;
            ghostSilhouette = null;
            composite = null;
            pass = null;
        }

        bool EnsureMaterials()
        {
            var silhouette = silhouetteShader != null ? silhouetteShader : Shader.Find(OverlayShaderIds.SilhouetteShaderName);
            var blit = compositeShader != null ? compositeShader : Shader.Find(OverlayShaderIds.CompositeShaderName);
            if (silhouette == null || blit == null)
            {
                Debug.LogWarning("Map Editor Overlay shaders are missing.");
                return false;
            }

            if (hoverSilhouette == null)
                hoverSilhouette = CoreUtils.CreateEngineMaterial(silhouette);
            if (ghostSilhouette == null)
                ghostSilhouette = CoreUtils.CreateEngineMaterial(silhouette);
            if (composite == null)
                composite = CoreUtils.CreateEngineMaterial(blit);

            return hoverSilhouette != null && ghostSilhouette != null && composite != null;
        }

        void ApplyMaterialState()
        {
            var zTest = (float)CompareFunction.LessEqual;
            hoverSilhouette.SetFloat(OverlayShaderIds.ZTestId, zTest);
            ghostSilhouette.SetFloat(OverlayShaderIds.ZTestId, zTest);
            hoverSilhouette.SetColor(OverlayShaderIds.ColorId, settings.HoverOutlineColor);
            ghostSilhouette.SetColor(OverlayShaderIds.ColorId, settings.GhostOutlineColor);
            hoverSilhouette.SetFloat(OverlayShaderIds.FillMaskId, 0f);
            ghostSilhouette.SetFloat(OverlayShaderIds.FillMaskId, 1f);
            composite.SetFloat(OverlayShaderIds.OutlineWidthId, settings.OutlineWidth);
            composite.SetFloat(OverlayShaderIds.ReferenceResolutionId, settings.ReferenceResolution);
            composite.SetColor(OverlayShaderIds.FillColorId, settings.GhostFillColor);
            if (settings.ScaleWithResolution)
                composite.EnableKeyword(OverlayShaderIds.ScaleKeyword);
            else
                composite.DisableKeyword(OverlayShaderIds.ScaleKeyword);
        }

        sealed class OverlayPass : ScriptableRenderPass
        {
            MapEditorOverlaySettings settings;
            Material hoverSilhouette;
            Material ghostSilhouette;
            Material composite;

            public OverlayPass()
            {
                profilingSampler = new ProfilingSampler("Map Editor Overlay");
                requiresIntermediateTexture = true;
            }

            public void Setup(
                MapEditorOverlaySettings overlaySettings,
                Material hoverMaterial,
                Material ghostMaterial,
                Material compositeMaterial)
            {
                settings = overlaySettings;
                hoverSilhouette = hoverMaterial;
                ghostSilhouette = ghostMaterial;
                composite = compositeMaterial;
                renderPassEvent = overlaySettings.InjectionPoint;
                ConfigureInput(ScriptableRenderPassInput.Depth);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                if (resourceData.isActiveTargetBackBuffer)
                    return;

                var cameraDesc = resourceData.activeColorTexture.GetDescriptor(renderGraph);
                var width = cameraDesc.width;
                var height = cameraDesc.height;
                if (width < 2 || height < 2)
                    return;

                var drawFill = settings.GhostActive && (uint)settings.GhostLayer != 0;
                var drawHover = settings.HoverActive && (uint)settings.HoverLayer != 0;
                if (!drawFill && !drawHover)
                    return;

                RecordGeometryAndOutline(renderGraph, frameData, resourceData, cameraData, width, height, drawHover, drawFill);
            }

            void RecordGeometryAndOutline(
                RenderGraph renderGraph,
                ContextContainer frameData,
                UniversalResourceData resourceData,
                UniversalCameraData cameraData,
                int width,
                int height,
                bool drawHover,
                bool drawGhost)
            {
                var silhouette = CreateColorTexture(renderGraph, width, height, GraphicsFormat.R8G8B8A8_UNorm, "_DZDOverlaySilhouette");
                var fillMask = CreateColorTexture(renderGraph, width, height, GraphicsFormat.R8G8B8A8_UNorm, "_DZDOverlayFillMask");
                var ping = CreateColorTexture(renderGraph, width, height, GraphicsFormat.R16G16_SFloat, "_DZDOverlayPing");
                var pong = CreateColorTexture(renderGraph, width, height, GraphicsFormat.R16G16_SFloat, "_DZDOverlayPong");

                using (var builder = renderGraph.AddRasterRenderPass<SilhouettePassData>("Overlay Silhouette", out var passData))
                {
                    passData.hoverList = default;
                    passData.ghostList = default;

                    if (drawHover)
                    {
                        passData.hoverList = CreateOverrideList(renderGraph, frameData, hoverSilhouette, settings.HoverLayer);
                        if (passData.hoverList.IsValid())
                            builder.UseRendererList(passData.hoverList);
                    }

                    if (drawGhost)
                    {
                        passData.ghostList = CreateOverrideList(renderGraph, frameData, ghostSilhouette, settings.GhostLayer);
                        if (passData.ghostList.IsValid())
                            builder.UseRendererList(passData.ghostList);
                    }

                    builder.SetRenderAttachment(silhouette, 0);
                    builder.SetRenderAttachment(fillMask, 1);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                    builder.SetGlobalTextureAfterPass(silhouette, OverlayShaderIds.SilhouetteTexId);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (SilhouettePassData data, RasterGraphContext context) =>
                    {
                        if (data.hoverList.IsValid())
                            context.cmd.DrawRendererList(data.hoverList);
                        if (data.ghostList.IsValid())
                            context.cmd.DrawRendererList(data.ghostList);
                    });
                }

                if (drawGhost)
                {
                    using (var builder = renderGraph.AddRasterRenderPass<BlitPassData>("Overlay Fill", out var passData))
                    {
                        passData.source = fillMask;
                        passData.material = composite;
                        passData.passIndex = 3;
                        builder.UseTexture(fillMask);
                        builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                        builder.AllowPassCulling(false);
                        builder.SetRenderFunc(static (BlitPassData data, RasterGraphContext context) =>
                        {
                            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, data.passIndex);
                        });
                    }
                }

                using (var builder = renderGraph.AddUnsafePass<FloodPassData>("Overlay JFA", out var passData))
                {
                    passData.silhouette = silhouette;
                    passData.ping = ping;
                    passData.pong = pong;
                    passData.material = composite;
                    passData.jumps = ComputeJumpCount(cameraData);
                    builder.UseTexture(silhouette);
                    builder.UseTexture(ping, AccessFlags.ReadWrite);
                    builder.UseTexture(pong, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (FloodPassData data, UnsafeGraphContext context) =>
                    {
                        var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                        var load = RenderBufferLoadAction.DontCare;
                        var store = RenderBufferStoreAction.Store;
                        Blitter.BlitCameraTexture(cmd, data.silhouette, data.ping, load, store, data.material, 0);
                        for (var i = data.jumps - 1; i >= 0; i--)
                        {
                            var step = Mathf.Pow(2f, i) + 0.5f;
                            data.material.SetVector(OverlayShaderIds.AxisWidthId, new Vector2(step, 0f));
                            Blitter.BlitCameraTexture(cmd, data.ping, data.pong, load, store, data.material, 1);
                            data.material.SetVector(OverlayShaderIds.AxisWidthId, new Vector2(0f, step));
                            Blitter.BlitCameraTexture(cmd, data.pong, data.ping, load, store, data.material, 1);
                        }
                    });
                }

                using (var builder = renderGraph.AddRasterRenderPass<BlitPassData>("Overlay Outline", out var passData))
                {
                    passData.source = ping;
                    passData.material = composite;
                    passData.passIndex = 2;
                    builder.UseTexture(ping);
                    builder.UseTexture(silhouette);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                    builder.SetRenderFunc(static (BlitPassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, data.passIndex);
                    });
                }
            }

            int ComputeJumpCount(UniversalCameraData cameraData)
            {
                var width = settings.OutlineWidth * cameraData.renderScale;
                if (settings.ScaleWithResolution)
                    width *= cameraData.camera.pixelHeight / Mathf.Max(settings.ReferenceResolution, 1f);

                return Mathf.Clamp(Mathf.CeilToInt(Mathf.Log(width + 1f, 2f)), 1, 8);
            }

            static TextureHandle CreateColorTexture(
                RenderGraph renderGraph,
                int width,
                int height,
                GraphicsFormat format,
                string name)
            {
                var desc = new TextureDesc(width, height)
                {
                    name = name,
                    format = format,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    dimension = TextureDimension.Tex2D,
                    msaaSamples = MSAASamples.None,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    depthBufferBits = DepthBits.None
                };
                return renderGraph.CreateTexture(desc);
            }

            static RendererListHandle CreateOverrideList(
                RenderGraph renderGraph,
                ContextContainer frameData,
                Material overrideMaterial,
                RenderingLayerMask layer)
            {
                var renderingData = frameData.Get<UniversalRenderingData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                var lightData = frameData.Get<UniversalLightData>();
                var drawing = RenderingUtils.CreateDrawingSettings(
                    OverlayShaderIds.ShaderTags,
                    renderingData,
                    cameraData,
                    lightData,
                    cameraData.defaultOpaqueSortFlags);
                drawing.overrideMaterial = overrideMaterial;
                drawing.overrideMaterialPassIndex = 0;

                var filtering = new FilteringSettings(RenderQueueRange.all, -1, (uint)layer);
                var param = new RendererListParams(renderingData.cullResults, drawing, filtering);
                return renderGraph.CreateRendererList(param);
            }

            class SilhouettePassData
            {
                internal RendererListHandle hoverList;
                internal RendererListHandle ghostList;
            }

            class BlitPassData
            {
                internal TextureHandle source;
                internal Material material;
                internal int passIndex;
            }

            class FloodPassData
            {
                internal TextureHandle silhouette;
                internal TextureHandle ping;
                internal TextureHandle pong;
                internal Material material;
                internal int jumps;
            }
        }
    }
}
