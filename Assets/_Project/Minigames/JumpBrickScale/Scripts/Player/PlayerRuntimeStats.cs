namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Immutable runtime snapshot of the aggregated player stats (Docs/BrickMovementController_Anforderungen_v0.2.md
    // section 3). Rebuilt wholesale whenever the brick assembly changes - never mutated in place -
    // so the movement controller can hold a reference between rebuilds without it going stale silently.
    public class PlayerRuntimeStats
    {
        public float MoveSpeed { get; private set; }
        public float GroundAcceleration { get; private set; }
        public float GroundDeceleration { get; private set; }
        public float AirControl { get; private set; }
        public float JumpHeight { get; private set; }
        public float GravityMagnitude { get; private set; }
        public float FallGravityMultiplier { get; private set; }
        public float LowJumpGravityMultiplier { get; private set; }
        public float MaxFallSpeed { get; private set; }
        public float TotalWeight { get; private set; }
        public float MaxAngularVelocity { get; private set; }
        public float AngularDrag { get; private set; }
        public float CoyoteTime { get; private set; }
        public float JumpBufferTime { get; private set; }
        public float GroundNormalThreshold { get; private set; }
        public float AttachCooldown { get; private set; }

        // No brick modifiers applied - placeholder until the StatAggregator (implementation step 8)
        // starts rebuilding this from PlayerAssembly's connected bricks on every OnAssemblyChanged.
        public static PlayerRuntimeStats FromBaseStats(PlayerMovementStats stats) => new()
        {
            MoveSpeed = stats.moveSpeed.baseValue,
            GroundAcceleration = stats.groundAcceleration,
            GroundDeceleration = stats.groundDeceleration,
            AirControl = stats.airControl,
            JumpHeight = stats.jumpHeight.baseValue,
            GravityMagnitude = stats.gravityMagnitude.baseValue,
            FallGravityMultiplier = stats.fallGravityMultiplier,
            LowJumpGravityMultiplier = stats.lowJumpGravityMultiplier,
            MaxFallSpeed = stats.maxFallSpeed,
            TotalWeight = stats.baseWeight.baseValue,
            MaxAngularVelocity = stats.maxAngularVelocity,
            AngularDrag = stats.angularDrag,
            CoyoteTime = stats.coyoteTime,
            JumpBufferTime = stats.jumpBufferTime,
            GroundNormalThreshold = stats.groundNormalThreshold,
            AttachCooldown = stats.attachCooldown,
        };
    }
}
