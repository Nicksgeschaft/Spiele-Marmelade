using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamUniverse.Shared.Stats
{
    // Designer-authored base stats for one character archetype (e.g. "Stats_Player",
    // "Stats_Slime"). Plain Inspector list - no custom editor tool needed for this.
    [CreateAssetMenu(fileName = "Stats_", menuName = "GameJam Universe/Stat Block")]
    public class StatBlockDefinition : ScriptableObject
    {
        [Serializable]
        public struct StatEntry
        {
            public StatType type;
            public float baseValue;
        }

        public List<StatEntry> stats = new();

        public float GetBaseValue(StatType type)
        {
            for (int i = 0; i < stats.Count; i++)
            {
                if (stats[i].type == type) return stats[i].baseValue;
            }
            return 0f;
        }
    }
}
