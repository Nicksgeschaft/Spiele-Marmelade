namespace GameJamUniverse.World
{
    // Single source of truth for per-shape brick behaviour.
    // Square bricks (Plate/Brick) fill their 1x1 cell completely and tile
    // seamlessly, so they're safe to greedy-mesh/voxelize in ChunkData/BrickWorld.
    // Round bricks (PlateRound/BrickRound) don't fill their cell — they are
    // placed as individual decoration props and must never enter the voxel grid.
    public static class BrickShapeInfo
    {
        public static int HeightInPlates(BrickType type) => type switch
        {
            BrickType.Plate      => 1,
            BrickType.Brick      => 3,
            BrickType.PlateRound => 1,
            BrickType.BrickRound => 3,
            _                    => 0,
        };

        public static bool IsStructural(BrickType type) =>
            type == BrickType.Plate || type == BrickType.Brick;

        public static bool IsDecoration(BrickType type) =>
            type == BrickType.PlateRound || type == BrickType.BrickRound;
    }
}
