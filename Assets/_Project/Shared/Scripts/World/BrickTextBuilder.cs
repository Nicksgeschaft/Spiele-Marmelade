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
            Material backgroundMaterial, string objectName = "BrickText", bool includeBackground = true) =>
            Build(brickPrefab, text, new[] { letterMaterial }, backgroundMaterial, objectName, includeBackground);

        // Cycles through letterMaterials one per character (e.g. a rainbow effect across a
        // title) — a single-entry array behaves exactly like the single-material overload above.
        public static Result Build(GameObject brickPrefab, string text, Material[] letterMaterials,
            Material backgroundMaterial, string objectName = "BrickText", bool includeBackground = true)
        {
            var root = new GameObject(objectName);

            float colStep = WorldConstants.PlateWidth;
            float rowStep = BrickShapeInfo.HeightInPlates(BrickType.Brick) * WorldConstants.PlateHeight;

            int cursorCol      = 0;
            int highestColUsed = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = char.ToUpperInvariant(text[i]);
                bool hasNext = i < text.Length - 1;

                if (c == ' ')
                {
                    if (includeBackground)
                        for (int col = 0; col < BrickFont.SpaceWidth; col++)
                            PlaceBackgroundColumn(root.transform, brickPrefab, backgroundMaterial, cursorCol + col, rowStep, colStep);
                    cursorCol += BrickFont.SpaceWidth + 1;
                    continue;
                }

                var glyph = BrickFont.Lookup(c);
                if (glyph == null)
                {
                    Debug.LogWarning($"[BrickTextBuilder] Kein Muster für '{c}' — übersprungen.");
                    continue;
                }

                Material letterMaterial = letterMaterials != null && letterMaterials.Length > 0
                    ? letterMaterials[i % letterMaterials.Length]
                    : null;

                for (int row = 0; row < BrickFont.GlyphHeight; row++)
                for (int col = 0; col < BrickFont.GlyphWidth; col++)
                {
                    bool isLetter = glyph[row][col] == '#';
                    if (!isLetter && !includeBackground) continue;
                    var  mat      = isLetter ? letterMaterial : backgroundMaterial;

                    int gridCol = cursorCol + col;
                    int gridRow = BrickFont.GlyphHeight - 1 - row; // row 0 = glyph top → highest local Y

                    var go = Object.Instantiate(brickPrefab, root.transform, false);
                    go.transform.localPosition = new Vector3(gridCol * colStep, gridRow * rowStep, 0f);
                    foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                        mr.sharedMaterial = mat;

                    var marker = go.GetComponent<PlacedBrick>();
                    if (marker != null) marker.shape = BrickType.Brick;
                }

                // Nominal glyph width, independent of whether every cell got an actual brick —
                // Width/MakeClickable's collider should still cover the full letter slot even
                // with background bricks turned off (sparse letters shouldn't shrink the button).
                highestColUsed = Mathf.Max(highestColUsed, cursorCol + BrickFont.GlyphWidth - 1);

                // The single-column gap between this letter and the next one was previously left
                // completely empty even with includeBackground on — only each glyph's own 3-wide
                // grid got background fill, so a background-enabled word still showed a visible
                // gap between every pair of letters instead of reading as one solid plate.
                if (includeBackground && hasNext)
                {
                    int gapCol = cursorCol + BrickFont.GlyphWidth;
                    PlaceBackgroundColumn(root.transform, brickPrefab, backgroundMaterial, gapCol, rowStep, colStep);
                    highestColUsed = Mathf.Max(highestColUsed, gapCol);
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

        // Fills one full-height column with background bricks — used for the 1-column gap
        // between letters (and the gap left by a space) so a background-enabled word reads as
        // one solid plate instead of separate letter tiles with visible gaps between them.
        private static void PlaceBackgroundColumn(Transform parent, GameObject brickPrefab,
            Material backgroundMaterial, int col, float rowStep, float colStep)
        {
            for (int row = 0; row < BrickFont.GlyphHeight; row++)
            {
                var go = Object.Instantiate(brickPrefab, parent, false);
                go.transform.localPosition = new Vector3(col * colStep, row * rowStep, 0f);
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                    mr.sharedMaterial = backgroundMaterial;

                var marker = go.GetComponent<PlacedBrick>();
                if (marker != null) marker.shape = BrickType.Brick;
            }
        }

        // Adds a BoxCollider + BrickTextButton sized to the built result, matching what
        // BrickTextGeneratorWindow's "Als Button nutzbar" checkbox does.
        public static void MakeClickable(Result result)
        {
            var box = result.Root.AddComponent<BoxCollider>();
            box.center = new Vector3(result.Width * 0.5f, result.Height * 0.5f, 0f);
            box.size   = new Vector3(result.Width, result.Height, WorldConstants.PlateDepth);
            result.Root.AddComponent<BrickTextButton>();
        }
    }
}
