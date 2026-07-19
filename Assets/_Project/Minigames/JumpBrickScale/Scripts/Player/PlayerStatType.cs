namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // What a brick can change about the player (Docs section 1.1). Weight is deliberately absent -
    // the physical brick.weight already drives mass and centre of mass via PlayerAssembly.
    // New entries may only be appended at the end, so existing BrickDefinition assets keep working.
    public enum PlayerStatType
    {
        MoveSpeed,
        JumpHeight,
        GravityMagnitude,

        // Acceleration toward the target run speed - how snappy the controls feel.
        GroundAcceleration,

        // 0 = no extra jump. Each point makes the mid-air jump higher (see PlayerMovementStats
        // airJumpHeightFactor / airJumpFactorPerPower).
        AirJumpPower,

        // 0 = no dash. Each point makes the dash faster/longer.
        DashPower,

        // 0 = no wall jump. Each point makes pushing off a wall stronger.
        WallJumpPower,
    }
}
