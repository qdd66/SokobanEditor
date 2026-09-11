using UnityEngine;
using UnityEngine.InputSystem;

namespace Sokoban
{
    public readonly struct SokobanFrameInput
    {
        public SokobanFrameInput(Vector2Int move, bool reset, bool undo)
        {
            Move = move;
            Reset = reset;
            Undo = undo;
        }

        public Vector2Int Move { get; }
        public bool Reset { get; }
        public bool Undo { get; }
    }

    public sealed class SokobanKeyboardInput
    {
        public SokobanFrameInput Read(SokobanConfig config, bool blockMove)
        {
            var keyboard = Keyboard.current;
            if (config == null || keyboard == null)
                return default;

            var reset = WasPressed(keyboard, config.ResetKey);
            var undo = WasPressed(keyboard, config.UndoKey);
            if (blockMove)
                return new SokobanFrameInput(Vector2Int.zero, reset, undo);

            return new SokobanFrameInput(ReadPressedThisFrame(keyboard, config), reset, undo);
        }

        static Vector2Int ReadPressedThisFrame(Keyboard keyboard, SokobanConfig config)
        {
            var x = AxisPressed(keyboard, config.RightKey, config.LeftKey);
            var y = AxisPressed(keyboard, config.UpKey, config.DownKey);
            if (x != 0 && y != 0)
                return Vector2Int.zero;
            return new Vector2Int(x, y);
        }

        static int AxisPressed(Keyboard keyboard, Key positive, Key negative)
        {
            var pos = WasPressed(keyboard, positive);
            var neg = WasPressed(keyboard, negative);
            if (pos == neg)
                return 0;
            return pos ? 1 : -1;
        }

        static bool WasPressed(Keyboard keyboard, Key key)
        {
            return key != Key.None && keyboard[key].wasPressedThisFrame;
        }
    }
}
