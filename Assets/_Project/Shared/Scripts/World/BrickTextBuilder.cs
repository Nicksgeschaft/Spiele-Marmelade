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
        // How a multi-material palette is spread across the text.
        public enum ColorMode
        {
            // Strict round-robin: letter 0 takes colour 0, letter 1 colour 1, and so on. Predictable,
            // but with a short palette every word ends up with the identical colour sequence.
            Cycle,
            // One random colour per letter, so the same palette reads differently on every sign.
            RandomPerLetter,
            // A random colour per individual brick — a confetti look, meant for a big logo rather
            // than body-sized text where it would make letters hard to read.
            RandomPerBrick,
        }

        public struct Result
        {
            public GameObject Root;
            public float Width;
            public float Height;
        }

        public static Result Build(GameObject brickPrefab, string text, Material letterMaterial,
            Material backgroundMaterial, string objectName = "BrickText", bool includeBackground = true) =>
            Build(brickPrefab, text, new[] { letterMaterial }, backgroundMaterial, objectName, includeBackground);

        // Spreads letterMaterials across the text according to `mode` — a single-entry array always
        // behaves like the single-material overload above, whatever the mode.
        public static Result Build(GameObject brickPrefab, string text, Material[] letterMaterials,
            Material backgroundMaterial, string objectName = "BrickText", bool includeBackground = true,
            ColorMode mode = ColorMode.Cycle)
        {
            var root = new GameObject(objectName);

            float colStep = WorldConstants.PlateWidth;
            float rowStep = BrickShapeInfo.HeightInPlates(BrickType.Brick) * WorldConstants.PlateHeight;

            int cursorCol      = 0;
            int highestColUsed = 0;
            // Which palette entry the previous letter (or brick, in RandomPerBrick) used, so the next
            // pick can rule it out - random alone happily produces "yellow yellow blue", which reads
            // as one wide blob instead of two letters.
            int lastColorIndex = -1;

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

                lastColorIndex = PickColorIndex(letterMaterials, mode, i, lastColorIndex);
                Material letterMaterial = MaterialAt(letterMaterials, lastColorIndex);

                for (int row = 0; row < BrickFont.GlyphHeight; row++)
                for (int col = 0; col < BrickFont.GlyphWidth; col++)
                {
                    bool isLetter = glyph[row][col] == '#';
                    if (!isLetter && !includeBackground) continue;
                    // RandomPerBrick re-rolls for every brick; the other modes reuse the one colour
                    // picked for this letter above.
                    if (isLetter && mode == ColorMode.RandomPerBrick)
                    {
                        lastColorIndex = PickColorIndex(letterMaterials, mode, i, lastColorIndex);
                        letterMaterial = MaterialAt(letterMaterials, lastColorIndex);
                    }
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

        private static Material MaterialAt(Material[] letterMaterials, int index) =>
            letterMaterials != null && index >= 0 && index < letterMaterials.Length ? letterMaterials[index] : null;

        // Returns the palette index to use next. The random modes draw from every entry EXCEPT
        // `excludedIndex` (the one just used), so neighbours are always visibly different - rolling
        // freely and retrying would be both slower and still able to repeat.
        private static int PickColorIndex(Material[] letterMaterials, ColorMode mode, int letterIndex, int excludedIndex)
        {
            if (letterMaterials == null || letterMaterials.Length == 0) return -1;

            int count = letterMaterials.Length;
            if (mode == ColorMode.Cycle) return letterIndex % count;

            // With a single colour there's nothing to vary, and with none excluded any entry is fair game.
            if (count == 1) return 0;
            if (excludedIndex < 0 || excludedIndex >= count) return Random.Range(0, count);

            // Draw from the count-1 remaining entries, then shift past the excluded one - this stays
            // uniform across all allowed colours.
            int roll = Random.Range(0, count - 1);
            return roll >= excludedIndex ? roll + 1 : roll;
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
