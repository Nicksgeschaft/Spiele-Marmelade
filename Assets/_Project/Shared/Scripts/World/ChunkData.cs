using System;
using UnityEngine;

namespace SpieleMarmelade.World
{
    [Serializable]
    public class ChunkData
    {
        public const int SizeX = 16;
        public const int SizeY = 64; // plate units — ~2.05m
        public const int SizeZ = 16;

        // Flat array for serialization compatibility
        [SerializeField] private BrickCell[] _cells = new BrickCell[SizeX * SizeY * SizeZ];

        public BrickCell Get(int x, int y, int z)
        {
            if (!InBounds(x, y, z)) return BrickCell.Empty;
            return _cells[Index(x, y, z)];
        }

        public void Set(int x, int y, int z, BrickCell cell)
        {
            if (!InBounds(x, y, z)) return;
            _cells[Index(x, y, z)] = cell;
        }

        public bool IsEmpty(int x, int y, int z)
        {
            if (!InBounds(x, y, z)) return true;
            return _cells[Index(x, y, z)].IsEmpty;
        }

        public static bool InBounds(int x, int y, int z) =>
            x >= 0 && x < SizeX && y >= 0 && y < SizeY && z >= 0 && z < SizeZ;

        // Convert chunk-local plate-unit position to world position — X and Z use separate
        // constants because the brick footprint isn't a perfect square (see WorldConstants).
        public static Vector3 LocalToWorld(Vector3Int chunkCoord, int lx, int ly, int lz)
        {
            const float pw = WorldConstants.PlateWidth;
            const float pd = WorldConstants.PlateDepth;
            const float ph = WorldConstants.PlateHeight;
            return new Vector3(
                (chunkCoord.x * SizeX + lx) * pw,
                ly * ph,
                (chunkCoord.z * SizeZ + lz) * pd);
        }

        // Convert world position to chunk coord + local plate position
        public static (Vector3Int chunk, Vector3Int local) WorldToChunkLocal(Vector3 worldPos)
        {
            const float pw = WorldConstants.PlateWidth;
            const float pd = WorldConstants.PlateDepth;
            const float ph = WorldConstants.PlateHeight;
            int gx = Mathf.FloorToInt(worldPos.x / pw);
            int gy = Mathf.FloorToInt(worldPos.y / ph);
            int gz = Mathf.FloorToInt(worldPos.z / pd);
            int cx = Mathf.FloorToInt((float)gx / SizeX);
            int cz = Mathf.FloorToInt((float)gz / SizeZ);
            return (new Vector3Int(cx, 0, cz),
                    new Vector3Int(gx - cx * SizeX, gy, gz - cz * SizeZ));
        }

        private static int Index(int x, int y, int z) => x + SizeX * (y + SizeY * z);
    }
}
