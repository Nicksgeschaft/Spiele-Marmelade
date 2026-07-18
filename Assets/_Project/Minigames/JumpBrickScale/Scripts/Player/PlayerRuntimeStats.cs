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

        // Ability powers aggregated from the attached bricks. 0 means the player hasn't picked up a
        // brick of that kind yet and the ability stays locked.
        public float AirJumpPower { get; set; }
        public float DashPower { get; set; }
        public float WallJumpPower { get; set; }

        /// <summary>Extra seconds each round-timer brick is worth, from purple bricks.</summary>
        public float TimerSecondsPerBrick { get; set; }

        // Resolved ability values, so the controller doesn't repeat the power maths every frame.
        public float AirJumpHeight { get; set; }
        public float DashSpeed { get; set; }
        public float DashDuration { get; set; }
        public float DashCooldown { get; set; }
        public float DoubleTapWindow { get; set; }
        public float WallJumpHeight { get; set; }
        public float WallJumpPush { get; set; }
        public float WallCheckDistance { get; set; }

        public bool CanAirJump => AirJumpPower >= 1f;
        public bool CanDash => DashPower >= 1f;
        public bool CanWallJump => WallJumpPower >= 1f;
    }
}
