using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Ground check for the whole brick assembly: box-casts down (world -Y, the jump axis - see
    // the coordinate model in Docs/BrickMovementController_Anforderungen_v0.2.md section 2) from
    // every collider currently under PlayerRoot and looks for a hit whose normal points enough
    // along world-up (+Y) to count as standable ground. Works for any number of attached bricks
    // without extra wiring, and never reports the assembly's own colliders as ground.
    public class PlayerGroundSensor : MonoBehaviour
    {
        private static readonly Vector3 WorldUp = Vector3.up;
        private const float CastSkin = 0.05f;

        [SerializeField] private PlayerMovementStats stats;
        [SerializeField] private LayerMask groundMask = ~0;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            int playerAssemblyLayer = LayerMask.NameToLayer("PlayerAssembly");
            if (playerAssemblyLayer >= 0)
            {
                groundMask &= ~(1 << playerAssemblyLayer);
            }
        }

        private void FixedUpdate()
        {
            float threshold = stats != null ? stats.groundNormalThreshold : 0.6f;
            bool grounded = false;

            foreach (Collider brickCollider in GetComponentsInChildren<Collider>())
            {
                if (brickCollider.isTrigger) continue;

                Bounds bounds = brickCollider.bounds;
                Vector3 castHalfExtents = new Vector3(bounds.extents.x * 0.9f, 0.02f, bounds.extents.z * 0.9f);
                float castDistance = bounds.extents.y + CastSkin;

                bool hitGround = Physics.BoxCast(bounds.center, castHalfExtents, -WorldUp, out RaycastHit hit,
                    Quaternion.identity, castDistance, groundMask, QueryTriggerInteraction.Ignore);

                if (hitGround && Vector3.Dot(hit.normal, WorldUp) >= threshold)
                {
                    grounded = true;
                    break;
                }
            }

            IsGrounded = grounded;
        }
    }
}
