using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class OutlineSelectionVisual : MonoBehaviour, ISelectionVisual
    {
        [LabelText("叠加设置")]
        [Tooltip("和摆放配置共用同一份 Overlay 资源。")]
        [SerializeField, Required("请指定叠加设置")]
        MapEditorOverlaySettings overlaySettings;

        GameObject current;
        readonly List<(Renderer renderer, uint mask)> originalMasks = new List<(Renderer, uint)>();

        public void Show(GameObject target, Color color, float width)
        {
            if (current != target)
                Hide();

            current = target;
            if (current == null || overlaySettings == null)
                return;

            overlaySettings.SetHover(true, color, width);
            if (originalMasks.Count > 0)
                return;

            var bits = (uint)overlaySettings.HoverLayer;
            if (bits == 0)
                return;

            var found = current.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < found.Length; i++)
            {
                var renderer = found[i];
                if (!IsSupported(renderer))
                    continue;

                originalMasks.Add((renderer, renderer.renderingLayerMask));
                renderer.renderingLayerMask = renderer.renderingLayerMask | bits;
            }
        }

        public void Hide()
        {
            if (overlaySettings != null)
                overlaySettings.SetHover(false, overlaySettings.HoverOutlineColor, overlaySettings.OutlineWidth);

            for (var i = 0; i < originalMasks.Count; i++)
            {
                if (originalMasks[i].renderer != null)
                    originalMasks[i].renderer.renderingLayerMask = originalMasks[i].mask;
            }

            originalMasks.Clear();
            current = null;
        }

        void OnDisable()
        {
            Hide();
        }

        static bool IsSupported(Renderer renderer)
        {
            return renderer is MeshRenderer || renderer is SkinnedMeshRenderer;
        }
    }
}
