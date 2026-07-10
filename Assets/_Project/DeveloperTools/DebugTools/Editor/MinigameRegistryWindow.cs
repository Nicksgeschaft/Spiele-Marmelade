using System.Collections.Generic;
using System.Linq;
using GameJamUniverse.Core.Minigames;
using UnityEditor;
using UnityEngine;

namespace GameJamUniverse.DevTools.Editor
{
    /// <summary>
    /// Shows every <see cref="MinigameMetadata"/> found under Minigames/*/Config/ and lets the
    /// developer push that list into <see cref="MinigameRegistry"/> so the Hub picks up new
    /// minigames without any manual wiring.
    /// </summary>
    public class MinigameRegistryWindow : EditorWindow
    {
        private const string RegistryPath = "Assets/_Project/Shared/Data/MinigameRegistry.asset";

        private Vector2 _scroll;

        [MenuItem("Tools/GameJam/Game Registry")]
        public static void Open()
        {
            GetWindow<MinigameRegistryWindow>("Game Registry");
        }

        private void OnGUI()
        {
            List<MinigameMetadata> discovered = DiscoverMinigames();

            EditorGUILayout.LabelField($"Discovered {discovered.Count} minigame(s) under Minigames/*/Config/", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (MinigameMetadata metadata in discovered)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(metadata.gameId, GUILayout.Width(140));
                EditorGUILayout.LabelField(metadata.displayName, GUILayout.Width(160));
                EditorGUILayout.LabelField(metadata.genre.ToString(), GUILayout.Width(80));
                EditorGUILayout.LabelField(metadata.sceneName, GUILayout.Width(120));
                EditorGUILayout.ObjectField(metadata, typeof(MinigameMetadata), false);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            MinigameRegistry registry = AssetDatabase.LoadAssetAtPath<MinigameRegistry>(RegistryPath);
            int registeredCount = registry != null ? registry.Games.Count : 0;
            EditorGUILayout.LabelField($"Registry currently has {registeredCount} game(s).");

            if (GUILayout.Button("Sync Registry"))
            {
                SyncRegistry(discovered);
            }
        }

        internal static List<MinigameMetadata> DiscoverMinigames()
        {
            var results = new List<MinigameMetadata>();
            string[] guids = AssetDatabase.FindAssets("t:MinigameMetadata", new[] { "Assets/_Project/Minigames" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("/Config/")) continue;

                var metadata = AssetDatabase.LoadAssetAtPath<MinigameMetadata>(path);
                if (metadata != null) results.Add(metadata);
            }

            return results.OrderBy(m => m.gameId).ToList();
        }

        internal static void SyncRegistry(List<MinigameMetadata> discovered)
        {
            var registry = AssetDatabase.LoadAssetAtPath<MinigameRegistry>(RegistryPath);
            if (registry == null)
            {
                Debug.LogError($"[MinigameRegistryWindow] No MinigameRegistry asset found at '{RegistryPath}'.");
                return;
            }

            registry.EditorSetGames(discovered);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MinigameRegistryWindow] Synced {discovered.Count} minigame(s) into the registry.");
        }
    }
}
