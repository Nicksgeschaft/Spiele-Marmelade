using System.Collections;
using UnityEngine;

namespace SpieleMarmelade.Shared.Combat
{
    // Procedurally swings a weapon transform through one of several randomly-picked arcs
    // (diagonal, overhead, horizontal) instead of the weapon just popping visible/invisible —
    // no Animator/AnimationClip rig needed. Each variant is a start/end rotation offset from the
    // blade's rest pose; the swing Slerps between them with an ease-out (fast strike, slightly
    // slower wind-up) over the same duration as the attack's hit window.
    public class SwordSwingAnimator : MonoBehaviour
    {
        [System.Serializable]
        public struct SwingVariant
        {
            [Tooltip("Rotation offset from the rest pose at the start of the swing (wind-up).")]
            public Vector3 startEuler;
            [Tooltip("Rotation offset from the rest pose at the end of the swing (follow-through).")]
            public Vector3 endEuler;
        }

        [Tooltip("Leer lassen, um dieses GameObject selbst zu drehen (z. B. wenn das Script direkt auf der Klinge sitzt).")]
        [SerializeField] private Transform blade;

        [SerializeField] private SwingVariant[] variants =
        {
            new() { startEuler = new Vector3(0, 0, 55),   endEuler = new Vector3(0, 0, -55) },  // oben-links -> unten-rechts
            new() { startEuler = new Vector3(0, 0, -55),  endEuler = new Vector3(0, 0, 55) },    // oben-rechts -> unten-links
            new() { startEuler = new Vector3(-45, 0, 0),  endEuler = new Vector3(35, 0, 0) },    // von oben (Überkopf-Hieb)
            new() { startEuler = new Vector3(0, -55, 0),  endEuler = new Vector3(0, 55, 0) },    // waagerecht rechts -> links
            new() { startEuler = new Vector3(0, 55, 0),   endEuler = new Vector3(0, -55, 0) },   // waagerecht links -> rechts
        };

        private Quaternion _restLocalRotation;
        private Coroutine _swingRoutine;

        private void Awake()
        {
            if (blade == null) blade = transform;
            _restLocalRotation = blade.localRotation;
        }

        // Called alongside MeleeHitbox.Activate() with the same duration, so the visible swing
        // and the damage window line up. Picks a random variant every time — no repeats tracked,
        // simple and good enough for "feels varied" rather than a strict non-repeat sequence.
        public void PlayRandomSwing(float duration)
        {
            if (blade == null || variants == null || variants.Length == 0) return;
            if (_swingRoutine != null) StopCoroutine(_swingRoutine);
            var variant = variants[Random.Range(0, variants.Length)];
            _swingRoutine = StartCoroutine(SwingRoutine(variant, duration));
        }

        private IEnumerator SwingRoutine(SwingVariant variant, float duration)
        {
            Quaternion start = _restLocalRotation * Quaternion.Euler(variant.startEuler);
            Quaternion end   = _restLocalRotation * Quaternion.Euler(variant.endEuler);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t); // ease-out: quick strike, slower wind-up
                blade.localRotation = Quaternion.Slerp(start, end, eased);
                yield return null;
            }

            blade.localRotation = _restLocalRotation;
            _swingRoutine = null;
        }
    }
}
