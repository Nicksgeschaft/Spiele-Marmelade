using UnityEngine;

namespace SpieleMarmelade.Shared.UI
{
    // Drives the Character Creator screen's preview: the six arrow buttons call the Next*/Prev*
    // methods below, each of which steps that part one colour through the palette, repaints the
    // preview and saves the choice. CharacterAppearance then reads the same saved values onto the
    // real player when the game starts - see CharacterLook for the shared palette/storage.
    //
    // Built by MenuFlowGenerator for any MenuScreenKind.CharacterCreator screen; `preview` is the
    // Player_Platformer instance it spawns in the middle of that screen.
    public class CharacterCustomizer : MonoBehaviour
    {
        [Tooltip("Die Charakter-Vorschau (Player_Platformer-Instanz) mit Head/Body/Feet darunter.")]
        [SerializeField] private Transform preview;

        public void SetPreview(Transform value)
        {
            preview = value;
            ApplyAll();
        }

        private void Start() => ApplyAll();

        // Separate no-argument methods rather than one Change(part, direction): UnityEvents wired from
        // the generator carry no arguments, and six explicit entry points are far easier to hook up
        // (and to read in the Inspector) than persistent listeners with baked-in parameters.
        public void NextHead() => Step(CharacterPart.Head, 1);
        public void PrevHead() => Step(CharacterPart.Head, -1);
        public void NextBody() => Step(CharacterPart.Body, 1);
        public void PrevBody() => Step(CharacterPart.Body, -1);
        public void NextFeet() => Step(CharacterPart.Feet, 1);
        public void PrevFeet() => Step(CharacterPart.Feet, -1);

        private void Step(CharacterPart part, int direction)
        {
            CharacterLook.SetIndex(part, CharacterLook.Wrap(CharacterLook.GetIndex(part), direction));
            Apply(part);
        }

        private void ApplyAll()
        {
            if (preview == null) return;
            CharacterLook.ApplySaved(preview);
        }

        private void Apply(CharacterPart part)
        {
            if (preview == null) return;
            CharacterLook.Paint(CharacterLook.FindPart(preview, part), CharacterLook.GetColor(part));
        }
    }
}
