using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Sokoban
{
    [RequireComponent(typeof(Camera))]
    [InfoBox("场景里的 3D 俯视相机：略微倾斜看向棋盘。玩法中滚轮调节高度，不挂在玩家预制体上。")]
    public sealed class SokobanSceneCamera : MonoBehaviour
    {
        [Title("引用")]
        [LabelText("玩法配置")]
        [Tooltip("滚轮调高度的灵敏度、限位从这里读。可空，空则只自动取景。")]
        [SerializeField, AssetsOnly]
        SokobanConfig config;

        [Title("取景")]
        [LabelText("棋盘根")]
        [SerializeField, Required("请指定棋盘根"), SceneObjectsOnly]
        Transform boardRoot;

        [LabelText("俯仰")]
        [Tooltip("相对水平面的看向角度。90 为正上方，越小越侧。")]
        [SuffixLabel("度", Overlay = true)]
        [SerializeField, MinValue(35f), MaxValue(85f)]
        float pitch = 62f;

        [LabelText("偏航")]
        [Tooltip("水平旋转。0 表示从 -Z 一侧看向棋盘。")]
        [SuffixLabel("度", Overlay = true)]
        [SerializeField]
        float yaw;

        [LabelText("视野")]
        [SuffixLabel("度", Overlay = true)]
        [SerializeField, MinValue(20f), MaxValue(80f)]
        float fieldOfView = 40f;

        [LabelText("边距")]
        [Tooltip("框住棋盘时在包围盒外再留的空间。")]
        [SuffixLabel("米", Overlay = true)]
        [SerializeField, MinValue(0f)]
        float padding = 1.6f;

        [LabelText("最近距离")]
        [Tooltip("自动取景时的距离下限。有玩法配置时改用配置里的最近距离。")]
        [SuffixLabel("米", Overlay = true)]
        [SerializeField, MinValue(1f)]
        float minDistance = 8f;

        Camera cam;
        Vector3 lookCenter;
        float fittedDistance = 8f;
        float targetDistance = 8f;
        float currentDistance = 8f;
        float zoomVelocity;
        bool userHasZoomed;
        bool hasLookTarget;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void Start()
        {
            FrameBoard();
        }

        void OnDisable()
        {
            zoomVelocity = 0f;
        }

        void LateUpdate()
        {
            if (!Application.isPlaying || !hasLookTarget)
                return;
            if (Time.timeScale <= 0f)
                return;

            ReadScroll();
            ApplyPose(true);
        }

        public void FrameBoard()
        {
            if (boardRoot == null)
                return;
            if (cam == null)
                cam = GetComponent<Camera>();
            if (cam == null)
                return;

            cam.orthographic = false;
            cam.fieldOfView = fieldOfView;
            cam.nearClipPlane = 0.1f;

            var bounds = CalculateBounds(boardRoot);
            lookCenter = bounds.center;
            hasLookTarget = true;
            fittedDistance = FitDistance(cam, bounds, padding, MinAllowedDistance(), pitch);
            if (!userHasZoomed)
                targetDistance = fittedDistance;
            targetDistance = ClampDistance(targetDistance);
            currentDistance = targetDistance;
            zoomVelocity = 0f;
            ApplyPose(false);
        }

        void ReadScroll()
        {
            if (config == null || !config.CameraScrollZoom)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
                return;

            var raw = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(raw) < 0.01f)
                return;

            var steps = ScrollSteps(raw);
            if (config.InvertCameraScroll)
                steps = -steps;

            userHasZoomed = true;
            targetDistance = ClampDistance(targetDistance - steps * config.CameraScrollSensitivity);
        }

        void ApplyPose(bool smooth)
        {
            if (cam == null)
                cam = GetComponent<Camera>();
            if (cam == null || !hasLookTarget)
                return;

            var desired = ClampDistance(targetDistance);
            targetDistance = desired;
            var dt = Time.unscaledDeltaTime;
            var smoothTime = config != null ? config.CameraZoomSmooth : 0f;
            if (!smooth || smoothTime <= 0f || dt <= 0f)
            {
                currentDistance = desired;
                zoomVelocity = 0f;
            }
            else
            {
                currentDistance = Mathf.SmoothDamp(
                    currentDistance,
                    desired,
                    ref zoomVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    dt);
            }

            cam.farClipPlane = Mathf.Max(80f, currentDistance * 8f);
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(
                lookCenter - rotation * Vector3.forward * currentDistance,
                rotation);
        }

        float MinAllowedDistance()
        {
            return config != null ? config.CameraMinDistance : minDistance;
        }

        float ClampDistance(float distance)
        {
            var min = MinAllowedDistance();
            var max = config != null ? config.CameraMaxDistance : Mathf.Max(min + 0.1f, minDistance * 6f);
            return Mathf.Clamp(distance, min, max);
        }

        static float ScrollSteps(float raw)
        {
            if (Mathf.Abs(raw) >= 2f)
                return raw / 120f;
            return raw;
        }

        static float FitDistance(Camera camera, Bounds bounds, float padding, float minDistance, float pitch)
        {
            var pitchRad = Mathf.Max(15f, pitch) * Mathf.Deg2Rad;
            var halfWidth = bounds.extents.x + padding;
            var halfDepth = (bounds.extents.z + padding) / Mathf.Max(0.2f, Mathf.Sin(pitchRad));
            var halfFov = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var tan = Mathf.Max(0.01f, Mathf.Tan(halfFov));
            var vertical = halfDepth / tan;
            var horizontal = halfWidth / (tan * Mathf.Max(0.01f, camera.aspect));
            return Mathf.Max(minDistance, vertical, horizontal);
        }

        static Bounds CalculateBounds(Transform root)
        {
            var pieces = root.GetComponentsInChildren<SokobanPiece>();
            var hasPieceBounds = false;
            var bounds = new Bounds(root.position, Vector3.one);
            if (pieces != null)
            {
                for (var i = 0; i < pieces.Length; i++)
                {
                    var piece = pieces[i];
                    if (piece == null || piece.Role == SokobanRole.Floor)
                        continue;
                    if (!TryEncapsulateRenderers(piece.transform, ref bounds, ref hasPieceBounds))
                        EncapsulatePoint(piece.transform.position, ref bounds, ref hasPieceBounds);
                }
            }

            if (hasPieceBounds)
                return bounds;

            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
                return new Bounds(root.position, Vector3.one * 4f);

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        static bool TryEncapsulateRenderers(Transform root, ref Bounds bounds, ref bool hasBounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0)
                return false;

            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return hasBounds;
        }

        static void EncapsulatePoint(Vector3 point, ref Bounds bounds, ref bool hasBounds)
        {
            if (!hasBounds)
            {
                bounds = new Bounds(point, Vector3.one * 0.5f);
                hasBounds = true;
                return;
            }

            bounds.Encapsulate(point);
        }
    }
}
