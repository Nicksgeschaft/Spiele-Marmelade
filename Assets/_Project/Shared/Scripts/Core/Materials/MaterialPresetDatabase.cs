using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamUniverse.Core.Materials
{
    /// <summary>
    /// Maps each <see cref="MaterialCategory"/> to a shared preset material. Populated by the
    /// editor-only Material Preset Generator tool.
    /// </summary>
    [CreateAssetMenu(fileName = "MaterialPresetDatabase", menuName = "GameJam Universe/Material Preset Database")]
    public class MaterialPresetDatabase : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public MaterialCategory category;
            public Material material;
        }

        [SerializeField] private List<Entry> presets = new();

        public IReadOnlyList<Entry> Presets => presets;

        public Material Find(MaterialCategory category)
        {
            foreach (Entry entry in presets)
            {
                if (entry != null && entry.category == category)
                {
                    return entry.material;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        public void EditorSetPresets(List<Entry> newPresets) => presets = newPresets;
#endif
    }
}
