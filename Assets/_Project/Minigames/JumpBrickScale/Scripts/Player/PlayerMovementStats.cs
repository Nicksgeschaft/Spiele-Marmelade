using System;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Base values, clamps and feel parameters for the brick assembly movement controller.
    // Defaults match Docs/BrickMovementController_Anforderungen_v0.2.md section 3.1.
    // The controller never reads this asset directly - it reads a PlayerRuntimeStats snapshot
    // that the (future) StatAggregator rebuilds from these base values plus brick modifiers.
    [CreateAssetMenu(fileName = "PlayerMovementStats_", menuName = "Spiele Marmelade/JumpBrickScale/Player Movement Stats")]
    public class PlayerMovementStats : ScriptableObject
    {
        [Serializable]
        public struct ClampedStat
        {
            public float baseValue;
            public float minValue;
            public float maxValue;

            public readonly float Clamp(float value) => Mathf.Clamp(value, minValue, maxValue);
        }

        [Header("Horizontal Movement")]
        public ClampedStat moveSpeed = new() { baseValue = 6.5f, minValue = 0f, maxValue = 20f };
        public float groundAcceleration = 45f;
        public float groundDeceleration = 60f;
        [Range(0f, 1f)] public float airControl = 0.60f;

        [Header("Jump")]
        public ClampedStat jumpHeight = new() { baseValue = 2.5f, minValue = 0f, maxValue = 10f };

        [Header("Gravity")]
        public ClampedStat gravityMagnitude = new() { baseValue = 28f, minValue = 1f, maxValue = 100f };
        public float fallGravityMultiplier = 1.55f;
        public float lowJumpGravityMultiplier = 2.0f;
        public float maxFallSpeed = 20f;

        [Header("Weight")]
        public ClampedStat baseWeight = new() { baseValue = 1.0f, minValue = 0.1f, maxValue = 1000f };
        public float weightPerStandardBrick = 1.0f;

        [Header("Rotation")]
        // Lowered from the documented 8 rad/s MVP-default: a jump on a single right-attached
        // brick (equal weight, one grid step over) works out to roughly 30 rad/s of raw angular
        // impulse before clamping - 8 rad/s still reads as a wild spin, 3 reads as a visible but
        // controlled lean. Re-tune after playtesting (Right-Heavy-Test, Docs section 12 T02).
        public float maxAngularVelocity = 3f;
        public float angularDrag = 1.4f;

        [Header("Jump Feel")]
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;

        [Header("Ground Detection")]
        [Range(0f, 1f)] public float groundNormalThreshold = 0.60f;

        [Header("Attachment")]
        public float attachCooldown = 0.10f;
    }
}
