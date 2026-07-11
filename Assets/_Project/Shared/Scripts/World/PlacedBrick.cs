using UnityEngine;

namespace SpieleMarmelade.World
{
    // Marker component on every brick prefab (Plate, Brick, PlateRound, BrickRound).
    // Lets tools (Level Painter, world export) identify a placed brick's shape and
    // material reliably, instead of guessing from the GameObject name.
    [DisallowMultipleComponent]
    public class PlacedBrick : MonoBehaviour
    {
        public BrickType shape;
        public byte materialIndex;
    }
}
