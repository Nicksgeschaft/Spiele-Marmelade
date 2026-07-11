using UnityEngine;

namespace SpieleMarmelade.Shared.Cameras
{
    // Contract for a swappable camera perspective (Side-Scroll, Top-Down, Third-Person, ...).
    // Kept hand-written and dependency-free (no Cinemachine) so jam projects stay light.
    public interface ICameraRig
    {
        void Init(Transform target);
        void LateUpdateFollow();
    }
}
