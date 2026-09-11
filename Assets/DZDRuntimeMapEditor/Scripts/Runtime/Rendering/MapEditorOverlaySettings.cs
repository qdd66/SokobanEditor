using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "MapEditorOverlaySettings",
        menuName = MapEditorInfo.CreateMenu + "/Overlay Settings")]
    public sealed class MapEditorOverlaySettings : ScriptableObject
    {
        [Title("注入")]
        [LabelText("插入点")]
        [Tooltip("叠在场景画面上的时机。默认在后处理之后，避免被颜色分级冲掉。")]
        [SerializeField]
        RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;

        [LabelText("Scene 视图也显示")]
        [SerializeField]
        bool showInSceneView = true;

        [Title("渲染层")]
        [InfoBox("物体只要带上对应渲染层就会被画填充或描边。悬停和幽灵预览在运行时开关这两层。")]
        [LabelText("悬停描边层")]
        [SerializeField]
        RenderingLayerMask hoverLayer;

        [LabelText("幽灵填充层")]
        [SerializeField]
        RenderingLayerMask ghostLayer;

        [Title("描边")]
        [LabelText("宽度")]
        [Tooltip("屏幕空间像素宽度，不随物体缩放改变。")]
        [SuffixLabel("像素", Overlay = true)]
        [SerializeField, PropertyRange(1f, 32f)]
        float outlineWidth = 8f;

        [LabelText("随分辨率缩放")]
        [Tooltip("以参考高度为基准，分辨率变高时描边保持视觉粗细。")]
        [SerializeField]
        bool scaleWithResolution = true;

        [LabelText("参考高度")]
        [SuffixLabel("像素", Overlay = true)]
        [SerializeField, MinValue(1f)]
        float referenceResolution = 1080f;

        [LabelText("悬停颜色")]
        [SerializeField]
        Color hoverOutlineColor = new Color(1f, 0.85f, 0.2f, 1f);

        [LabelText("幽灵描边颜色")]
        [SerializeField]
        Color ghostOutlineColor = new Color(0.4f, 0.9f, 1f, 1f);

        [Title("填充")]
        [LabelText("幽灵填充颜色")]
        [Tooltip("运行时呼吸会改这里的透明度和亮度。")]
        [SerializeField]
        Color ghostFillColor = new Color(0.4f, 0.9f, 1f, 0.18f);

        public RenderPassEvent InjectionPoint => injectionPoint;
        public bool ShowInSceneView => showInSceneView;
        public RenderingLayerMask HoverLayer => hoverLayer;
        public RenderingLayerMask GhostLayer => ghostLayer;
        public float OutlineWidth => outlineWidth;
        public bool ScaleWithResolution => scaleWithResolution;
        public float ReferenceResolution => referenceResolution;
        public Color HoverOutlineColor => hoverOutlineColor;
        public Color GhostOutlineColor => ghostOutlineColor;
        public Color GhostFillColor => ghostFillColor;
        public bool HoverActive { get; private set; }
        public bool GhostActive { get; private set; }

        public void SetHover(bool active, Color color, float widthPixels)
        {
            HoverActive = active;
            if (!active)
                return;

            hoverOutlineColor = color;
            if (widthPixels >= 1f)
                outlineWidth = widthPixels;
        }

        public void SetGhost(bool active, Color outlineColor)
        {
            GhostActive = active;
            if (!active)
                return;

            ghostOutlineColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 1f);
        }

        public void SetGhostFillColor(Color color)
        {
            ghostFillColor = color;
        }
    }
}
