using UnityEngine;

namespace SpieleMarmelade.Shared.UI.MenuFlow
{
    // Keeps the MenuCamera's orthographic size correct for whatever the ACTUAL current screen
    // aspect ratio is, so 3D stage elements that need to line up with 2D Canvas rows (Options
    // icons/sliders — see StageAlignedElement) stay aligned at any resolution or window size,
    // not just the Canvas Scaler's 16:9 reference resolution.
    //
    // The Canvas Scaler here uses "Match Width" (matchWidthOrHeight = 0): the reference WIDTH
    // (1920) always spans the full screen width regardless of aspect ratio, so the horizontal
    // world-units-per-reference-pixel ratio never changes — only the vertical extent (and
    // therefore orthographicSize) needs to track the actual aspect ratio.
    public class MenuStageResizer : MonoBehaviour
    {
        public const float ReferenceWidth = 1920f;
        // Calibrated so a 1920x1080 (16:9) screen gives orthographicSize 3.6 — big enough that the
        // Options screen's title + 4 rows (each scaled up via OptionsRowScale) + buttons all
        // actually fit in view; smaller values looked great for the Main Menu but weren't tall
        // enough for Options once its rows grew, and the two screens share one camera. Keep
        // MenuFlowGenerator.BaselineOrthographicSize in sync with whatever this produces at 16:9
        // (540 * UnitsPerReferencePixel) — it's the fixed reference point every stage/canvas
        // conversion in that file (StageYToCanvasY, StageAlignedElement) is baked against.
        public const float UnitsPerReferencePixel = 1f / 150f;

        [SerializeField] private Camera stageCamera;

        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;

        private void Awake()
        {
            if (stageCamera == null) stageCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (stageCamera == null) return;
            if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight) return;

            _lastScreenWidth  = Screen.width;
            _lastScreenHeight = Screen.height;

            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            float refHeightVisible = ReferenceWidth / aspect;
            stageCamera.orthographicSize = refHeightVisible * UnitsPerReferencePixel * 0.5f;
        }
    }
}
