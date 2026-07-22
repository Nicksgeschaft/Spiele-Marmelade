using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
    public static class BrickUtils
    {
        // the 4x2 brick has an extent in blender between +/- 0.159 along the X axis (4 studs wide)
        private const float UnityStudSizeX = 0.159f * 0.5f;
        // the 4x2 brick has an extent in blender between +/- 0.079 along the Y axis (2 studs tall)
        private const float UnityStudSizeY = 0.079f;
        
        public static readonly Vector2 UnityStudSizeOnlyX = new Vector2(UnityStudSizeX, 0.0f);
        public static readonly Vector2 UnityStudSizeOnlyY = new Vector2(0.0f, UnityStudSizeY);
        public static readonly Vector2 UnityStudSize = new Vector2(UnityStudSizeX, UnityStudSizeY);
    }
}
