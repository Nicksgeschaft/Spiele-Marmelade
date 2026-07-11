using UnityEngine;

namespace SpieleMarmelade.Core.Progression
{
    [CreateAssetMenu(fileName = "Challenge_", menuName = "Spiele Marmelade/Challenge Definition")]
    public class ChallengeDefinition : ScriptableObject
    {
        [Tooltip("Stable unique id, e.g. 'daily_play_3_games'.")]
        public string challengeId;
        public ChallengeType type;
        [TextArea] public string description;

        public ChallengeMetric metric;
        public float targetValue = 1f;
        public int rewardCoins = 10;
    }
}
