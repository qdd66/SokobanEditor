using UnityEngine;

namespace DZDMapEditor
{
    public sealed class PlacementRaycaster
    {
        public PlacementHit Cast(Camera camera, PlacementConfig config)
        {
            if (camera == null || config == null)
                return default;

            var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (!Physics.Raycast(
                    ray,
                    out var hit,
                    config.MaxDistance,
                    config.SurfaceMask,
                    QueryTriggerInteraction.Ignore))
            {
                return default;
            }

            return new PlacementHit(true, hit.point, hit.normal, PlacedItem.FindOn(hit.collider));
        }
    }
}
