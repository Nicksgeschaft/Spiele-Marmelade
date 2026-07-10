using UnityEngine;

namespace GameJamUniverse.World
{
    // Pure marker component — pack onto any wall brick's GameObject to make it climbable.
    // FreeThirdPersonMovement raycasts forward and checks for this via GetComponentInParent
    // when the player holds the Climb action; no behaviour lives here.
    [DisallowMultipleComponent]
    public class ClimbableSurface : MonoBehaviour
    {
    }
}
