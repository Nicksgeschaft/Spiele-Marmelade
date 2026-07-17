namespace SpieleMarmelade.World
{
    public static class WorldConstants
    {
        // Measured from the imported mesh's bounds (0.0795, 0.05, 0.079) — the brick footprint
        // is NOT a perfect square, so X and Z need their own constant or adjacent tiles leave a
        // hairline gap/overlap on whichever axis uses the wrong value.
        public const float PlateWidth  = 0.0795f; // X footprint per grid cell
        public const float PlateDepth  = 0.0790f; // Z footprint per grid cell
        public const float PlateHeight = 0.0320f; // 1 plate unit in world units
        // Brick = 3 plate units = 0.096f world units
    }
}
