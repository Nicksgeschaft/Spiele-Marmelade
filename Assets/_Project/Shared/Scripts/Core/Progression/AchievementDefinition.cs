using UnityEngine;

namespace GameJamUniverse.Core.Progression
{
    [CreateAssetMenu(fileName = "Achievement_", menuName = "GameJam Universe/Achievement Definition")]
    public class AchievementDefinition : ScriptableObject
    {
        [Tooltip("Stable unique id, e.g. 'play_first_game'. Never reuse or change once shipped.")]
        public string achievementId;
        public string title;
        [TextArea] public string description;
        public Sprite icon;

        public AchievementStatKey statKey;

        [Tooltip("Only used when statKey is CustomEvent: the event name reported via " +
                 "AchievementManager.ReportEvent(eventKey, amount), e.g. 'EnemyKilled'. " +
                 "Any gameplay event can be wired to that call via an AchievementEventHook, " +
                 "so new achievements for new events never require new C# code.")]
        public string eventKey;

        public float targetValue = 1f;

        [Tooltip("If true, the achievement unlocks when the stat drops to or below targetValue " +
                 "(used for time-based stats like 'Speedrunner').")]
        public bool lowerIsBetter;
    }
}
