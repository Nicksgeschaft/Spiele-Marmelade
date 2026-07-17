using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Static data for one brick "kind": which color/look it has and how heavy it is.
    // Stat modifiers (the actual up/downgrades) are added once the StatAggregator exists
    // (implementation step 8) - see Docs/BrickMovementController_Anforderungen_v0.2.md section 5.1.
    [CreateAssetMenu(fileName = "BrickDefinition_", menuName = "Spiele Marmelade/JumpBrickScale/Brick Definition")]
    public class BrickDefinition : ScriptableObject
    {
        public BrickColor color = BrickColor.Green;
        public float weight = 1f;
        public Material material;
    }
}
