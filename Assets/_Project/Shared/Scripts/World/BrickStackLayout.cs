using UnityEngine;

namespace SpieleMarmelade.Shared.World
{
    // A minimal 3D-world equivalent of uGUI's Horizontal/VerticalLayoutGroup — those only work on
    // RectTransforms inside a Canvas, so they can't arrange brick objects built directly in world
    // space (icons, sliders, brick text — see BrickTextBuilder, IconPainterWindow,
    // BrickSliderTrack). Parent brick objects under a GameObject with this component, then call
    // Arrange() once after all children exist. This is a one-shot layout pass for procedurally
    // generated content (Editor tools), not a live-updating layout — there's no per-frame cost.
    //
    // Each child is measured by its own combined renderer bounds (whatever it actually contains,
    // regardless of its internal pivot convention) and placed one after another along `axis`. By
    // default children are also re-centered on the stack's own origin along the cross axis (e.g.
    // a short slider ends up vertically centered on a taller icon next to it in a Horizontal
    // stack) — set centerCrossAxis to false to leave each child's already-set cross-axis position
    // alone (used when the caller wants to control that axis itself, e.g. a column of rows that
    // should all share the same left edge rather than each being individually re-centered).
    public class BrickStackLayout : MonoBehaviour
    {
        public enum Axis { Horizontal, Vertical }

        [SerializeField] private Axis axis = Axis.Horizontal;
        [SerializeField] private float spacing = 0.1f;
        [SerializeField] private bool centerCrossAxis = true;

        public void Configure(Axis newAxis, float newSpacing, bool centerCrossAxis = true)
        {
            axis = newAxis;
            spacing = newSpacing;
            this.centerCrossAxis = centerCrossAxis;
        }

        // Editor-only "live" feedback — lets a designer drag spacing/axis/centering in the
        // Inspector and immediately see children reflow, the way a real uGUI LayoutGroup would,
        // instead of only ever seeing the effect of the one bake-time Arrange() call that built
        // the hierarchy in the first place.
        private void OnValidate() => Arrange();

        public void Arrange()
        {
            int count = transform.childCount;
            float cursor = 0f;

            for (int i = 0; i < count; i++)
            {
                var child = transform.GetChild(i);
                if (!TryGetLocalBounds(child, out var bounds)) continue;

                // The gap BEFORE this child defaults to the stack's own spacing, but a child can
                // ask for a tighter (or looser) one via BrickStackLayoutSpacing — e.g. the
                // Options screen's Fullscreen row sitting close under SFX instead of using the
                // same wide gap as the rest of the stack.
                if (i > 0)
                {
                    var spacingOverride = child.GetComponent<BrickStackLayoutSpacing>();
                    float gapBefore = spacingOverride != null ? spacingOverride.spacingBefore : spacing;
                    cursor += axis == Axis.Horizontal ? gapBefore : -gapBefore;
                }

                float mainSize = axis == Axis.Horizontal ? bounds.size.x : bounds.size.y;
                // Horizontal grows rightward (increasing X), Vertical grows downward (decreasing
                // Y, since +Y is up on the stage) — an earlier version advanced `cursor` in the
                // same direction for both axes, which stacked Vertical children bottom-to-top
                // instead of top-to-bottom (later children ended up ABOVE earlier ones).
                float mainDelta = axis == Axis.Horizontal
                    ? cursor - bounds.min.x
                    : cursor - bounds.max.y;

                Vector3 delta = axis == Axis.Horizontal
                    ? new Vector3(mainDelta, centerCrossAxis ? -bounds.center.y : 0f, 0f)
                    : new Vector3(centerCrossAxis ? -bounds.center.x : 0f, mainDelta, 0f);
                child.localPosition += delta;

                cursor += axis == Axis.Horizontal ? mainSize : -mainSize;
            }
        }

        // Combined renderer bounds of `child` (and its descendants), expressed in THIS stack's
        // own local space rather than the child's — lets Arrange() reason in one consistent
        // space even though icons/sliders/text each have very different internal pivots.
        private bool TryGetLocalBounds(Transform child, out Bounds localBounds)
        {
            var renderers = child.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                localBounds = default;
                return false;
            }

            Bounds worldBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                worldBounds.Encapsulate(renderers[i].bounds);

            Vector3 a = transform.InverseTransformPoint(worldBounds.min);
            Vector3 b = transform.InverseTransformPoint(worldBounds.max);
            localBounds = new Bounds();
            localBounds.SetMinMax(Vector3.Min(a, b), Vector3.Max(a, b));
            return true;
        }
    }

    // Attach to a direct child of a BrickStackLayout to override the gap between it and the
    // PREVIOUS child, instead of using the stack's default spacing — for one specific child that
    // needs to sit noticeably closer (or farther) than the rest of an otherwise uniform stack.
    public class BrickStackLayoutSpacing : MonoBehaviour
    {
        public float spacingBefore;
    }
}
