using UnityEngine;

namespace SpieleMarmelade.Shared
{
    // Contract for a swappable movement style (Platformer, Top-Down Grid, Free 3D, ...).
    // Deliberately not tied to CharacterController: a future grid-based mover may want to
    // tween the transform directly instead of pushing it through CharacterController.Move.
    public interface IPlayerMovement
    {
        void Init(Transform playerTransform, PlayerInputReader input);
        void MovementTick(float deltaTime);
        Vector3 Velocity { get; }
        bool IsGrounded { get; }
    }
}
