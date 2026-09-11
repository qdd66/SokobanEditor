using UnityEngine;

namespace DZDMapEditor
{
    public sealed class PlacementSession
    {
        readonly GhostPreview ghost;
        readonly ISelectionVisual visual;

        public PlacementSession(GhostPreview ghost, ISelectionVisual visual)
        {
            this.ghost = ghost;
            this.visual = visual;
        }

        public bool IsPreviewing => ghost != null && ghost.IsActive;

        public Transform GhostTransform => ghost != null && ghost.Instance != null ? ghost.Instance.transform : null;

        public PlaceableItemDef CurrentDefinition => ghost != null ? ghost.Definition : null;

        public bool IsRelocatingExisting => ghost != null && ghost.IsExisting;

        public void BeginNew(GameObject prefab, PlacementConfig config)
        {
            if (prefab == null)
                return;
            visual.Hide();
            ghost.BeginNew(prefab, config, null);
        }

        public void BeginNew(PlaceableItemDef def, PlacementConfig config)
        {
            if (def == null || def.Prefab == null)
                return;
            visual.Hide();
            ghost.BeginNew(def.Prefab, config, def);
        }

        public void BeginExisting(PlacedItem item, PlacementConfig config)
        {
            if (item == null)
                return;
            visual.Hide();
            ghost.BeginExisting(item, config);
        }

        public PlacementMutation Tick(
            in PlacementInput input,
            in PlacementHit hit,
            PlacementConfig config,
            Transform placedRoot,
            float deltaTime,
            bool allowWorldButtons,
            bool heightAdjust,
            bool continuousPlace,
            bool snapToSurface,
            bool rotateAdjust,
            bool gridPlace,
            bool occupied,
            out bool rejectedOccupied)
        {
            rejectedOccupied = false;
            if (config == null)
                return default;

            if (ghost.IsActive)
            {
                visual.Hide();
                ghost.Tick(config, occupied);

                if (heightAdjust && input.HeightDelta != 0f)
                    ghost.AddHeight(input.HeightDelta * config.HeightMetersPerPixel, config);

                if (input.Rotate != 0f)
                {
                    var degrees = config.TransformKeyStyle == RepeatKeyStyle.Hold
                        ? input.Rotate * config.RotateDegreesPerSecond * deltaTime
                        : input.Rotate * config.RotateDegreesPerTap;
                    ghost.AddYaw(degrees);
                }
                if (rotateAdjust && !heightAdjust)
                {
                    var deg = config.RotateDegreesPerPixel;
                    if (input.LookDelta.x != 0f)
                        ghost.AddYaw(input.LookDelta.x * deg);
                    if (input.LookDelta.y != 0f)
                        ghost.AddPitch(-input.LookDelta.y * deg);
                }

                if (input.Scale != 0f)
                {
                    var amount = config.TransformKeyStyle == RepeatKeyStyle.Hold
                        ? input.Scale * config.ScaleRatePerSecond * deltaTime
                        : input.Scale * config.ScaleStepPerTap;
                    ghost.MultiplyUniformScale(1f + amount, config);
                }

                ghost.Follow(hit, config, snapToSurface, gridPlace);

                if (allowWorldButtons && input.PlacePressed && hit.HasHit)
                {
                    if (occupied)
                    {
                        rejectedOccupied = true;
                        return default;
                    }

                    var stamp = ghost.CaptureStamp();
                    var mutation = ghost.ConfirmPlace(placedRoot);
                    visual.Hide();
                    if (continuousPlace && stamp.CanContinue)
                    {
                        ghost.BeginNew(stamp.Definition.Prefab, config, stamp.Definition);
                        ghost.ApplyStamp(stamp, config);
                        ghost.Follow(hit, config, snapToSurface, gridPlace);
                    }

                    return mutation;
                }

                if (allowWorldButtons && input.CancelPressed)
                {
                    var mutation = ghost.DeleteHeld();
                    visual.Hide();
                    return mutation;
                }

                return default;
            }

            if (hit.PlacedItem != null)
                visual.Show(hit.PlacedItem.gameObject, config.HoverColor, config.OutlineWidth);
            else
                visual.Hide();

            if (allowWorldButtons && input.PlacePressed && hit.PlacedItem != null)
                BeginExisting(hit.PlacedItem, config);

            return default;
        }

        public void Abort()
        {
            if (ghost.IsActive)
                ghost.Cancel();
            visual.Hide();
        }
    }
}
