using System;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // The collected-points display: a physical stack of round bricks that builds up in the corner of
    // the screen. Put this on a child of the gameplay camera so it rides along with the view and
    // reads as UI, even though it is real 3D geometry (same brick-as-UI approach the menus use).
    //
    // Slots fill column-major: bottom of the first column upward, then a new column beside it, which
    // is what makes a growing score read at a glance.
    public class PointStack : MonoBehaviour
    {
        // Collectables look this up the moment they're picked up rather than searching the scene, so
        // there is no per-frame FindObjectOfType in the gameplay loop.
        public static PointStack Instance { get; private set; }

        // Fallback until the first pickup reports its real size (used by the gizmo preview): one brick
        // at this project's x10 scale.
        private static readonly Vector3 DefaultBrickUnitSize = new(0.795f, 1.14f, 0.795f);

        [Header("Layout")]
        [Tooltip("How many bricks stack up before a new column starts to the right.")]
        [SerializeField] private int maxPerColumn = 8;

        [Tooltip("How deep a brick's studs sink into the brick above, at full brick size. The stacking " +
                 "step is the measured brick height minus this, so the pile interlocks like real bricks.")]
        [SerializeField] private float studOverlap = 0.18f;

        [Header("Appearance")]
        [Tooltip("Scale a collectable is resized to once it lands in the stack. Spacing follows the measured " +
                 "brick size automatically, so this is the only value to tune for how big the pile reads.")]
        [SerializeField] private float stackedScale = 0.3f;

        // Measured from the pickup itself rather than typed in, so the spacing can never drift out of
        // sync with the actual brick (which is exactly how every stacked brick ended up on one spot).
        private Vector3 _brickUnitSize = DefaultBrickUnitSize;

        public int Count { get; private set; }

        /// <summary>Raised whenever a point is added. Carries the new total.</summary>
        public event Action<int> OnCountChanged;

        public float StackedScale => stackedScale;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Claims the next free slot, bumps the counter and returns where that slot sits in
        /// this stack's local space. <paramref name="brickUnitSize"/> is the pickup's size at scale 1,
        /// which drives the spacing.</summary>
        public Vector3 ReserveSlot(Vector3 brickUnitSize)
        {
            if (brickUnitSize.x > 0f && brickUnitSize.y > 0f) _brickUnitSize = brickUnitSize;

            Vector3 slot = SlotLocalPosition(Count);
            Count++;
            OnCountChanged?.Invoke(Count);
            return slot;
        }

        public Vector3 SlotLocalPosition(int index)
        {
            int column = maxPerColumn > 0 ? index / maxPerColumn : 0;
            int row = maxPerColumn > 0 ? index % maxPerColumn : index;

            // Stacking step is a brick height minus the studs, so they interlock; columns step by a
            // full width so they sit flush. Both scale with the pile's display size.
            float stepY = (_brickUnitSize.y - studOverlap) * stackedScale;
            float stepX = _brickUnitSize.x * stackedScale;
            return new Vector3(column * stepX, row * stepY, 0f);
        }

        // Draws the first two columns of slots so the corner layout can be positioned in the Scene
        // view without entering play mode.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            int preview = Mathf.Max(maxPerColumn, 1) * 2;
            for (int i = 0; i < preview; i++)
            {
                Gizmos.DrawWireSphere(transform.TransformPoint(SlotLocalPosition(i)), 0.02f);
            }
        }
    }
}
