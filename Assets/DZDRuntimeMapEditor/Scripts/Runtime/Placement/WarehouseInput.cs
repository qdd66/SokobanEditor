namespace DZDMapEditor
{
    public readonly struct WarehouseInput
    {
        public WarehouseInput(int scrollSteps, bool confirmPressed, bool toggleBackpackPressed)
        {
            ScrollSteps = scrollSteps;
            ConfirmPressed = confirmPressed;
            ToggleBackpackPressed = toggleBackpackPressed;
        }

        public int ScrollSteps { get; }
        public bool ConfirmPressed { get; }
        public bool ToggleBackpackPressed { get; }
    }
}
