using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    [InfoBox("WASD 水平飞，Space / Ctrl 升降，鼠标转向。Esc 打开暂停。")]
    public sealed class FirstPersonFlyController : MonoBehaviour
    {
        [LabelText("飞行配置")]
        [Tooltip("速度、视角和快捷键。")]
        [SerializeField, Required("请指定飞行配置"), AssetsOnly, InlineEditor]
        FirstPersonFlyConfig config;

        [LabelText("视角枢轴")]
        [Tooltip("只绕这个节点做俯仰，水平转向在自身 Transform 上。")]
        [SerializeField, Required("请指定视角枢轴"), ChildGameObjectsOnly]
        Transform lookPivot;

        [LabelText("鼠标锁定")]
        [Tooltip("控制鼠标锁定与解锁。")]
        [SerializeField, Required("请指定鼠标锁定组件")]
        GameplayCursor gameplayCursor;

        [LabelText("放置模式")]
        [Tooltip("高度调节或旋转调节开启且正在预览时，占用的鼠标轴不再转镜头。高度调节只占用上下。可空。")]
        [SerializeField]
        PlacementModeState modeState;

        readonly KeyboardMouseFlyInput input = new KeyboardMouseFlyInput();
        readonly FirstPersonFlyMotor motor = new FirstPersonFlyMotor();
        float pitch;

        void OnEnable()
        {
            pitch = NormalizePitch(lookPivot != null ? lookPivot.localEulerAngles.x : 0f);
            if (config != null && config.LockCursor && gameplayCursor != null)
                gameplayCursor.ForceLock();
        }

        void Update()
        {
            if (config == null || lookPivot == null)
                return;

            var flyInput = input.Read(config);
            if (modeState != null && modeState.SuppressLook)
                flyInput = new FlyInput(flyInput.Planar, flyInput.Vertical, Vector2.zero);

            motor.Tick(transform, lookPivot, config, flyInput, Time.deltaTime, ref pitch);
        }

        public CameraPose CapturePose()
        {
            return new CameraPose(transform.position, transform.eulerAngles.y, pitch);
        }

        public void ApplyPose(CameraPose pose)
        {
            transform.position = pose.Position;
            transform.rotation = Quaternion.Euler(0f, pose.Yaw, 0f);
            pitch = NormalizePitch(pose.Pitch);
            if (lookPivot != null)
                lookPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        static float NormalizePitch(float eulerX)
        {
            if (eulerX > 180f)
                eulerX -= 360f;
            return eulerX;
        }
    }
}
