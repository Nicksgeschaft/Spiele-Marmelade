using System.Collections.Generic;

namespace SpieleMarmelade.World
{
    // A deliberately blocky 3-wide x 5-tall pixel font, used by the Brick Text Generator to
    // build letters out of Wand-Baustein (Brick) bricks. Every glyph is exactly 3 columns wide
    // and 5 rows tall so letters line up on a consistent grid; diagonal-heavy letters
    // (K/M/N/R/W/X/Y/Z) are stylized approximations — that's normal for a font this small.
    // Row 0 is the TOP of the glyph, row 4 is the BOTTOM. '#' = brick, '.' = empty (background).
    public static class BrickFont
    {
        public const int GlyphWidth  = 3;
        public const int GlyphHeight = 5;

        // Narrower than a letter — just fills as background, no strokes.
        public const int SpaceWidth = 2;

        private static readonly Dictionary<char, string[]> Glyphs = new()
        {
            ['A'] = new[] { ".#.", "#.#", "###", "#.#", "#.#" },
            ['B'] = new[] { "##.", "#.#", "##.", "#.#", "##." },
            ['C'] = new[] { ".##", "#..", "#..", "#..", ".##" },
            ['D'] = new[] { "##.", "#.#", "#.#", "#.#", "##." },
            ['E'] = new[] { "###", "#..", "###", "#..", "###" },
            ['F'] = new[] { "###", "#..", "###", "#..", "#.." },
            ['G'] = new[] { ".##", "#..", "#.#", "#.#", ".##" },
            ['H'] = new[] { "#.#", "#.#", "###", "#.#", "#.#" },
            ['I'] = new[] { "###", ".#.", ".#.", ".#.", "###" },
            ['J'] = new[] { "..#", "..#", "..#", "#.#", ".#." },
            ['K'] = new[] { "#.#", "##.", "#..", "##.", "#.#" },
            ['L'] = new[] { "#..", "#..", "#..", "#..", "###" },
            ['M'] = new[] { "#.#", "###", "#.#", "#.#", "#.#" },
            ['N'] = new[] { "###", "#.#", "#.#", "#.#", "#.#" },
            ['O'] = new[] { ".#.", "#.#", "#.#", "#.#", ".#." },
            ['P'] = new[] { "##.", "#.#", "##.", "#..", "#.." },
            ['Q'] = new[] { ".#.", "#.#", "#.#", ".#.", "..#" },
            ['R'] = new[] { "##.", "#.#", "##.", "#.#", "#.#" },
            ['S'] = new[] { ".##", "#..", ".#.", "..#", "##." },
            ['T'] = new[] { "###", ".#.", ".#.", ".#.", ".#." },
            ['U'] = new[] { "#.#", "#.#", "#.#", "#.#", "###" },
            ['V'] = new[] { "#.#", "#.#", "#.#", ".#.", ".#." },
            ['W'] = new[] { "#.#", "#.#", "#.#", "###", "#.#" },
            ['X'] = new[] { "#.#", "#.#", ".#.", "#.#", "#.#" },
            ['Y'] = new[] { "#.#", "#.#", ".#.", ".#.", ".#." },
            ['Z'] = new[] { "###", "..#", ".#.", "#..", "###" },

            ['0'] = new[] { ".#.", "#.#", "#.#", "#.#", ".#." },
            ['1'] = new[] { ".#.", "##.", ".#.", ".#.", "###" },
            ['2'] = new[] { "##.", "..#", ".#.", "#..", "###" },
            ['3'] = new[] { "##.", "..#", ".#.", "..#", "##." },
            ['4'] = new[] { "#.#", "#.#", "###", "..#", "..#" },
            ['5'] = new[] { "###", "#..", "##.", "..#", "##." },
            ['6'] = new[] { ".##", "#..", "##.", "#.#", ".#." },
            ['7'] = new[] { "###", "..#", ".#.", ".#.", ".#." },
            ['8'] = new[] { ".#.", "#.#", ".#.", "#.#", ".#." },
            ['9'] = new[] { ".#.", "#.#", ".##", "..#", ".#." },

            ['!'] = new[] { ".#.", ".#.", ".#.", "...", ".#." },
            ['?'] = new[] { "##.", "..#", ".#.", "...", ".#." },
            ['.'] = new[] { "...", "...", "...", "...", ".#." },
            ['-'] = new[] { "...", "...", "###", "...", "..." },
        };

        // Returns null for space or unknown characters — caller decides what to do (space is
        // handled as a fixed-width empty gap by the generator, unknown chars are skipped).
        public static string[] Lookup(char uppercase) =>
            Glyphs.TryGetValue(uppercase, out var rows) ? rows : null;
    }
}
