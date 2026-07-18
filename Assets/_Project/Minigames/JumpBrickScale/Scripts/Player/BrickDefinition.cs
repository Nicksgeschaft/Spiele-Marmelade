using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Static data for one brick "kind": which color/look it has, how heavy it is, and which
    // up/downgrades it grants while attached. See Docs/BrickMovementController_Anforderungen_v0.2.md
    // sections 3.2, 5.1 and 7.
    [CreateAssetMenu(fileName = "BrickDefinition_", menuName = "Spiele Marmelade/JumpBrickScale/Brick Definition")]
    public class BrickDefinition : ScriptableObject
    {
        [Serializable]
        public struct BrickStatModifier
        {
            public PlayerStatType stat;
            public StatModifierMode mode;
            public float value;
        }

        public BrickColor color = BrickColor.Green;
        public float weight = 1f;
        public Material material;

        [Tooltip("Additive values add before multiplicative ones are applied (Docs section 3.2). " +
                 "Downgrades use negative additive values or a multiplicative value below 1.")]
        public List<BrickStatModifier> statModifiers = new();
    }
}
