using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    public enum ModeSwitchStyle
    {
        [LabelText("点按切换")]
        [RuntimeLabel("Toggle", "点按切换")]
        Toggle = 0,

        [LabelText("长按，松开还原")]
        [RuntimeLabel("Hold", "长按，松开还原")]
        Hold = 1
    }

    public enum RepeatKeyStyle
    {
        [LabelText("长按")]
        [RuntimeLabel("Hold", "长按")]
        Hold = 0,

        [LabelText("单次点击")]
        [RuntimeLabel("Tap", "单次点击")]
        Tap = 1
    }

    [CreateAssetMenu(
        fileName = "PlacementConfig",
        menuName = MapEditorInfo.CreateMenu + "/Placement Config")]
    public sealed class PlacementConfig : ScriptableObject
    {
        [Title("射线")]
        [LabelText("地表层")]
        [Tooltip("射线检测可放置表面使用的层。")]
        [SerializeField]
        LayerMask surfaceMask = ~0;

        [LabelText("最大距离")]
        [Tooltip("准心射线最远检测距离。")]
        [SuffixLabel("米", Overlay = true)]
        [SerializeField, MinValue(0.1f)]
        float maxDistance = 200f;

        [LabelText("表面偏移")]
        [Tooltip("物体相对命中点沿法线抬起的距离，避免穿模。")]
        [SuffixLabel("米", Overlay = true)]
        [SerializeField]
        float surfaceOffset = 0.02f;

        [Title("变换")]
        [LabelText("Q/E 触发")]
        [Tooltip("长按：按住 Q/E 持续旋转或缩放。单次点击：每按一下转过或缩放一个固定量。")]
        [EnumToggleButtons]
        [RuntimeEditable(SettingsCategory.Placement, "Q/E Repeat", "Q/E 触发")]
        [SerializeField]
        RepeatKeyStyle transformKeyStyle = RepeatKeyStyle.Hold;

        [LabelText("旋转速度")]
        [Tooltip("Q/E 为长按时，按住旋转键每秒转过的角度。")]
        [SuffixLabel("度/秒", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Placement, "Rotate Speed", "旋转速度", sliderMax: 360f, suffix: "deg/s", suffixChinese: "度/秒")]
        [SerializeField, MinValue(0f)]
        float rotateDegreesPerSecond = 90f;

        [LabelText("单击旋转角度")]
        [Tooltip("Q/E 为单次点击时，每按一下绕竖直轴转过的角度。")]
        [SuffixLabel("度", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Placement, "Rotate Per Tap", "单击旋转角度", sliderMax: 180f, suffix: "deg", suffixChinese: "度")]
        [SerializeField, MinValue(0f)]
        float rotateDegreesPerTap = 15f;

        [LabelText("缩放速度")]
        [Tooltip("Q/E 为长按时，按住缩放修饰键再按 Q/E，每秒的均匀缩放变化量。")]
        [SuffixLabel("倍/秒", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Placement, "Scale Speed", "缩放速度", sliderMax: 5f, suffix: "x/s", suffixChinese: "倍/秒")]
        [SerializeField, MinValue(0f)]
        float scaleRatePerSecond = 0.8f;

        [LabelText("单击缩放")]
        [Tooltip("Q/E 为单次点击时，按住缩放修饰键再点一下 Q/E 的均匀缩放变化量。")]
        [SuffixLabel("倍", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Placement, "Scale Per Tap", "单击缩放", sliderMax: 2f, suffix: "x", suffixChinese: "倍")]
        [SerializeField, MinValue(0f)]
        float scaleStepPerTap = 0.1f;

        [LabelText("最小缩放")]
        [Tooltip("均匀缩放下限。")]
        [RuntimeEditable(SettingsCategory.Placement, "Min Scale", "最小缩放", sliderMax: 4f)]
        [SerializeField, MinValue(0.01f)]
        float minUniformScale = 0.15f;

        [LabelText("最大缩放")]
        [Tooltip("均匀缩放上限。")]
        [RuntimeEditable(SettingsCategory.Placement, "Max Scale", "最大缩放", sliderMax: 16f)]
        [SerializeField, MinValue(0.01f)]
        float maxUniformScale = 8f;

        [Title("幽灵预览")]
        [InfoBox("把预览体加到 Overlay 的幽灵渲染层，由屏幕空间填充和描边画出半透明剪影。颜色在这里调，宽度和层在叠加设置里。")]
        [LabelText("叠加设置")]
        [Tooltip("URP Overlay Feature 使用的配置。幽灵预览和悬停描边都读这份。")]
        [SerializeField, Required("请指定叠加设置"), AssetsOnly]
        MapEditorOverlaySettings overlaySettings;

        [LabelText("呼吸颜色")]
        [Tooltip("填充呼吸使用的颜色。")]
        [SerializeField]
        Color ghostTint = new Color(0.4f, 0.9f, 1f, 1f);

        [LabelText("呼吸最低透明度")]
        [SerializeField, PropertyRange(0f, 1f)]
        float ghostBreathMinAlpha = 0.06f;

        [LabelText("呼吸最高透明度")]
        [SerializeField, PropertyRange(0f, 1f)]
        float ghostBreathMaxAlpha = 0.32f;

        [LabelText("呼吸速度")]
        [Tooltip("每秒呼吸次数。")]
        [SuffixLabel("次/秒", Overlay = true)]
        [SerializeField, MinValue(0f)]
        float ghostBreathSpeed = 0.85f;

        [Title("悬停描边")]
        [LabelText("悬停颜色")]
        [Tooltip("准心对准已放物体时的描边颜色。宽度在叠加设置里，单位是像素。")]
        [SerializeField]
        Color hoverColor = new Color(1f, 0.85f, 0.2f, 0.9f);

        [Title("仓库")]
        [LabelText("确认选取")]
        [Tooltip("从仓库热键栏取出当前高亮物品。")]
        [RuntimeEditable(SettingsCategory.Control, "Confirm Select", "确认选取")]
        [SerializeField]
        Key confirmSelectKey = Key.F;

        [LabelText("反转滚轮")]
        [Tooltip("勾选后滚轮方向与切换高亮的方向相反。")]
        [RuntimeEditable(SettingsCategory.Placement, "Invert Scroll", "反转滚轮")]
        [SerializeField]
        bool invertScroll;

        [LabelText("开关背包")]
        [Tooltip("打开或关闭分类背包面板。")]
        [RuntimeEditable(SettingsCategory.Control, "Backpack", "开关背包")]
        [SerializeField]
        Key toggleBackpackKey = Key.B;

        [Title("放置模式")]
        [LabelText("高度调节")]
        [Tooltip("预览时开关高度调节：鼠标上下改为抬高或压低物体，左右移动无效，不再转镜头。开启后锁定落点，只改高度，不跟地形和已放物品碰撞。")]
        [RuntimeEditable(SettingsCategory.Control, "Height Adjust", "高度调节")]
        [SerializeField]
        Key toggleHeightAdjustKey = Key.V;

        [LabelText("高度调节触发")]
        [Tooltip("点按：按一下开或关。长按：按住开启，松开后回到关闭。")]
        [EnumToggleButtons]
        [RuntimeEditable(SettingsCategory.Placement, "Height Adjust Mode", "高度调节触发")]
        [SerializeField]
        ModeSwitchStyle heightAdjustSwitchStyle = ModeSwitchStyle.Toggle;

        [LabelText("连续摆放")]
        [Tooltip("开关连续摆放：左键放下后保留旋转、缩放和高度，继续预览同一物品。")]
        [RuntimeEditable(SettingsCategory.Control, "Continuous Place", "连续摆放")]
        [SerializeField]
        Key toggleContinuousPlaceKey = Key.X;

        [LabelText("连续摆放触发")]
        [Tooltip("点按：按一下开或关。长按：按住开启，松开后回到关闭。")]
        [EnumToggleButtons]
        [RuntimeEditable(SettingsCategory.Placement, "Continuous Place Mode", "连续摆放触发")]
        [SerializeField]
        ModeSwitchStyle continuousSwitchStyle = ModeSwitchStyle.Toggle;

        [LabelText("贴合表面")]
        [Tooltip("预览时开关贴合表面：物体朝向随落点法线倾斜，可贴地、贴墙、贴斜面。")]
        [RuntimeEditable(SettingsCategory.Control, "Snap to Surface", "贴合表面")]
        [SerializeField]
        Key toggleSnapToSurfaceKey = Key.C;

        [LabelText("贴合表面触发")]
        [Tooltip("点按：按一下开或关。长按：按住开启，松开后回到关闭。")]
        [EnumToggleButtons]
        [RuntimeEditable(SettingsCategory.Placement, "Snap to Surface Mode", "贴合表面触发")]
        [SerializeField]
        ModeSwitchStyle snapToSurfaceSwitchStyle = ModeSwitchStyle.Toggle;

        [LabelText("旋转调节")]
        [Tooltip("预览时开关旋转调节：鼠标移动改为转动物体，不再转镜头。左右偏航，上下俯仰。")]
        [RuntimeEditable(SettingsCategory.Control, "Rotate Adjust", "旋转调节")]
        [SerializeField]
        Key toggleRotateAdjustKey = Key.CapsLock;

        [LabelText("旋转调节触发")]
        [Tooltip("点按：按一下开或关。长按：按住开启，松开后回到关闭。")]
        [EnumToggleButtons]
        [RuntimeEditable(SettingsCategory.Placement, "Rotate Adjust Mode", "旋转调节触发")]
        [SerializeField]
        ModeSwitchStyle rotateAdjustSwitchStyle = ModeSwitchStyle.Toggle;

        [LabelText("网格放置")]
        [Tooltip("预览时开关网格放置：水平吸到格子中心，网格线画在最底层；每个物品占一格。按住 Ctrl 时不切换，避免和重做抢 Y。")]
        [RuntimeEditable(SettingsCategory.Control, "Grid Place", "网格放置")]
        [SerializeField]
        Key toggleGridPlaceKey = Key.Y;

        [LabelText("网格放置触发")]
        [Tooltip("点按：按一下开或关。长按：按住开启，松开后回到关闭。")]
        [EnumToggleButtons]
        [RuntimeEditable(SettingsCategory.Placement, "Grid Place Mode", "网格放置触发")]
        [SerializeField]
        ModeSwitchStyle gridPlaceSwitchStyle = ModeSwitchStyle.Toggle;

        [LabelText("默认连续摆放")]
        [Tooltip("进入游戏时连续摆放是否打开。之后仍可用快捷键开关。长按触发时每帧以按键为准，不会沿用这个默认值。")]
        [SerializeField]
        bool defaultContinuousPlace = true;

        [LabelText("默认网格放置")]
        [Tooltip("进入游戏时网格放置是否打开。之后仍可用快捷键开关。长按触发时每帧以按键为准，不会沿用这个默认值。")]
        [SerializeField]
        bool defaultGridPlace = true;

        [Title("网格")]
        [LabelText("格子大小")]
        [Tooltip("水平方向每格的边长。")]
        [SuffixLabel("米", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Placement, "Grid Cell Size", "格子大小", sliderMin: 0.05f, sliderMax: 8f, suffix: "m", suffixChinese: "米")]
        [SerializeField, MinValue(0.05f)]
        float gridCellSize = 1f;

        [LabelText("原点 (X/Z)")]
        [Tooltip("格子从世界 XZ 的这个点开始切分。默认世界原点。")]
        [SerializeField]
        Vector2 gridOrigin;

        [LabelText("显示半径")]
        [Tooltip("准心周围画出的格子圈数。")]
        [SuffixLabel("格", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Placement, "Grid Visual Radius", "显示半径", sliderMin: 1f, sliderMax: 24f, suffix: "cells", suffixChinese: "格")]
        [SerializeField, MinValue(1)]
        int gridVisualRadius = 8;

        [LabelText("线宽")]
        [Tooltip("网格线在世界里的宽度。")]
        [SuffixLabel("米", Overlay = true)]
        [SerializeField, MinValue(0.005f)]
        float gridLineWidth = 0.02f;

        [LabelText("抬起")]
        [Tooltip("网格相对最底层向上抬起，减少和地面重叠。")]
        [SuffixLabel("米", Overlay = true)]
        [SerializeField]
        float gridYOffset = 0.03f;

        [LabelText("一格一件")]
        [Tooltip("勾选后，同一水平格子里已有物品时不能再放下。")]
        [RuntimeEditable(SettingsCategory.Placement, "One Item Per Cell", "一格一件")]
        [SerializeField]
        bool gridOccupancy = true;

        [LabelText("网格线颜色")]
        [SerializeField]
        Color gridLineColor = new Color(0.45f, 0.85f, 1f, 0.55f);

        [LabelText("空闲格填充")]
        [SerializeField]
        Color gridFreeFillColor = new Color(0.35f, 1f, 0.45f, 0.22f);

        [LabelText("占用格填充")]
        [SerializeField]
        Color gridOccupiedFillColor = new Color(1f, 0.28f, 0.22f, 0.28f);

        [LabelText("占用时幽灵颜色")]
        [Tooltip("目标格已被占用时，预览体改用这个颜色。")]
        [SerializeField]
        Color gridOccupiedGhostTint = new Color(1f, 0.35f, 0.3f, 1f);

        [LabelText("高度灵敏度")]
        [Tooltip("高度调节开启时，鼠标每移动 1 像素改变的高度。")]
        [SuffixLabel("米/像素", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Placement, "Height Sensitivity", "高度灵敏度", sliderMax: 0.2f, suffix: "m/px", suffixChinese: "米/像素")]
        [SerializeField, MinValue(0f)]
        float heightMetersPerPixel = 0.02f;

        [LabelText("最低高度偏移")]
        [Tooltip("相对地表落点的最低抬高。0 表示贴地。")]
        [SuffixLabel("米", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Placement, "Min Height Offset", "最低高度偏移", sliderMin: -20f, sliderMax: 40f, suffix: "m", suffixChinese: "米")]
        [SerializeField]
        float minHeightOffset;

        [LabelText("最高高度偏移")]
        [Tooltip("相对地表落点的最高抬高。")]
        [SuffixLabel("米", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Placement, "Max Height Offset", "最高高度偏移", sliderMax: 80f, suffix: "m", suffixChinese: "米")]
        [SerializeField, MinValue(0f)]
        float maxHeightOffset = 40f;

        [LabelText("旋转灵敏度")]
        [Tooltip("旋转调节开启时，鼠标每移动 1 像素改变的角度。")]
        [SuffixLabel("度/像素", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Placement, "Rotate Sensitivity", "旋转灵敏度", sliderMax: 2f, suffix: "deg/px", suffixChinese: "度/像素")]
        [SerializeField, MinValue(0f)]
        float rotateDegreesPerPixel = 0.25f;

        [Title("提示")]
        [LabelText("提示频道")]
        [Tooltip("模式切换等短提示发到这里。可空则不播报。")]
        [SerializeField, AssetsOnly]
        ToastChannel toastChannel;

        [LabelText("高度调节开启")]
        [Tooltip("按高度调节键打开时的提示文字。")]
        [SerializeField]
        LocalizedText heightAdjustOnHint = new LocalizedText("Height Adjust: On", "高度调节：开");

        [LabelText("高度调节关闭")]
        [Tooltip("按高度调节键关闭时的提示文字。")]
        [SerializeField]
        LocalizedText heightAdjustOffHint = new LocalizedText("Height Adjust: Off", "高度调节：关");

        [LabelText("连续摆放开启")]
        [Tooltip("按连续摆放键打开时的提示文字。")]
        [SerializeField]
        LocalizedText continuousOnHint = new LocalizedText("Continuous Place: On", "连续摆放：开");

        [LabelText("连续摆放关闭")]
        [Tooltip("按连续摆放键关闭时的提示文字。")]
        [SerializeField]
        LocalizedText continuousOffHint = new LocalizedText("Continuous Place: Off", "连续摆放：关");

        [LabelText("贴合表面开启")]
        [Tooltip("按贴合表面键打开时的提示文字。")]
        [SerializeField]
        LocalizedText snapToSurfaceOnHint = new LocalizedText("Snap to Surface: On", "贴合表面：开");

        [LabelText("贴合表面关闭")]
        [Tooltip("按贴合表面键关闭时的提示文字。")]
        [SerializeField]
        LocalizedText snapToSurfaceOffHint = new LocalizedText("Snap to Surface: Off", "贴合表面：关");

        [LabelText("旋转调节开启")]
        [Tooltip("按旋转调节键打开时的提示文字。")]
        [SerializeField]
        LocalizedText rotateAdjustOnHint = new LocalizedText("Rotate Adjust: On", "旋转调节：开");

        [LabelText("旋转调节关闭")]
        [Tooltip("按旋转调节键关闭时的提示文字。")]
        [SerializeField]
        LocalizedText rotateAdjustOffHint = new LocalizedText("Rotate Adjust: Off", "旋转调节：关");

        [LabelText("网格放置开启")]
        [Tooltip("按网格放置键打开时的提示文字。")]
        [SerializeField]
        LocalizedText gridPlaceOnHint = new LocalizedText("Grid Place: On", "网格放置：开");

        [LabelText("网格放置关闭")]
        [Tooltip("按网格放置键关闭时的提示文字。")]
        [SerializeField]
        LocalizedText gridPlaceOffHint = new LocalizedText("Grid Place: Off", "网格放置：关");

        [LabelText("格子已被占用")]
        [Tooltip("网格开启且目标格已有物品时，左键放下失败的提示。")]
        [SerializeField]
        LocalizedText gridOccupiedHint = new LocalizedText("Cell occupied", "此格已有物品");

        [LabelText("数量已达上限")]
        [Tooltip("该物品在场景里的数量已达到最大数量时，左键放下失败的提示。")]
        [SerializeField]
        LocalizedText gridInstanceCapHint = new LocalizedText("Placement limit reached", "已达数量上限");

        [Title("快捷键")]
        [LabelText("左转")]
        [Tooltip("预览时绕竖直轴逆时针旋转。长按或单击由 Q/E 触发决定。")]
        [RuntimeEditable(SettingsCategory.Control, "Rotate Left", "左转")]
        [SerializeField]
        Key rotateLeftKey = Key.Q;

        [LabelText("右转")]
        [Tooltip("预览时绕竖直轴顺时针旋转。长按或单击由 Q/E 触发决定。")]
        [RuntimeEditable(SettingsCategory.Control, "Rotate Right", "右转")]
        [SerializeField]
        Key rotateRightKey = Key.E;

        [LabelText("缩放修饰键")]
        [Tooltip("按住后，Q/E 改为缩放而不是旋转。")]
        [RuntimeEditable(SettingsCategory.Control, "Scale Modifier", "缩放修饰键")]
        [SerializeField]
        Key scaleModifierKey = Key.LeftShift;

        [LabelText("缩放修饰键（备选）")]
        [Tooltip("另一侧 Shift，作用与缩放修饰键相同。")]
        [RuntimeEditable(SettingsCategory.Control, "Scale Modifier (Alt)", "缩放修饰键（备选）")]
        [SerializeField]
        Key scaleModifierAltKey = Key.RightShift;

        public LayerMask SurfaceMask => surfaceMask;
        public float MaxDistance => maxDistance;
        public float SurfaceOffset => surfaceOffset;
        public RepeatKeyStyle TransformKeyStyle => transformKeyStyle;
        public float RotateDegreesPerSecond => rotateDegreesPerSecond;
        public float RotateDegreesPerTap => rotateDegreesPerTap;
        public float ScaleRatePerSecond => scaleRatePerSecond;
        public float ScaleStepPerTap => scaleStepPerTap;
        public float MinUniformScale => minUniformScale;
        public float MaxUniformScale => maxUniformScale;
        public MapEditorOverlaySettings OverlaySettings => overlaySettings;
        public Color GhostTint => ghostTint;
        public float GhostBreathMinAlpha => ghostBreathMinAlpha;
        public float GhostBreathMaxAlpha => ghostBreathMaxAlpha;
        public float GhostBreathSpeed => ghostBreathSpeed;
        public Color HoverColor => hoverColor;
        public float OutlineWidth => overlaySettings != null ? overlaySettings.OutlineWidth : 8f;
        public Key ConfirmSelectKey => confirmSelectKey;
        public bool InvertScroll => invertScroll;
        public Key ToggleBackpackKey => toggleBackpackKey;
        public Key ToggleHeightAdjustKey => toggleHeightAdjustKey;
        public ModeSwitchStyle HeightAdjustSwitchStyle => heightAdjustSwitchStyle;
        public Key ToggleContinuousPlaceKey => toggleContinuousPlaceKey;
        public ModeSwitchStyle ContinuousSwitchStyle => continuousSwitchStyle;
        public Key ToggleSnapToSurfaceKey => toggleSnapToSurfaceKey;
        public ModeSwitchStyle SnapToSurfaceSwitchStyle => snapToSurfaceSwitchStyle;
        public Key ToggleRotateAdjustKey => toggleRotateAdjustKey;
        public ModeSwitchStyle RotateAdjustSwitchStyle => rotateAdjustSwitchStyle;
        public Key ToggleGridPlaceKey => toggleGridPlaceKey;
        public ModeSwitchStyle GridPlaceSwitchStyle => gridPlaceSwitchStyle;
        public bool DefaultContinuousPlace => defaultContinuousPlace;
        public bool DefaultGridPlace => defaultGridPlace;
        public float GridCellSize => gridCellSize;
        public Vector2 GridOrigin => gridOrigin;
        public int GridVisualRadius => Mathf.Max(1, gridVisualRadius);
        public float GridLineWidth => gridLineWidth;
        public float GridYOffset => gridYOffset;
        public bool GridOccupancy => gridOccupancy;
        public Color GridLineColor => gridLineColor;
        public Color GridFreeFillColor => gridFreeFillColor;
        public Color GridOccupiedFillColor => gridOccupiedFillColor;
        public Color GridOccupiedGhostTint => gridOccupiedGhostTint;
        public float HeightMetersPerPixel => heightMetersPerPixel;
        public float MinHeightOffset => minHeightOffset;
        public float MaxHeightOffset => maxHeightOffset;
        public float RotateDegreesPerPixel => rotateDegreesPerPixel;
        public ToastChannel ToastChannel => toastChannel;
        public string HeightAdjustOnHint => heightAdjustOnHint.Get();
        public string HeightAdjustOffHint => heightAdjustOffHint.Get();
        public string ContinuousOnHint => continuousOnHint.Get();
        public string ContinuousOffHint => continuousOffHint.Get();
        public string SnapToSurfaceOnHint => snapToSurfaceOnHint.Get();
        public string SnapToSurfaceOffHint => snapToSurfaceOffHint.Get();
        public string RotateAdjustOnHint => rotateAdjustOnHint.Get();
        public string RotateAdjustOffHint => rotateAdjustOffHint.Get();
        public string GridPlaceOnHint => gridPlaceOnHint.Get();
        public string GridPlaceOffHint => gridPlaceOffHint.Get();
        public string GridOccupiedHint => gridOccupiedHint.Get();
        public string GridInstanceCapHint => gridInstanceCapHint.Get();
        public Key RotateLeftKey => rotateLeftKey;
        public Key RotateRightKey => rotateRightKey;
        public Key ScaleModifierKey => scaleModifierKey;
        public Key ScaleModifierAltKey => scaleModifierAltKey;
    }
}
