using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class PlacementModeState : MonoBehaviour
    {
        [ShowInInspector, ReadOnly, LabelText("高度调节")]
        public bool HeightAdjust { get; private set; }

        [ShowInInspector, ReadOnly, LabelText("连续摆放")]
        public bool Continuous { get; private set; }

        [ShowInInspector, ReadOnly, LabelText("贴合表面")]
        public bool SnapToSurface { get; private set; }

        [ShowInInspector, ReadOnly, LabelText("旋转调节")]
        public bool RotateAdjust { get; private set; }

        [ShowInInspector, ReadOnly, LabelText("网格放置")]
        public bool Grid { get; private set; }

        [ShowInInspector, ReadOnly, LabelText("正在预览")]
        public bool IsPreviewing { get; private set; }

        public bool SuppressLook => IsPreviewing && (HeightAdjust || RotateAdjust);

        public void SetHeightAdjust(bool value)
        {
            HeightAdjust = value;
        }

        public void SetContinuous(bool value)
        {
            Continuous = value;
        }

        public void SetSnapToSurface(bool value)
        {
            SnapToSurface = value;
        }

        public void SetRotateAdjust(bool value)
        {
            RotateAdjust = value;
        }

        public void SetGrid(bool value)
        {
            Grid = value;
        }

        public void ToggleHeightAdjust()
        {
            HeightAdjust = !HeightAdjust;
        }

        public void ToggleContinuous()
        {
            Continuous = !Continuous;
        }

        public void ToggleSnapToSurface()
        {
            SnapToSurface = !SnapToSurface;
        }

        public void ToggleRotateAdjust()
        {
            RotateAdjust = !RotateAdjust;
        }

        public void ToggleGrid()
        {
            Grid = !Grid;
        }

        public void SetPreviewing(bool previewing)
        {
            IsPreviewing = previewing;
        }
    }
}
