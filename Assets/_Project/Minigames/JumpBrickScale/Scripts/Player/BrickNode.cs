using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Per-brick node: one BrickNode sits on every brick that's part of a PlayerAssembly, the
    // Main-Brick included. PlayerAssembly owns all mutation of Id, GridPosition and neighbors -
    // this component just carries that state. See Docs/BrickMovementController_Anforderungen_v0.2.md
    // section 5.1.
    public class BrickNode : MonoBehaviour
    {
        [SerializeField] private BrickDefinition definition;
        [SerializeField] private bool isMainBrick;

        private readonly Dictionary<CardinalDirection, BrickNode> _neighbors = new();

        public int Id { get; private set; }
        public bool IsMainBrick => isMainBrick;
        public BrickDefinition Definition => definition;
        public BrickColor Color => definition != null ? definition.color : BrickColor.None;
        public float Weight => definition != null ? definition.weight : 1f;
        public Vector2Int GridPosition { get; internal set; }
        public IReadOnlyDictionary<CardinalDirection, BrickNode> Neighbors => _neighbors;

        internal void AssignId(int id) => Id = id;

        internal void SetNeighbor(CardinalDirection direction, BrickNode neighbor) => _neighbors[direction] = neighbor;
    }
}
