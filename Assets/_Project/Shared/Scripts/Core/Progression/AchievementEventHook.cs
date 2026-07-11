using SpieleMarmelade.Core.Managers;
using UnityEngine;

namespace SpieleMarmelade.Core.Progression
{
    // No-code glue: wire any UnityEvent (Health.OnDeath, LevelExitTrigger.OnPlayerReached, ...)
    // to Report() in the Inspector to feed a named counter that CustomEvent achievements watch.
    public class AchievementEventHook : MonoBehaviour
    {
        [SerializeField] private string eventKey;
        [SerializeField] private float amount = 1f;

        /// <summary>Reports the configured eventKey/amount. Wire this to a UnityEvent in the Inspector.</summary>
        public void Report()
        {
            GameManager.Instance?.AchievementManager?.ReportEvent(eventKey, amount);
        }

        /// <summary>Reports the configured eventKey with a caller-supplied amount instead of the configured one.</summary>
        public void ReportAmount(float customAmount)
        {
            GameManager.Instance?.AchievementManager?.ReportEvent(eventKey, customAmount);
        }
    }
}
