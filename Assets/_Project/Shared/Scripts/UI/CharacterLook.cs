using UnityEngine;

namespace SpieleMarmelade.Shared.UI
{
    public enum CharacterPart
    {
        Head,
        Body,
        Feet,
    }

    // Shared source of truth for the player's chosen colours: the palette itself, where the choice is
    // stored, and how it gets painted onto a character. Both the Character Creator screen
    // (CharacterCustomizer) and the actual in-game player (CharacterAppearance) go through here, so
    // the preview and the real thing can't drift apart.
    //
    // Colours are stored, not material references - that way the in-game side needs no serialized
    // palette of its own and works from PlayerPrefs alone.
    public static class CharacterLook
    {
        // Bright, clearly distinguishable brick colours. Order is what the arrows cycle through.
        public static readonly Color[] Palette =
        {
            new(0.94f, 0.27f, 0.24f), // red
            new(0.98f, 0.58f, 0.16f), // orange
            new(0.99f, 0.85f, 0.25f), // yellow
            new(0.36f, 0.79f, 0.31f), // green
            new(0.25f, 0.80f, 0.75f), // teal
            new(0.27f, 0.55f, 0.93f), // blue
            new(0.58f, 0.36f, 0.85f), // purple
            new(0.95f, 0.45f, 0.72f), // pink
            new(0.60f, 0.40f, 0.25f), // brown
            new(0.96f, 0.96f, 0.96f), // white
            new(0.35f, 0.37f, 0.42f), // slate
            new(0.15f, 0.15f, 0.17f), // black
        };

        // Distinct starting colours so a player who never touches the arrows still gets a character
        // that reads as three separate parts.
        private static readonly int[] DefaultIndices = { 2, 5, 0 }; // head yellow, body blue, feet red

        // URP Lit uses _BaseColor; the older built-in path uses _Color. Both are set so this works
        // whatever shader the brick materials end up on.
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static string PrefKey(CharacterPart part) => $"Character.{part}";

        public static int GetIndex(CharacterPart part) =>
            Mathf.Clamp(PlayerPrefs.GetInt(PrefKey(part), DefaultIndices[(int)part]), 0, Palette.Length - 1);

        public static void SetIndex(CharacterPart part, int index)
        {
            PlayerPrefs.SetInt(PrefKey(part), Mathf.Clamp(index, 0, Palette.Length - 1));
            PlayerPrefs.Save();
        }

        public static Color GetColor(CharacterPart part) => Palette[GetIndex(part)];

        /// <summary>Steps an index forward/backward through the palette, wrapping at both ends.</summary>
        public static int Wrap(int index, int direction) =>
            ((index + direction) % Palette.Length + Palette.Length) % Palette.Length;

        /// <summary>Finds a character part by GameObject name (Head/Body/Feet), as laid out in
        /// Player_Platformer.prefab. Name-based because those parts are prefab instances of one shared
        /// plate prefab - there's nothing else that distinguishes them.</summary>
        public static Renderer FindPart(Transform root, CharacterPart part)
        {
            if (root == null) return null;

            string wanted = part.ToString();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == wanted) return child.GetComponentInChildren<Renderer>(true);
            }
            return null;
        }

        // A property block rather than an instanced material: no per-click material allocation, and
        // the shared brick materials stay untouched for everything else using them.
        public static void Paint(Renderer target, Color color)
        {
            if (target == null) return;

            var block = new MaterialPropertyBlock();
            target.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            target.SetPropertyBlock(block);
        }

        /// <summary>Paints every part of a character below <paramref name="root"/> with the saved colours.</summary>
        public static void ApplySaved(Transform root)
        {
            for (int i = 0; i < 3; i++)
            {
                var part = (CharacterPart)i;
                Paint(FindPart(root, part), GetColor(part));
            }
        }
    }
}
