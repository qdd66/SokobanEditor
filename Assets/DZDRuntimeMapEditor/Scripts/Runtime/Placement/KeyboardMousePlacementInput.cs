using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    public sealed class KeyboardMousePlacementInput : IPlacementInput
    {
        public PlacementInput Read(PlacementConfig config)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            var place = mouse != null && mouse.leftButton.wasPressedThisFrame;
            var cancel = mouse != null && mouse.rightButton.wasPressedThisFrame;
            var rotate = 0f;
            var scale = 0f;
            var heightDelta = 0f;
            var lookDelta = Vector2.zero;

            if (keyboard != null && config != null)
            {
                var scaling = IsHeld(keyboard, config.ScaleModifierKey) ||
                              IsHeld(keyboard, config.ScaleModifierAltKey);
                var tap = config.TransformKeyStyle == RepeatKeyStyle.Tap;
                var axis = ReadAxis(keyboard, config.RotateLeftKey, config.RotateRightKey, tap);
                if (scaling)
                    scale = axis;
                else
                    rotate = axis;
            }

            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                lookDelta = mouse.delta.ReadValue();
                heightDelta = lookDelta.y;
            }

            return new PlacementInput(place, cancel, rotate, scale, heightDelta, lookDelta);
        }

        static float ReadAxis(Keyboard keyboard, Key left, Key right, bool tap)
        {
            var axis = 0f;
            if (IsActive(keyboard, right, tap))
                axis += 1f;
            if (IsActive(keyboard, left, tap))
                axis -= 1f;
            return axis;
        }

        static bool IsHeld(Keyboard keyboard, Key key)
        {
            return IsActive(keyboard, key, false);
        }

        static bool IsActive(Keyboard keyboard, Key key, bool tap)
        {
            if (key == Key.None)
                return false;

            var control = keyboard[key];
            if (control == null)
                return false;

            return tap ? control.wasPressedThisFrame : control.isPressed;
        }
    }
}
