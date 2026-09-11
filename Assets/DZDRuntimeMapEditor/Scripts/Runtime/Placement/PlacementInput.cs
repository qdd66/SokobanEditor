using UnityEngine;

namespace DZDMapEditor
{
    public readonly struct PlacementInput
    {
        public PlacementInput(
            bool placePressed,
            bool cancelPressed,
            float rotate,
            float scale,
            float heightDelta,
            Vector2 lookDelta)
        {
            PlacePressed = placePressed;
            CancelPressed = cancelPressed;
            Rotate = rotate;
            Scale = scale;
            HeightDelta = heightDelta;
            LookDelta = lookDelta;
        }

        public bool PlacePressed { get; }
        public bool CancelPressed { get; }
        public float Rotate { get; }
        public float Scale { get; }
        public float HeightDelta { get; }
        public Vector2 LookDelta { get; }
    }
}
