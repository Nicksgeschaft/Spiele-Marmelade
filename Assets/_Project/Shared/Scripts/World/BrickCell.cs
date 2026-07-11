using System;

namespace SpieleMarmelade.World
{
    [Serializable]
    public struct BrickCell
    {
        public BrickType type;
        public byte      materialIndex;

        public bool IsEmpty => type == BrickType.None;

        // Height in plate units this brick occupies vertically
        public int HeightInPlates => BrickShapeInfo.HeightInPlates(type);

        public static readonly BrickCell Empty = new() { type = BrickType.None };

        public BrickCell(BrickType type, byte materialIndex = 0)
        {
            this.type          = type;
            this.materialIndex = materialIndex;
        }
    }
}
