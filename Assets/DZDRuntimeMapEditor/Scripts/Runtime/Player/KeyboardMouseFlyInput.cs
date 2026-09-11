using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    public sealed class KeyboardMouseFlyInput : IFirstPersonFlyInput
    {
        public FlyInput Read(FirstPersonFlyConfig config)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            var planar = Vector2.zero;
            var vertical = 0f;
            var lookDelta = Vector2.zero;

            if (keyboard != null)
            {
                var chord = CtrlWithOtherKey(keyboard);
                if (!chord)
                {
                    if (keyboard[config.ForwardKey].isPressed) planar.y += 1f;
                    if (keyboard[config.BackKey].isPressed) planar.y -= 1f;
                    if (keyboard[config.RightKey].isPressed) planar.x += 1f;
                    if (keyboard[config.LeftKey].isPressed) planar.x -= 1f;
                }

                if (keyboard[config.AscendKey].isPressed) vertical += 1f;
                if (!chord)
                {
                    if (keyboard[config.DescendKey].isPressed) vertical -= 1f;
                    if (config.DescendAltKey != config.DescendKey &&
                        keyboard[config.DescendAltKey].isPressed)
                    {
                        vertical -= 1f;
                    }
                }
            }

            if (planar.sqrMagnitude > 1f)
                planar.Normalize();

            vertical = Mathf.Clamp(vertical, -1f, 1f);

            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
                lookDelta = mouse.delta.ReadValue();

            return new FlyInput(planar, vertical, lookDelta);
        }

        static bool CtrlWithOtherKey(Keyboard keyboard)
        {
            if (!keyboard.leftCtrlKey.isPressed &&
                !keyboard.rightCtrlKey.isPressed &&
                !keyboard.leftCommandKey.isPressed &&
                !keyboard.rightCommandKey.isPressed)
            {
                return false;
            }

            for (var key = Key.A; key <= Key.Z; key++)
            {
                if (keyboard[key].isPressed)
                    return true;
            }

            for (var key = Key.Digit1; key <= Key.Digit0; key++)
            {
                if (keyboard[key].isPressed)
                    return true;
            }

            for (var key = Key.F1; key <= Key.F12; key++)
            {
                if (keyboard[key].isPressed)
                    return true;
            }

            return false;
        }
    }
}
