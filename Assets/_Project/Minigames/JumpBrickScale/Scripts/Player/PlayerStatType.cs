namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // The subset of PlayerMovementStats that bricks can modify (Docs section 1.1: "Move Speed,
    // Jump Height, Gravity und Weight/Mass sind ... zur Laufzeit aggregierbar"). Weight is handled
    // separately via BrickNode.Weight/PlayerAssembly mass-and-center-of-mass, not through this
    // enum (Docs section 3.2). New entries may only be appended at the end.
    public enum PlayerStatType
    {
        MoveSpeed,
        JumpHeight,
        GravityMagnitude,
    }
}
