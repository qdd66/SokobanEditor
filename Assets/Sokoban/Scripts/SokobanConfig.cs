using DZDMapEditor;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sokoban
{
    [CreateAssetMenu(fileName = "SokobanConfig", menuName = "推箱子/玩法配置")]
    public sealed class SokobanConfig : ScriptableObject
    {
        [Title("网格")]
        [LabelText("格子大小")]
        [Tooltip("水平方向每格边长。场景里摆放时请按这个尺寸对齐。")]
        [SuffixLabel("米", Overlay = true)]
        [SerializeField, MinValue(0.05f)]
        float cellSize = 1f;

        [LabelText("原点 (X/Z)")]
        [Tooltip("格子从世界 XZ 的这个点开始切分。")]
        [SerializeField]
        Vector2 gridOrigin;

        [Title("移动")]
        [LabelText("前进")]
        [RuntimeEditable(SettingsCategory.Sokoban, "Forward", "前进")]
        [SerializeField]
        Key upKey = Key.W;

        [LabelText("后退")]
        [RuntimeEditable(SettingsCategory.Sokoban, "Back", "后退")]
        [SerializeField]
        Key downKey = Key.S;

        [LabelText("左移")]
        [RuntimeEditable(SettingsCategory.Sokoban, "Left", "左移")]
        [SerializeField]
        Key leftKey = Key.A;

        [LabelText("右移")]
        [RuntimeEditable(SettingsCategory.Sokoban, "Right", "右移")]
        [SerializeField]
        Key rightKey = Key.D;

        [LabelText("重置本关")]
        [Tooltip("把人和箱子放回场景里摆好的初始位置。")]
        [RuntimeEditable(SettingsCategory.Sokoban, "Reset Level", "重置本关")]
        [SerializeField]
        Key resetKey = Key.R;

        [LabelText("撤回")]
        [Tooltip("撤销上一次成功的移动。")]
        [RuntimeEditable(SettingsCategory.Sokoban, "Undo", "撤回")]
        [SerializeField]
        Key undoKey = Key.Z;

        [LabelText("最大撤回步数")]
        [Tooltip("超出后丢掉最早的一步。")]
        [SerializeField, MinValue(1)]
        int maxUndoSteps = 64;

        [LabelText("移动时长")]
        [Tooltip("视觉平移时间。0 表示瞬间落格。")]
        [SuffixLabel("秒", Overlay = true)]
        [SerializeField, MinValue(0f)]
        float moveDuration = 0.08f;

        [Title("编辑")]
        [LabelText("切换编辑模式")]
        [Tooltip("玩法和 DZD 地图编辑之间切换。暂停或改键时无效。")]
        [RuntimeEditable(SettingsCategory.Sokoban, "Toggle Edit", "切换编辑")]
        [SerializeField]
        Key toggleEditKey = Key.F1;

        [Title("相机")]
        [LabelText("滚轮调高度")]
        [Tooltip("玩法模式下用鼠标滚轮拉近或拉远俯视相机。暂停和编辑时无效。")]
        [SerializeField]
        bool cameraScrollZoom = true;

        [LabelText("反转滚轮")]
        [Tooltip("勾选后向上滚是拉远，向下滚是拉近。")]
        [RuntimeEditable(SettingsCategory.Sokoban, "Invert Camera Scroll", "反转相机滚轮")]
        [SerializeField]
        bool invertCameraScroll;

        [LabelText("滚轮灵敏度")]
        [Tooltip("滚轮每一格改变的相机距离。")]
        [SuffixLabel("米", Overlay = true)]
        [RuntimeEditable(
            SettingsCategory.Sokoban,
            "Camera Scroll",
            "相机滚轮",
            sliderMin: 0.2f,
            sliderMax: 6f,
            suffix: "m",
            suffixChinese: "米")]
        [SerializeField, MinValue(0.05f)]
        float cameraScrollSensitivity = 1.4f;

        [LabelText("最近距离")]
        [Tooltip("滚轮拉近的下限。")]
        [SuffixLabel("米", Overlay = true)]
        [RuntimeEditable(
            SettingsCategory.Sokoban,
            "Camera Min Distance",
            "相机最近",
            sliderMin: 2f,
            sliderMax: 20f,
            suffix: "m",
            suffixChinese: "米")]
        [SerializeField, MinValue(1f)]
        float cameraMinDistance = 5f;

        [LabelText("最远距离")]
        [Tooltip("滚轮拉远的上限。")]
        [SuffixLabel("米", Overlay = true)]
        [RuntimeEditable(
            SettingsCategory.Sokoban,
            "Camera Max Distance",
            "相机最远",
            sliderMin: 8f,
            sliderMax: 60f,
            suffix: "m",
            suffixChinese: "米")]
        [SerializeField, MinValue(2f)]
        float cameraMaxDistance = 32f;

        [LabelText("高度平滑")]
        [Tooltip("滚轮后相机跟上的时间。0 表示立刻到位。")]
        [SuffixLabel("秒", Overlay = true)]
        [SerializeField, MinValue(0f)]
        float cameraZoomSmooth = 0.1f;

        [Title("文案")]
        [LabelText("操作提示")]
        [SerializeField]
        string hintText = "WASD 移动    滚轮 高度    Z 撤回    R 重置    F1 编辑";

        [LabelText("过关提示")]
        [SerializeField]
        string winText = "关卡完成";

        public float CellSize => cellSize;
        public Vector2 GridOrigin => gridOrigin;
        public Key UpKey => upKey;
        public Key DownKey => downKey;
        public Key LeftKey => leftKey;
        public Key RightKey => rightKey;
        public Key ResetKey => resetKey;
        public Key UndoKey => undoKey;
        public int MaxUndoSteps => maxUndoSteps;
        public float MoveDuration => moveDuration;
        public Key ToggleEditKey => toggleEditKey;
        public bool CameraScrollZoom => cameraScrollZoom;
        public bool InvertCameraScroll => invertCameraScroll;
        public float CameraScrollSensitivity => Mathf.Max(0.05f, cameraScrollSensitivity);
        public float CameraMinDistance => Mathf.Max(1f, cameraMinDistance);
        public float CameraMaxDistance => Mathf.Max(CameraMinDistance + 0.1f, cameraMaxDistance);
        public float CameraZoomSmooth => Mathf.Max(0f, cameraZoomSmooth);
        public string HintText => hintText;
        public string WinText => winText;
    }
}
