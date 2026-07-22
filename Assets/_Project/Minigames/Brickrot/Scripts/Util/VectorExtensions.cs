using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot
{
    // Both halves of Brick Survivor play on the XZ ground plane, so grid/gameplay maths is done in
    // Vector2 and lifted into world space through XZ(). Lives in the minigame's ROOT namespace on
    // purpose: C# resolves extension methods from enclosing namespaces too, so everything under
    // .Tetris and .Survivor picks these up without needing an extra using.
    public static class VectorExtensions
    {
        public static readonly Vector2 Half = Vector2.one * 0.5f;

        public static Vector3 XZ(this Vector2 vector) => new(vector.x, 0f, vector.y);

        public static Vector2 XZ(this Vector3 vector) => new(vector.x, vector.z);

        public static Vector2 Reciprocal(this Vector2Int vector) => new(1.0f / vector.x, 1.0f / vector.y);

        public static Vector2 Reciprocal(this Vector2 vector) => new(1.0f / vector.x, 1.0f / vector.y);

        public static Vector2 InverseScale(this Vector2 vector, Vector2 scale) =>
            new(vector.x / scale.x, vector.y / scale.y);

        public static Vector2Int RoundToInt(this Vector2 vector) =>
            new(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y));

        // x is rounded rather than floored/ceiled in both of these because a piece is always snapped
        // to a whole column — only the fall axis (y) needs directional rounding.
        public static Vector2Int FloorToInt(this Vector2 vector) =>
            new(Mathf.RoundToInt(vector.x), Mathf.FloorToInt(vector.y));

        public static Vector2Int CeilToInt(this Vector2 vector) =>
            new(Mathf.RoundToInt(vector.x), Mathf.CeilToInt(vector.y));
    }
}
