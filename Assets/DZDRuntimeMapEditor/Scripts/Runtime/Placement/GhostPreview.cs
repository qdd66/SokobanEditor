using System.Collections.Generic;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class GhostPreview
    {
        const int IgnoreRaycastLayer = 2;

        readonly List<(Transform transform, int layer)> layers = new List<(Transform, int)>();
        readonly List<(Collider collider, bool enabled)> colliders = new List<(Collider, bool)>();
        readonly List<(Rigidbody body, bool kinematic)> bodies = new List<(Rigidbody, bool)>();
        readonly GhostVisual visual = new GhostVisual();

        GameObject instance;
        Transform restoreParent;
        Vector3 restoreLocalPosition;
        Quaternion restoreLocalRotation;
        Vector3 restoreLocalScale;
        Vector3 beforeWorldPosition;
        Quaternion beforeWorldRotation;
        Vector3 beforeWorldScale;
        Vector3 baseScale;
        float yaw;
        float extraPitch;
        float extraRoll;
        float heightOffset;
        bool isExisting;
        PlaceableItemDef definition;

        public bool IsActive => instance != null;
        public GameObject Instance => instance;
        public PlaceableItemDef Definition => definition;
        public bool IsExisting => isExisting;

        public void Tick(PlacementConfig config)
        {
            Tick(config, false);
        }

        public void Tick(PlacementConfig config, bool occupied)
        {
            if (!IsActive)
                return;
            visual.Tick(config, Time.time, occupied);
        }

        public void BeginExisting(PlacedItem item, PlacementConfig config)
        {
            ReleaseWithoutDestroy();
            instance = item.gameObject;
            definition = item.Definition;
            isExisting = true;
            item.EnsureInstanceId();
            CapturePose();
            instance.transform.SetParent(null, true);
            ExtractLocalEuler(instance.transform, out yaw, out extraPitch, out extraRoll);
            heightOffset = 0f;
            baseScale = ResolveBaseScale(definition, instance.transform);
            PrepareAsGhost(config);
        }

        public void BeginNew(GameObject prefab, PlacementConfig config, PlaceableItemDef def)
        {
            ReleaseWithoutDestroy();
            instance = Object.Instantiate(prefab);
            instance.name = prefab.name;
            var placed = instance.GetComponent<PlacedItem>();
            definition = def != null ? def : placed != null ? placed.Definition : null;
            if (definition != null)
                PlacedItem.Ensure(instance, definition);
            isExisting = false;
            yaw = instance.transform.eulerAngles.y;
            extraPitch = 0f;
            extraRoll = 0f;
            heightOffset = 0f;
            baseScale = instance.transform.localScale;
            PrepareAsGhost(config);
        }

        public void Follow(in PlacementHit hit, PlacementConfig config, bool snapToSurface, bool gridPlace)
        {
            if (!IsActive || !hit.HasHit)
                return;

            var extra = definition != null ? definition.ExtraSurfaceOffset : 0f;
            instance.transform.position = hit.Point
                                          + hit.Normal * (config.SurfaceOffset + extra)
                                          + Vector3.up * heightOffset;

            var local = Quaternion.Euler(extraPitch, yaw, extraRoll);
            var alignToSurface = !gridPlace &&
                                 (snapToSurface ||
                                  (definition != null && definition.AlignToSurfaceNormal));
            if (alignToSurface)
            {
                instance.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.Normal) * local;
            }
            else
            {
                instance.transform.rotation = local;
            }
        }

        public void AddHeight(float meters, PlacementConfig config)
        {
            if (!IsActive || config == null)
                return;

            var min = Mathf.Min(config.MinHeightOffset, config.MaxHeightOffset);
            var max = Mathf.Max(config.MinHeightOffset, config.MaxHeightOffset);
            heightOffset = Mathf.Clamp(heightOffset + meters, min, max);
        }

        public GhostStamp CaptureStamp()
        {
            if (!IsActive)
                return default;

            return new GhostStamp(definition, yaw, extraPitch, extraRoll, UniformScaleMagnitude(), heightOffset);
        }

        public void ApplyStamp(in GhostStamp stamp, PlacementConfig config)
        {
            if (!IsActive || config == null)
                return;

            yaw = stamp.Yaw;
            extraPitch = stamp.ExtraPitch;
            extraRoll = stamp.ExtraRoll;
            heightOffset = stamp.HeightOffset;
            SetUniformScaleMagnitude(stamp.UniformScale, config);
        }

        public void AddYaw(float degrees)
        {
            if (!IsActive)
                return;
            yaw += degrees;
        }

        public void AddPitch(float degrees)
        {
            if (!IsActive)
                return;
            extraPitch += degrees;
        }

        public void MultiplyUniformScale(float factor, PlacementConfig config)
        {
            if (!IsActive)
                return;

            var current = instance.transform.localScale;
            var magnitude = current.x / Mathf.Max(0.0001f, baseScale.x);
            magnitude = Mathf.Clamp(magnitude * factor, config.MinUniformScale, config.MaxUniformScale);
            instance.transform.localScale = baseScale * magnitude;
        }

        float UniformScaleMagnitude()
        {
            var divisor = Mathf.Max(0.0001f, Mathf.Abs(baseScale.x));
            return instance.transform.localScale.x / divisor;
        }

        void SetUniformScaleMagnitude(float magnitude, PlacementConfig config)
        {
            magnitude = Mathf.Clamp(magnitude, config.MinUniformScale, config.MaxUniformScale);
            instance.transform.localScale = baseScale * magnitude;
        }

        public PlacementMutation ConfirmPlace(Transform placedRoot)
        {
            if (!IsActive)
                return default;

            visual.Restore();
            RestorePhysicsAndLayers();
            if (placedRoot != null)
                instance.transform.SetParent(placedRoot, true);
            var item = PlacedItem.Ensure(instance, definition);
            item.EnsureInstanceId();
            var mutation = isExisting
                ? PlacementMutation.Transformed(item, beforeWorldPosition, beforeWorldRotation, beforeWorldScale)
                : PlacementMutation.PlacedNew(item);
            ClearState();
            return mutation;
        }

        public PlacementMutation DeleteHeld()
        {
            if (!IsActive)
                return default;

            PlacementMutation mutation = default;
            if (isExisting)
            {
                var item = instance.GetComponent<PlacedItem>();
                if (item != null)
                {
                    item.EnsureInstanceId();
                    var defId = item.Definition != null ? item.Definition.Id : string.Empty;
                    mutation = PlacementMutation.Deleted(new MapItemRecord(
                        item.InstanceId,
                        defId,
                        beforeWorldPosition,
                        beforeWorldRotation,
                        beforeWorldScale));
                }
            }

            visual.Restore();
            Object.Destroy(instance);
            ClearState();
            return mutation;
        }

        public void Cancel()
        {
            if (!IsActive)
                return;

            visual.Restore();
            if (isExisting)
            {
                RestorePhysicsAndLayers();
                RestorePose();
            }
            else
            {
                Object.Destroy(instance);
            }

            ClearState();
        }

        void PrepareAsGhost(PlacementConfig config)
        {
            layers.Clear();
            colliders.Clear();
            bodies.Clear();

            var transforms = instance.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < transforms.Length; i++)
            {
                layers.Add((transforms[i], transforms[i].gameObject.layer));
                transforms[i].gameObject.layer = IgnoreRaycastLayer;
            }

            var cols = instance.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < cols.Length; i++)
            {
                colliders.Add((cols[i], cols[i].enabled));
                cols[i].enabled = false;
            }

            var rbs = instance.GetComponentsInChildren<Rigidbody>(true);
            for (var i = 0; i < rbs.Length; i++)
            {
                bodies.Add((rbs[i], rbs[i].isKinematic));
                rbs[i].isKinematic = true;
            }

            visual.Apply(instance, config);
        }

        void CapturePose()
        {
            restoreParent = instance.transform.parent;
            restoreLocalPosition = instance.transform.localPosition;
            restoreLocalRotation = instance.transform.localRotation;
            restoreLocalScale = instance.transform.localScale;
            beforeWorldPosition = instance.transform.position;
            beforeWorldRotation = instance.transform.rotation;
            beforeWorldScale = instance.transform.localScale;
        }

        static void ExtractLocalEuler(Transform target, out float yaw, out float pitch, out float roll)
        {
            var up = target.up;
            var local = target.rotation;
            if (up.sqrMagnitude >= 0.0001f)
            {
                var align = Quaternion.FromToRotation(Vector3.up, up);
                local = Quaternion.Inverse(align) * target.rotation;
            }

            var euler = local.eulerAngles;
            yaw = euler.y;
            pitch = NormalizeAngle(euler.x);
            roll = NormalizeAngle(euler.z);
        }

        static float NormalizeAngle(float degrees)
        {
            degrees %= 360f;
            if (degrees > 180f)
                degrees -= 360f;
            else if (degrees < -180f)
                degrees += 360f;
            return degrees;
        }

        static Vector3 ResolveBaseScale(PlaceableItemDef def, Transform target)
        {
            if (def != null && def.Prefab != null)
                return def.Prefab.transform.localScale;
            return target.localScale;
        }

        void RestorePose()
        {
            instance.transform.SetParent(restoreParent, false);
            instance.transform.localPosition = restoreLocalPosition;
            instance.transform.localRotation = restoreLocalRotation;
            instance.transform.localScale = restoreLocalScale;
        }

        void RestorePhysicsAndLayers()
        {
            for (var i = 0; i < layers.Count; i++)
            {
                if (layers[i].transform != null)
                    layers[i].transform.gameObject.layer = layers[i].layer;
            }

            for (var i = 0; i < colliders.Count; i++)
            {
                if (colliders[i].collider != null)
                    colliders[i].collider.enabled = colliders[i].enabled;
            }

            for (var i = 0; i < bodies.Count; i++)
            {
                if (bodies[i].body != null)
                    bodies[i].body.isKinematic = bodies[i].kinematic;
            }
        }

        void ReleaseWithoutDestroy()
        {
            if (IsActive)
                Cancel();
        }

        void ClearState()
        {
            instance = null;
            layers.Clear();
            colliders.Clear();
            bodies.Clear();
            restoreParent = null;
            definition = null;
            isExisting = false;
            heightOffset = 0f;
            extraPitch = 0f;
            extraRoll = 0f;
        }
    }

    public readonly struct GhostStamp
    {
        public GhostStamp(
            PlaceableItemDef definition,
            float yaw,
            float extraPitch,
            float extraRoll,
            float uniformScale,
            float heightOffset)
        {
            Definition = definition;
            Yaw = yaw;
            ExtraPitch = extraPitch;
            ExtraRoll = extraRoll;
            UniformScale = uniformScale;
            HeightOffset = heightOffset;
        }

        public PlaceableItemDef Definition { get; }
        public float Yaw { get; }
        public float ExtraPitch { get; }
        public float ExtraRoll { get; }
        public float UniformScale { get; }
        public float HeightOffset { get; }

        public bool CanContinue => Definition != null && Definition.Prefab != null;
    }
}
