using System.Collections.Generic;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class GhostVisual
    {
        struct RendererState
        {
            public Renderer Renderer;
            public uint OriginalLayerMask;
        }

        readonly List<RendererState> states = new List<RendererState>();
        MapEditorOverlaySettings overlay;

        public void Apply(GameObject instance, PlacementConfig config)
        {
            Apply(
                instance,
                config != null ? config.OverlaySettings : null,
                config != null ? config.GhostTint : default);
        }

        public void Apply(GameObject instance, MapEditorOverlaySettings settings, Color outlineTint)
        {
            Restore();
            if (instance == null || settings == null)
                return;

            overlay = settings;
            overlay.SetGhost(true, outlineTint);

            var bits = (uint)overlay.GhostLayer;
            if (bits == 0)
                return;

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (!IsSupported(renderer))
                    continue;

                states.Add(new RendererState
                {
                    Renderer = renderer,
                    OriginalLayerMask = renderer.renderingLayerMask
                });
                renderer.renderingLayerMask = renderer.renderingLayerMask | bits;
            }
        }

        public void Tick(PlacementConfig config, float time)
        {
            Tick(config, time, false);
        }

        public void Tick(PlacementConfig config, float time, bool occupied)
        {
            if (config == null)
                return;

            var tint = occupied ? config.GridOccupiedGhostTint : config.GhostTint;
            if (overlay != null)
                overlay.SetGhost(true, tint);
            Tick(tint, config.GhostBreathMinAlpha, config.GhostBreathMaxAlpha, config.GhostBreathSpeed, time);
        }

        public void Tick(Color tint, float minAlpha, float maxAlpha, float speed, float time)
        {
            if (overlay == null)
                return;

            var wave = (Mathf.Sin(time * speed * Mathf.PI * 2f) + 1f) * 0.5f;
            var brightness = Mathf.Lerp(0.55f, 1f, wave);
            var alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);
            overlay.SetGhostFillColor(new Color(tint.r * brightness, tint.g * brightness, tint.b * brightness, alpha));
        }

        public void Restore()
        {
            if (overlay != null)
            {
                overlay.SetGhost(false, overlay.GhostOutlineColor);
                overlay = null;
            }

            for (var i = 0; i < states.Count; i++)
            {
                if (states[i].Renderer != null)
                    states[i].Renderer.renderingLayerMask = states[i].OriginalLayerMask;
            }

            states.Clear();
        }

        static bool IsSupported(Renderer renderer)
        {
            return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
        }
    }
}
