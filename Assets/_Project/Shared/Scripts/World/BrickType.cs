namespace SpieleMarmelade.World
{
    public enum BrickType : byte
    {
        None       = 0,
        Plate      = 1, // flat, square — structural, height = 1 plate unit (0.032 world units)
        Brick      = 2, // tall, square — structural, height = 3 plate units (0.096 world units)
        PlateRound = 3, // flat, round — decoration only, never voxelized
        BrickRound = 4, // tall, round — decoration only, never voxelized
    }
}
