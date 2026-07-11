using System.Collections.Generic;
using System.IO;
using SpieleMarmelade.Core.Materials;
using UnityEditor;
using UnityEngine;

namespace SpieleMarmelade.DevTools.Editor
{
    /// <summary>
    /// Generates the shared <see cref="MaterialCategory"/> preset materials under
    /// Shared/Materials/Presets and (re)populates <see cref="MaterialPresetDatabase"/>.
    /// Existing presets are updated in place rather than duplicated, so this is safe to re-run.
    /// </summary>
    public static class MaterialPresetGenerator
    {
        private const string PresetsFolder = "Assets/_Project/Shared/Materials/Presets";
        private const string DatabasePath = "Assets/_Project/Shared/Materials/MaterialPresetDatabase.asset";
        private const string GoldMaterialPath = "Assets/_Project/Shared/Materials/Special/M_Special_MetalGold.mat";
        private const string SilverMaterialPath = "Assets/_Project/Shared/Materials/Special/M_Special_MetalSilver.mat";

        private struct PresetDefinition
        {
            public MaterialCategory Category;
            public Color BaseColor;
            public float Metallic;
            public float Smoothness;
            public Color? EmissionColor;

            public PresetDefinition(MaterialCategory category, Color baseColor, float metallic, float smoothness, Color? emissionColor = null)
            {
                Category = category;
                BaseColor = baseColor;
                Metallic = metallic;
                Smoothness = smoothness;
                EmissionColor = emissionColor;
            }
        }

        private static readonly PresetDefinition[] Definitions =
        {
            new(MaterialCategory.Neutral, new Color(0.80f, 0.80f, 0.80f), 0f, 0.4f),
            new(MaterialCategory.Stone, new Color(0.55f, 0.55f, 0.55f), 0f, 0.15f),
            new(MaterialCategory.Plastic, new Color(0.90f, 0.90f, 0.95f), 0f, 0.6f),
            new(MaterialCategory.Metal, new Color(0.70f, 0.70f, 0.75f), 0.85f, 0.7f),
            new(MaterialCategory.Wood, new Color(0.55f, 0.35f, 0.20f), 0f, 0.3f),
            new(MaterialCategory.Ice, new Color(0.75f, 0.92f, 1.0f), 0f, 0.9f),
            new(MaterialCategory.Lava, new Color(0.25f, 0.03f, 0.0f), 0f, 0.4f, new Color(3.5f, 0.6f, 0.05f)),
            new(MaterialCategory.Bronze, new Color(0.60f, 0.40f, 0.20f), 0.7f, 0.5f),
            new(MaterialCategory.Neon, new Color(0.05f, 0.05f, 0.05f), 0f, 0.6f, new Color(3.5f, 0.2f, 3.0f)),
            new(MaterialCategory.Hologram, new Color(0.4f, 0.9f, 1.0f), 0.2f, 0.9f, new Color(0.2f, 2.0f, 2.5f)),
            new(MaterialCategory.Grass, new Color(0.25f, 0.60f, 0.20f), 0f, 0.2f),
            new(MaterialCategory.Sand, new Color(0.85f, 0.75f, 0.50f), 0f, 0.2f),
            new(MaterialCategory.Dark, new Color(0.08f, 0.08f, 0.08f), 0.1f, 0.3f),
            new(MaterialCategory.Pastel, new Color(0.85f, 0.80f, 0.95f), 0f, 0.5f),
            new(MaterialCategory.Candy, new Color(1.0f, 0.40f, 0.70f), 0f, 0.8f),
            new(MaterialCategory.Retro, new Color(0.85f, 0.65f, 0.15f), 0f, 0.4f),
            new(MaterialCategory.SciFi, new Color(0.18f, 0.22f, 0.32f), 0.6f, 0.7f, new Color(0.1f, 1.5f, 2.0f)),
            new(MaterialCategory.Industrial, new Color(0.30f, 0.30f, 0.32f), 0.6f, 0.4f),
            new(MaterialCategory.Fantasy, new Color(0.50f, 0.25f, 0.70f), 0.2f, 0.6f),
            new(MaterialCategory.Future, new Color(0.90f, 0.95f, 1.0f), 0.3f, 0.85f, new Color(0.3f, 1.2f, 2.0f)),
            new(MaterialCategory.CelShading, new Color(0.75f, 0.75f, 0.75f), 0f, 0.1f),
        };

        [MenuItem("Tools/Utilities/Generate Material Presets")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder(PresetsFolder))
            {
                Directory.CreateDirectory(PresetsFolder);
                AssetDatabase.ImportAsset(PresetsFolder);
            }

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            var entries = new List<MaterialPresetDatabase.Entry>();

            foreach (PresetDefinition def in Definitions)
            {
                string path = $"{PresetsFolder}/M_Preset_{def.Category}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(litShader);
                    AssetDatabase.CreateAsset(material, path);
                }

                material.SetColor("_BaseColor", def.BaseColor);
                material.SetFloat("_Metallic", def.Metallic);
                material.SetFloat("_Smoothness", def.Smoothness);

                if (def.EmissionColor.HasValue)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", def.EmissionColor.Value);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }

                EditorUtility.SetDirty(material);
                entries.Add(new MaterialPresetDatabase.Entry { category = def.Category, material = material });
            }

            Material gold = AssetDatabase.LoadAssetAtPath<Material>(GoldMaterialPath);
            if (gold != null) entries.Add(new MaterialPresetDatabase.Entry { category = MaterialCategory.Gold, material = gold });

            Material silver = AssetDatabase.LoadAssetAtPath<Material>(SilverMaterialPath);
            if (silver != null) entries.Add(new MaterialPresetDatabase.Entry { category = MaterialCategory.Silver, material = silver });

            MaterialPresetDatabase database = AssetDatabase.LoadAssetAtPath<MaterialPresetDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<MaterialPresetDatabase>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            database.EditorSetPresets(entries);
            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MaterialPresetGenerator] Generated/updated {entries.Count} material presets.");
        }
    }
}
