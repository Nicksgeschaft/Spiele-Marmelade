using UnityEngine;

namespace SpieleMarmelade.Shared.UI.MenuFlow
{
    // Keeps a MenuStageRoot child lined up with a fixed 2D Canvas anchoredPosition (AnchorTopCenter
    // convention: Y negative going down from the top, X offset from horizontal center) regardless
    // of the actual screen aspect ratio — see MenuStageResizer for the camera half of this. Used
    // for the Options screen's icons/sliders, which need to visually sit next to a Canvas row.
    public class StageAlignedElement : MonoBehaviour
    {
        [SerializeField] private float anchoredX;
        [SerializeField] private float anchoredY;
        [SerializeField] private Camera stageCamera;

        private float _lastOrthoSize = -1f;

        public void SetAnchored(float x, float y, Camera cam)
        {
            anchoredX = x;
            anchoredY = y;
            stageCamera = cam;
            Reposition();
        }

        private void LateUpdate()
        {
            if (stageCamera == null) return;
            if (Mathf.Approximately(stageCamera.orthographicSize, _lastOrthoSize)) return;
            Reposition();
        }

        private void Reposition()
        {
            if (stageCamera == null) return;
            _lastOrthoSize = stageCamera.orthographicSize;

            float k = MenuStageResizer.UnitsPerReferencePixel;
            float x = anchoredX * k;
            float y = stageCamera.orthographicSize + anchoredY * k;

            var pos = transform.localPosition;
            transform.localPosition = new Vector3(x, y, pos.z);
        }
    }
}
