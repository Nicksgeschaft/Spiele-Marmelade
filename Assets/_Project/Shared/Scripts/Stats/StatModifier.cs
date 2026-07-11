using System;

namespace SpieleMarmelade.Shared.Stats
{
    public enum StatModifierMode
    {
        Flat,
        PercentAdd,
        PercentMultiply
    }

    // Runtime buff/debuff, not an asset. sourceId lets a specific buff be removed later
    // (e.g. "SwordBuff_123") via CharacterStats.RemoveModifiersFromSource.
    [Serializable]
    public class StatModifier
    {
        public StatType type;
        public StatModifierMode mode;
        public float value;

        /// <summary>Seconds remaining. 0 or less = permanent (never auto-expires).</summary>
        public float duration;

        public string sourceId;
    }
}
