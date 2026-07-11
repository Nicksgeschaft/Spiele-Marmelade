using SpieleMarmelade.Shared.World;
using UnityEngine;

namespace SpieleMarmelade.World
{
    // Runtime-safe core logic for building brick text (see BrickFont) — used by both
    // BrickTextGeneratorWindow (Editor tool, saves the result as a prefab) and runtime code that
    // needs to build text dynamically (e.g. the Hub's game-select list). Uses Object.Instantiate
    // rather than PrefabUtility, so it works identically in the Editor and in a built game — the
    // trade-off is that placed bricks are plain clones, not nested prefab instances of
    // Brick.prefab (fine for static signs; unavoidable for anything built at runtime).
    public static class BrickTextBuilder
    {
        public struct Result
        {
            public GameObject Root;
            public float Width;
            public float Height;
        }

        public static Result Build(GameObject brickPrefab, string text, Material letterMaterial,
            Material backgroundMaterial, string objectName = "BrickText")
        {
            var root = new GameObject(objectName);

            float colStep = WorldConstants.PlateWidth;
            float rowStep = BrickShapeInfo.HeightInPlates(BrickType.Brick) * WorldConstants.PlateHeight;

            int cursorCol      = 0;
            int highestColUsed = 0;

            foreach (char raw in text)
            {
                char c = char.ToUpperInvariant(raw);

                if (c == ' ')
                {
                    cursorCol += BrickFont.SpaceWidth + 1;
                    continue;
                }

                var glyph = BrickFont.Lookup(c);
                if (glyph == null)
                {
                    Debug.LogWarning($"[BrickTextBuilder] Kein Muster für '{c}' — übersprungen.");
                    continue;
                }

                for (int row = 0; row < BrickFont.GlyphHeight; row++)
                for (int col = 0; col < BrickFont.GlyphWidth; col++)
                {
                    bool isLetter = glyph[row][col] == '#';
                    var  mat      = isLetter ? letterMaterial : backgroundMaterial;

                    int gridCol = cursorCol + col;
                    int gridRow = BrickFont.GlyphHeight - 1 - row; // row 0 = glyph top → highest local Y

                    var go = Object.Instantiate(brickPrefab, root.transform, false);
                    go.transform.localPosition = new Vector3(gridCol * colStep, gridRow * rowStep, 0f);
                    foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                        mr.sharedMaterial = mat;

                    var marker = go.GetComponent<PlacedBrick>();
                    if (marker != null) marker.shape = BrickType.Brick;

                    highestColUsed = Mathf.Max(highestColUsed, gridCol);
                }

                cursorCol += BrickFont.GlyphWidth + 1;
            }

            return new Result
            {
                Root   = root,
                Width  = (highestColUsed + 1) * colStep,
                Height = BrickFont.GlyphHeight * rowStep,
            };
        }

        // Adds a BoxCollider + BrickTextButton sized to the built result, matching what
        // BrickTextGeneratorWindow's "Als Button nutzbar" checkbox does.
        public static void MakeClickable(Result result)
        {
            var box = result.Root.AddComponent<BoxCollider>();
            box.center = new Vector3(result.Width * 0.5f, result.Height * 0.5f, 0f);
            box.size   = new Vector3(result.Width, result.Height, WorldConstants.PlateWidth);
            result.Root.AddComponent<BrickTextButton>();
        }
    }
}
