namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    public enum DetachReason
    {
        // Explicitly requested (debug tooling, a future ability, etc).
        Manual,
        // Swept up as a fragment because it lost its connection to the Main-Brick
        // when a different brick was detached (Docs section 6).
        Collapse,
    }
}
