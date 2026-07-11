using UnityEngine;

namespace SpieleMarmelade.Shared.World
{
    // Segmented pip-style bar built from N bricks (see BrickBarBuilderWindow). SetFilledCount/
    // SetValue01 show/hide segments from the end, giving a "health bar made of bricks" look —
    // wire a value source (e.g. BrickHealthBarView) and no further code is needed.
    public class BrickBar : MonoBehaviour
    {
        [SerializeField] private GameObject[] segments;

        public int SegmentCount => segments != null ? segments.Length : 0;

        public void SetFilledCount(int count)
        {
            if (segments == null) return;
            count = Mathf.Clamp(count, 0, segments.Length);
            for (int i = 0; i < segments.Length; i++)
                if (segments[i] != null) segments[i].SetActive(i < count);
        }

        public void SetValue01(float normalized) =>
            SetFilledCount(Mathf.RoundToInt(Mathf.Clamp01(normalized) * SegmentCount));
    }
}
