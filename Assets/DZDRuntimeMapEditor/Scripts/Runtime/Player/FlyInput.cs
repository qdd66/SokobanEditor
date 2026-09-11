using UnityEngine;

namespace DZDMapEditor
{
    public readonly struct FlyInput
    {
        public FlyInput(Vector2 planar, float vertical, Vector2 lookDelta)
        {
            Planar = planar;
            Vertical = vertical;
            LookDelta = lookDelta;
        }

        /// <summary>WASD on XZ. X = right, Y = forward.</summary>
        public Vector2 Planar { get; }

        /// <summary>Positive = up, negative = down.</summary>
        public float Vertical { get; }

        public Vector2 LookDelta { get; }
    }
}
