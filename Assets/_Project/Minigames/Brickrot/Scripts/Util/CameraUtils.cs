using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot
{
    // Used to park the Tetris camera far enough back that the whole playfield fits, whatever aspect
    // ratio the window happens to have.
    public static class CameraUtils
    {
        public static float VerticalFov(this Camera cam) => cam.fieldOfView;

        public static float HorizontalFov(this Camera cam)
        {
            float halfFieldOfViewRadians = cam.fieldOfView * Mathf.Deg2Rad * 0.5f;
            float horizontalFieldOfViewRadians = 2.0f * Mathf.Atan(Mathf.Tan(halfFieldOfViewRadians) * cam.aspect);
            return horizontalFieldOfViewRadians * Mathf.Rad2Deg;
        }

        // Distance at which a rect of the given extents is fully visible — the larger of the two
        // per-axis requirements, so neither axis ends up cropped.
        public static float CalculateDistanceToFit(this Camera cam, Vector2 extents)
        {
            float verticalFOV = cam.VerticalFov();
            float horizontalFOV = cam.HorizontalFov();

            float distanceVertical = extents.y / Mathf.Tan(verticalFOV * Mathf.Deg2Rad * 0.5f);
            float distanceHorizontal = extents.x / Mathf.Tan(horizontalFOV * Mathf.Deg2Rad * 0.5f);

            return Mathf.Max(distanceVertical, distanceHorizontal);
        }
    }
}
