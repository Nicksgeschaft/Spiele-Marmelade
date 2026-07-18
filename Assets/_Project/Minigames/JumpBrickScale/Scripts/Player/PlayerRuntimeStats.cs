namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Runtime snapshot of the aggregated player stats (Docs/BrickMovementController_Anforderungen_v0.2.md
    // section 3), rebuilt wholesale by StatAggregator on every assembly change. Treat instances as
    // read-only from outside StatAggregator - a new one replaces the old rather than editing it in
    // place, so a consumer can hold a reference between rebuilds without it going stale silently.
    public class PlayerRuntimeStats
    {
        public float MoveSpeed { get; set; }
        public float GroundAcceleration { get; set; }
        public float GroundDeceleration { get; set; }
        public float AirControl { get; set; }
        public float JumpHeight { get; set; }
        public float GravityMagnitude { get; set; }
        public float FallGravityMultiplier { get; set; }
        public float LowJumpGravityMultiplier { get; set; }
        public float MaxFallSpeed { get; set; }
        public float TotalWeight { get; set; }
        public float MaxAngularVelocity { get; set; }
        public float AngularDrag { get; set; }
        public float CoyoteTime { get; set; }
        public float JumpBufferTime { get; set; }
        public float GroundNormalThreshold { get; set; }
        public float AttachCooldown { get; set; }
    }
}
