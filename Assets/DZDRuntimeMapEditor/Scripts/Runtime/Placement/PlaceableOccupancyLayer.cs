namespace DZDMapEditor
{
    public enum PlaceableOccupancyLayer
    {
        [RuntimeLabel("Solid", "实体")]
        Solid = 0,

        [RuntimeLabel("Overlay", "标记")]
        Overlay = 1,

        [RuntimeLabel("Floor", "地面")]
        Floor = 2
    }

    public enum PlacementBlockReason
    {
        None = 0,
        Occupied = 1,
        InstanceCap = 2
    }
}
