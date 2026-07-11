using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Shared.Stats
{
    // Optional add-on: existing components (Health, MeleeCombatController, ...) check for this on their
    // own GameObject and use it when present, otherwise fall back to their own serialized
    // fields. Adding this component to a prefab is what opts it into the stat/modifier system.
    public class CharacterStats : MonoBehaviour
    {
        [SerializeField] private StatBlockDefinition baseStats;

        private readonly List<StatModifier> _modifiers = new();
        private readonly Dictionary<StatType, float> _cache = new();
        private bool _dirty = true;

        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null) return;
            _modifiers.Add(modifier);
            _dirty = true;
        }

        public void RemoveModifiersFromSource(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId)) return;
            if (_modifiers.RemoveAll(m => m.sourceId == sourceId) > 0)
            {
                _dirty = true;
            }
        }

        public float GetStat(StatType type)
        {
            if (_dirty) Recompute();
            return _cache.TryGetValue(type, out float value) ? value : (baseStats != null ? baseStats.GetBaseValue(type) : 0f);
        }

        /// <summary>True if this stat block defines a base value for <paramref name="type"/>, or
        /// a runtime modifier (e.g. a timed buff) currently targets it. Use this before letting
        /// CharacterStats override a component's own serialized field — a stat that's neither
        /// configured nor buffed should fall back to that field instead of resolving to 0.</summary>
        public bool HasStat(StatType type)
        {
            if (baseStats != null)
            {
                foreach (var entry in baseStats.stats)
                {
                    if (entry.type == type) return true;
                }
            }
            foreach (StatModifier modifier in _modifiers)
            {
                if (modifier.type == type) return true;
            }
            return false;
        }

        private void Update()
        {
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                StatModifier modifier = _modifiers[i];
                if (modifier.duration <= 0f) continue;

                modifier.duration -= Time.deltaTime;
                if (modifier.duration <= 0f)
                {
                    _modifiers.RemoveAt(i);
                    _dirty = true;
                }
            }
        }

        private void Recompute()
        {
            _cache.Clear();
            _dirty = false;

            var types = new HashSet<StatType>();
            if (baseStats != null)
            {
                foreach (var entry in baseStats.stats) types.Add(entry.type);
            }
            foreach (StatModifier modifier in _modifiers) types.Add(modifier.type);

            foreach (StatType type in types)
            {
                float baseValue = baseStats != null ? baseStats.GetBaseValue(type) : 0f;
                float flatSum = 0f;
                float percentAddSum = 0f;
                float percentMultiplyProduct = 1f;

                foreach (StatModifier modifier in _modifiers)
                {
                    if (modifier.type != type) continue;

                    switch (modifier.mode)
                    {
                        case StatModifierMode.Flat: flatSum += modifier.value; break;
                        case StatModifierMode.PercentAdd: percentAddSum += modifier.value; break;
                        case StatModifierMode.PercentMultiply: percentMultiplyProduct *= modifier.value; break;
                    }
                }

                _cache[type] = (baseValue + flatSum) * (1f + percentAddSum) * percentMultiplyProduct;
            }
        }
    }
}
