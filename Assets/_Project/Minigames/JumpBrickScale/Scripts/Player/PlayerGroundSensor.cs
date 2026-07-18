using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Ground check for the whole brick assembly, driven by real collision contacts (Docs section 4.4).
    // Because the single Rigidbody lives on PlayerRoot, OnCollisionStay here reports contacts for every
    // collider in the assembly - main brick and attached bricks alike - with no extra wiring.
    //
    // The contact normal is what separates floor from wall from ceiling, and it's why this replaced an
    // earlier downward BoxCast: a cast can clip a wall's edge or a stud and mistake it for ground,
    // which let the player jump their way up a flat wall.
    //   floor   -> normal points up      (dot ~ +1) -> counts as ground
    //   wall    -> normal is horizontal  (dot ~  0) -> ignored, so no wall climbing
    //   ceiling -> normal points down    (dot ~ -1) -> ignored, so touching an overhang never grants a jump
    [RequireComponent(typeof(StatAggregator))]
    public class PlayerGroundSensor : MonoBehaviour
    {
        private static readonly Vector3 WorldUp = Vector3.up;

        [Tooltip("Which layers count as standable ground. The PlayerAssembly layer is excluded automatically.")]
        [SerializeField] private LayerMask groundMask = ~0;

        private StatAggregator _statAggregator;

        // Collision callbacks run after FixedUpdate, so a contact seen this physics step is consumed by
        // the next one. Coyote time comfortably covers that single-step delay.
        private bool _groundContactSinceLastTick;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            _statAggregator = GetComponent<StatAggregator>();

            int playerAssemblyLayer = LayerMask.NameToLayer("PlayerAssembly");
            if (playerAssemblyLayer >= 0)
            {
                groundMask &= ~(1 << playerAssemblyLayer);
            }
        }

        private void FixedUpdate()
        {
            IsGrounded = _groundContactSinceLastTick;
            _groundContactSinceLastTick = false;
        }

        private void OnCollisionStay(Collision collision)
        {
            if (_groundContactSinceLastTick)
            {
                return;
            }

            if ((groundMask.value & (1 << collision.gameObject.layer)) == 0)
            {
                return;
            }

            float threshold = _statAggregator.Current.GroundNormalThreshold;
            int contactCount = collision.contactCount;
            for (int i = 0; i < contactCount; i++)
            {
                if (Vector3.Dot(collision.GetContact(i).normal, WorldUp) >= threshold)
                {
                    _groundContactSinceLastTick = true;
                    return;
                }
            }
        }
    }
}
