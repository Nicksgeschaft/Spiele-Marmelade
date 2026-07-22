using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot
{
    // Trauma-based camera shake. Callers add trauma (0..1); the actual displacement is trauma
    // SQUARED, which is what makes a small hit barely register while a big one really kicks —
    // linear trauma reads as constant jitter instead.
    //
    // This only computes an offset; it never writes the transform itself. SurvivorCamera adds
    // CurrentOffset on top of its follow position, so the two can't fight over the transform
    // regardless of script execution order.
    [DisallowMultipleComponent]
    public class CameraShake : MonoBehaviour
    {
        /// <summary>Set by whichever CameraShake woke up last — there's only ever one camera here.</summary>
        public static CameraShake Instance { get; private set; }

        [Header("Strength")]
        [Tooltip("Maximaler Ausschlag in Weltunits bei voller Trauma.")]
        [SerializeField] private float maxOffset = 0.35f;

        [Tooltip("Maximale Kamera-Neigung in Grad bei voller Trauma.")]
        [SerializeField] private float maxRoll = 1.5f;

        [Header("Feel")]
        [Tooltip("Wie schnell sich die Kamera wieder beruhigt (Trauma pro Sekunde).")]
        [SerializeField] private float decayPerSecond = 1.8f;

        [Tooltip("Wie nervös das Wackeln ist.")]
        [SerializeField] private float frequency = 26f;

        private float _trauma;
        private float _seed;

        /// <summary>Positional shake offset for this frame. Zero when calm.</summary>
        public Vector3 CurrentOffset { get; private set; }

        /// <summary>Roll (Z rotation) in degrees for this frame. Zero when calm.</summary>
        public float CurrentRoll { get; private set; }

        private void Awake()
        {
            Instance = this;
            _seed = Random.value * 1000f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Adds trauma. Safe to call when no camera exists — it simply does nothing.</summary>
        public static void Add(float amount)
        {
            if (Instance != null) Instance.AddTrauma(amount);
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + Mathf.Max(0f, amount));
        }

        private void LateUpdate()
        {
            if (_trauma <= 0f)
            {
                CurrentOffset = Vector3.zero;
                CurrentRoll = 0f;
                return;
            }

            // Squared trauma: the difference between a chip hit and an explosion should be felt,
            // not just measured.
            float shake = _trauma * _trauma;
            float t = Time.unscaledTime * frequency;

            // Perlin rather than Random: continuous, so the camera swings instead of buzzing.
            // Offset seeds keep the three axes from moving in lockstep.
            float x = Mathf.PerlinNoise(_seed, t) * 2f - 1f;
            float z = Mathf.PerlinNoise(_seed + 37f, t) * 2f - 1f;
            float r = Mathf.PerlinNoise(_seed + 73f, t) * 2f - 1f;

            // Shake across the ground plane only — moving the camera's height reads as a zoom
            // wobble and makes the top-down view feel seasick.
            CurrentOffset = new Vector3(x, 0f, z) * (shake * maxOffset);
            CurrentRoll = r * shake * maxRoll;

            // Unscaled so a hit-stop freeze doesn't also freeze the shake mid-swing.
            _trauma = Mathf.Max(0f, _trauma - decayPerSecond * Time.unscaledDeltaTime);
        }
    }
}
