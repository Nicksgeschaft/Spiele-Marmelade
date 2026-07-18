namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Collectable brick colors. New colors may only be appended at the end, to keep
    // BrickDefinition asset references stable (same convention as MaterialCategory).
    public enum BrickColor
    {
        None,
        Green,      // move speed
        Red,        // acceleration
        Blue,       // gravity
        Yellow,
        Purple,     // extra round time
        LightBlue,  // double jump
        Orange,     // dash
        Brown,      // wall jump
    }
}
