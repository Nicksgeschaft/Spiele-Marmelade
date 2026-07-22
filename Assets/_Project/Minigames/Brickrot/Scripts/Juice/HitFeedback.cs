using System.Collections;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot
{
    // "I hit that" feedback: a brief colour flash plus a scale punch. Drop it on anything that
    // takes damage and call Play() — it finds its own renderers, so no Inspector wiring is needed.
    //
    // The flash goes through a MaterialPropertyBlock rather than touching materials, for two
    // reasons: no per-hit material allocation, and the shared brick materials stay untouched for
    // everything else using them (BrickShatterEffect samples sharedMaterial for its fragment
    // colour, so mutating materials here would make corpses flash white).
    [DisallowMultipleComponent]
    public class HitFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Flash")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.07f;

        [Header("Punch")]
        [Tooltip("Wie stark der Gegner beim Treffer aufploppt (0.25 = 25% größer).")]
        [SerializeField] private float punchScale = 0.28f;
        [SerializeField] private float punchDuration = 0.13f;

        private Renderer[] _renderers;
        private Color[] _originalColors;
        private bool _coloursCaptured;

        private Vector3 _baseScale;
        private Coroutine _flashRoutine;
        private Coroutine _punchRoutine;

        private void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _originalColors = new Color[_renderers.Length];
            _baseScale = transform.localScale;
        }

        public void Play()
        {
            if (!isActiveAndEnabled) return;

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            if (_punchRoutine != null) StopCoroutine(_punchRoutine);

            _flashRoutine = StartCoroutine(Flash());
            _punchRoutine = StartCoroutine(Punch());
        }

        // Captured on first use rather than in Awake: CharacterAppearance paints its colours in
        // Start, so anything cached in Awake would be the pre-paint colour and the first hit
        // would "restore" the character to the wrong colour.
        private void CaptureColours()
        {
            if (_coloursCaptured) return;
            _coloursCaptured = true;

            var block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) { _originalColors[i] = Color.white; continue; }

                r.GetPropertyBlock(block);
                // An untouched block reports black; fall back to the material in that case.
                Color c = block.GetColor(BaseColorId);
                if (c == default)
                {
                    var mat = r.sharedMaterial;
                    c = mat == null ? Color.white
                      : mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId)
                      : mat.color;
                }
                _originalColors[i] = c;
            }
        }

        private IEnumerator Flash()
        {
            CaptureColours();

            SetColour(flashColor, useOriginal: false);
            yield return new WaitForSeconds(flashDuration);
            SetColour(default, useOriginal: true);

            _flashRoutine = null;
        }

        private void SetColour(Color colour, bool useOriginal)
        {
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;

                Color c = useOriginal ? _originalColors[i] : colour;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColorId, c);
                block.SetColor(ColorId, c);
                r.SetPropertyBlock(block);
            }
        }

        // Snap out, ease back: the pop should be instant and the recovery readable.
        private IEnumerator Punch()
        {
            transform.localScale = _baseScale * (1f + punchScale);

            float t = 0f;
            while (t < punchDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / punchDuration);
                transform.localScale = Vector3.Lerp(_baseScale * (1f + punchScale), _baseScale, k * k);
                yield return null;
            }

            transform.localScale = _baseScale;
            _punchRoutine = null;
        }
    }
}
