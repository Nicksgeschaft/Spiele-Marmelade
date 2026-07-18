using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Free-floating collectible brick in the level. On touching any brick of a PlayerAssembly,
    // determines the cardinal side that was hit and asks that assembly to attach itself there.
    // See Docs/BrickMovementController_Anforderungen_v0.2.md sections 5.2-5.4.
    // No [RequireComponent(typeof(Rigidbody))]: PlayerAssembly.Attach() destroys the Rigidbody so
    // the brick's collider can merge into the player's compound collider, and RequireComponent
    // would block that removal.
    [RequireComponent(typeof(BrickNode))]
    public class WorldBrick : MonoBehaviour
    {
        [Tooltip("When on, an attach is rejected if the target cell is blocked by level geometry (Docs 5.4). " +
                 "Off by default so attaching just works - enable once your level colliders are tuned.")]
        [SerializeField] private bool checkTargetClearOfGeometry;

        [Tooltip("Layers checked for the pre-snap overlap safety check. PlayerAssembly and WorldBrick are excluded automatically.")]
        [SerializeField] private LayerMask levelGeometryMask = ~0;

        [Tooltip("Blocks re-attaching for this long after being detached, so a freshly-dropped fragment doesn't immediately re-stick.")]
        [SerializeField] private float detachGraceTime = 0.25f;

        [Tooltip("Corner rejection: if the weaker contact axis is at least this fraction of the stronger one, " +
                 "the hit is treated as a diagonal/corner touch and no attach happens. 0 = only perfectly axis-aligned " +
                 "hits attach, 1 = corners attach too. ~0.5 means one side must clearly dominate.")]
        [Range(0f, 1f)]
        [SerializeField] private float cornerRejectRatio = 0.5f;

        private BrickNode _brickNode;
        private Collider _collider;
        private bool _isAttached;
        private float _reattachBlockedUntil = float.NegativeInfinity;

        private void Awake()
        {
            _brickNode = GetComponent<BrickNode>();
            // Collider may sit on the root or on a child (e.g. inside the visual) after a rebuild.
            _collider = GetComponentInChildren<Collider>();

            int playerAssemblyLayer = LayerMask.NameToLayer("PlayerAssembly");
            int worldBrickLayer = LayerMask.NameToLayer("WorldBrick");
            if (playerAssemblyLayer >= 0) levelGeometryMask &= ~(1 << playerAssemblyLayer);
            if (worldBrickLayer >= 0) levelGeometryMask &= ~(1 << worldBrickLayer);
        }

        // Called by PlayerAssembly.Detach() when this brick falls off (Docs section 6.3) - resets
        // it back to a free, attachable brick after a short grace period.
        public void OnDetached()
        {
            _isAttached = false;
            _reattachBlockedUntil = Time.time + detachGraceTime;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_isAttached || Time.time < _reattachBlockedUntil)
            {
                return;
            }

            BrickNode receiver = collision.collider.GetComponentInParent<BrickNode>();
            if (receiver == null)
            {
                return;
            }

            PlayerAssembly assembly = receiver.GetComponentInParent<PlayerAssembly>();
            if (assembly == null)
            {
                return;
            }

            if (!TryGetFaceDirection(assembly.transform, receiver, out CardinalDirection direction))
            {
                // Corner/diagonal contact - not a clean face hit, so nothing attaches.
                return;
            }

            if (checkTargetClearOfGeometry && !TargetCellIsClear(assembly, receiver, direction))
            {
                return;
            }

            if (assembly.Attach(_brickNode, receiver, direction))
            {
                _isAttached = true;
            }
        }

        // Docs section 5.3: the touched side is the dominant local-space axis of the centre delta
        // (local Z is ignored - attachments stay in the X/Y plane). Returns false for a corner/edge
        // hit where neither axis clearly dominates, so a diagonal landing doesn't snap on.
        private bool TryGetFaceDirection(Transform root, BrickNode receiver, out CardinalDirection direction)
        {
            direction = default;

            Collider receiverCollider = receiver.GetComponentInChildren<Collider>();
            Vector3 receiverCenter = receiverCollider != null ? receiverCollider.bounds.center : receiver.transform.position;
            Vector3 localDelta = root.InverseTransformPoint(_collider.bounds.center)
                                - root.InverseTransformPoint(receiverCenter);

            float ax = Mathf.Abs(localDelta.x);
            float ay = Mathf.Abs(localDelta.y);
            float dominant = Mathf.Max(ax, ay);
            if (dominant < 0.0001f)
            {
                return false;
            }

            // Both axes comparable => diagonal/corner touch => reject.
            if (Mathf.Min(ax, ay) >= cornerRejectRatio * dominant)
            {
                return false;
            }

            direction = ax >= ay
                ? (localDelta.x >= 0f ? CardinalDirection.Right : CardinalDirection.Left)
                : (localDelta.y >= 0f ? CardinalDirection.Up : CardinalDirection.Down);
            return true;
        }

        // Docs section 5.4: reject the snap if the target cell is blocked by level geometry.
        //
        // Sweeps a box one cell over starting from the RECEIVER's collider centre (which is up at
        // player height and known-clear), not from this free brick (which rests on the ground and
        // would make the sweep clip the floor it's sitting on). Physics.BoxCast (a sweep) works
        // against non-convex MeshColliders too, unlike Physics.OverlapBox which can't reliably
        // test them (it reports hits for any point inside the mesh's broad bounding box).
        private bool TargetCellIsClear(PlayerAssembly assembly, BrickNode receiver, CardinalDirection direction)
        {
            Collider receiverCollider = receiver.GetComponentInChildren<Collider>();
            Vector3 origin = receiverCollider != null ? receiverCollider.bounds.center : receiver.transform.position;

            Vector3 step = assembly.CellStepWorld(direction);
            float distance = step.magnitude;
            if (distance <= 0.001f)
            {
                return true;
            }

            // Kept shorter than a full cell (esp. vertically) so it clears floor studs directly
            // below the target and doesn't clip a flush neighbour to the side. bounds.size is
            // world-space, so it already accounts for scale (no separate lossyScale factor).
            Vector3 halfExtents = _collider.bounds.size * 0.3f;
            bool blocked = Physics.BoxCast(origin, halfExtents, step.normalized, out _,
                assembly.transform.rotation, distance, levelGeometryMask, QueryTriggerInteraction.Ignore);

            return !blocked;
        }
    }
}
