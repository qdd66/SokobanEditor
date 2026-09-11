using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "FirstPersonFlyConfig",
        menuName = MapEditorInfo.CreateMenu + "/First Person Fly Config")]
    public sealed class FirstPersonFlyConfig : ScriptableObject
    {
        [Title("移动")]
        [LabelText("水平速度")]
        [Tooltip("WASD 在水平面飞行的速度。")]
        [SuffixLabel("米/秒", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Character, "Horizontal Speed", "水平速度", sliderMax: 40f, suffix: "m/s", suffixChinese: "米/秒")]
        [SerializeField, MinValue(0f)]
        float horizontalSpeed = 12f;

        [LabelText("升降速度")]
        [Tooltip("Space / Ctrl 垂直移动的速度。")]
        [SuffixLabel("米/秒", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Character, "Vertical Speed", "升降速度", sliderMax: 40f, suffix: "m/s", suffixChinese: "米/秒")]
        [SerializeField, MinValue(0f)]
        float verticalSpeed = 10f;

        [Title("视角")]
        [LabelText("鼠标灵敏度")]
        [Tooltip("鼠标移动到视角转动的倍率。")]
        [RuntimeEditable(SettingsCategory.Character, "Mouse Sensitivity", "鼠标灵敏度", sliderMax: 2f)]
        [SerializeField, MinValue(0f)]
        float lookSensitivity = 0.15f;

        [LabelText("反转垂直视角")]
        [Tooltip("勾选后鼠标上移时视角向下。")]
        [RuntimeEditable(SettingsCategory.Character, "Invert Y", "反转垂直视角")]
        [SerializeField]
        bool invertY;

        [LabelText("俯仰下限")]
        [Tooltip("低头的最大角度，0 为水平，负值朝下。")]
        [SuffixLabel("度", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Character, "Min Pitch", "俯仰下限", sliderMin: -89.9f, sliderMax: 0f, suffix: "deg", suffixChinese: "度")]
        [SerializeField, MinValue(-89.9f), MaxValue(0f)]
        float minPitch = -89f;

        [LabelText("俯仰上限")]
        [Tooltip("抬头的最大角度，0 为水平，正值朝上。")]
        [SuffixLabel("度", Overlay = true)]
        [RuntimeEditable(SettingsCategory.Character, "Max Pitch", "俯仰上限", sliderMin: 0f, sliderMax: 89.9f, suffix: "deg", suffixChinese: "度")]
        [SerializeField, MinValue(0f), MaxValue(89.9f)]
        float maxPitch = 89f;

        [Title("鼠标")]
        [LabelText("启用时锁定")]
        [Tooltip("进入游戏后立刻锁定并隐藏鼠标。")]
        [SerializeField]
        bool lockCursor = true;

        [LabelText("解锁鼠标")]
        [Tooltip("解除鼠标锁定，便于点编辑器或 UI。")]
        [SerializeField]
        Key unlockCursorKey = Key.Escape;

        [Title("快捷键")]
        [LabelText("前进")]
        [RuntimeEditable(SettingsCategory.Control, "Forward", "前进")]
        [SerializeField]
        Key forwardKey = Key.W;

        [LabelText("后退")]
        [RuntimeEditable(SettingsCategory.Control, "Back", "后退")]
        [SerializeField]
        Key backKey = Key.S;

        [LabelText("左移")]
        [RuntimeEditable(SettingsCategory.Control, "Left", "左移")]
        [SerializeField]
        Key leftKey = Key.A;

        [LabelText("右移")]
        [RuntimeEditable(SettingsCategory.Control, "Right", "右移")]
        [SerializeField]
        Key rightKey = Key.D;

        [LabelText("上升")]
        [RuntimeEditable(SettingsCategory.Control, "Rise", "上升")]
        [SerializeField]
        Key ascendKey = Key.Space;

        [LabelText("下降")]
        [RuntimeEditable(SettingsCategory.Control, "Descend", "下降")]
        [SerializeField]
        Key descendKey = Key.LeftCtrl;

        [LabelText("下降（备选）")]
        [Tooltip("另一侧 Ctrl，作用与下降相同。")]
        [RuntimeEditable(SettingsCategory.Control, "Descend (Alt)", "下降（备选）")]
        [SerializeField]
        Key descendAltKey = Key.RightCtrl;

        public float HorizontalSpeed => horizontalSpeed;
        public float VerticalSpeed => verticalSpeed;
        public float LookSensitivity => lookSensitivity;
        public bool InvertY => invertY;
        public float MinPitch => minPitch;
        public float MaxPitch => maxPitch;
        public bool LockCursor => lockCursor;
        public Key UnlockCursorKey => unlockCursorKey;
        public Key ForwardKey => forwardKey;
        public Key BackKey => backKey;
        public Key LeftKey => leftKey;
        public Key RightKey => rightKey;
        public Key AscendKey => ascendKey;
        public Key DescendKey => descendKey;
        public Key DescendAltKey => descendAltKey;
    }
}
