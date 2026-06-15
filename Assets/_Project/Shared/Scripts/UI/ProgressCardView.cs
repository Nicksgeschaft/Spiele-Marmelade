using UnityEngine;
using UnityEngine.UI;

namespace GameJamUniverse.Shared.UI
{
    /// <summary>
    /// Generic progress card used by the Hub's Achievements and Challenges tabs. Shows a title,
    /// description, "current / target" progress text, a fill bar, and a status label.
    /// </summary>
    public class ProgressCardView : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text progressText;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Text statusText;

        private static readonly Color CompleteColor = new Color(0.4f, 0.8f, 0.4f);
        private static readonly Color IncompleteColor = new Color(0.4f, 0.6f, 0.9f);

        public void Bind(string title, string description, float current, float target, bool complete, string statusLabel)
        {
            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;

            float clampedTarget = Mathf.Max(target, 0.0001f);
            float fraction = Mathf.Clamp01(current / clampedTarget);

            if (progressText != null)
            {
                progressText.text = $"{FormatValue(current)} / {FormatValue(target)}";
            }

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = fraction;
                progressFillImage.color = complete ? CompleteColor : IncompleteColor;
            }

            if (statusText != null)
            {
                statusText.text = statusLabel;
                statusText.color = complete ? CompleteColor : IncompleteColor;
            }
        }

        private static string FormatValue(float value) =>
            Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.#");
    }
}
