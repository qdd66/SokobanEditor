using UnityEngine;
using UnityEngine.InputSystem;

namespace DZDMapEditor
{
    public sealed class KeyboardMouseWarehouseInput
    {
        public WarehouseInput Read(PlacementConfig config)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var steps = 0;
            var confirm = false;
            var toggleBackpack = false;

            if (mouse != null)
            {
                var y = mouse.scroll.ReadValue().y;
                if (y > 0.01f)
                    steps = 1;
                else if (y < -0.01f)
                    steps = -1;

                if (config != null && config.InvertScroll)
                    steps = -steps;
            }

            if (keyboard != null && config != null)
            {
                confirm = keyboard[config.ConfirmSelectKey].wasPressedThisFrame;
                toggleBackpack = keyboard[config.ToggleBackpackKey].wasPressedThisFrame;
            }

            return new WarehouseInput(steps, confirm, toggleBackpack);
        }
    }
}
