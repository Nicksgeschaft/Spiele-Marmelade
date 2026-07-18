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

        [Tooltip("Recolour this brick's renderers from its BrickDefinition on start. Lets every collectable " +
                 "brick type share one prefab and differ only by its definition. Turn off for bricks whose " +
                 "look is authored by hand.")]
        [SerializeField] private bool applyDefinitionMaterial = true;

        private void Awake()
        {
            // Never repaint the Main-Brick: the player's visual is hand-built (three separate plates),
            // and recolouring it from the definition turned the whole character one flat colour.
            if (isMainBrick || !applyDefinitionMaterial) return;

            ApplyDefinitionMaterial();
        }

        private void ApplyDefinitionMaterial()
        {
            if (definition == null || definition.material == null) return;

            foreach (Renderer brickRenderer in GetComponentsInChildren<Renderer>(true))
            {
                brickRenderer.sharedMaterial = definition.material;
            }
        }

        internal void AssignId(int id) => Id = id;

        internal void SetNeighbor(CardinalDirection direction, BrickNode neighbor) => _neighbors[direction] = neighbor;

        internal void ClearNeighbor(CardinalDirection direction) => _neighbors.Remove(direction);

        internal void ClearAllNeighbors() => _neighbors.Clear();
    }
}
