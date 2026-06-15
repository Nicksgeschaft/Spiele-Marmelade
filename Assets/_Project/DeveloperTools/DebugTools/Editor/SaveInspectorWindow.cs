using System.IO;
using GameJamUniverse.Core.SaveSystem;
using UnityEditor;
using UnityEngine;

namespace GameJamUniverse.DevTools.Editor
{
    /// <summary>
    /// Pretty-prints the player's save.json from <see cref="Application.persistentDataPath"/> and
    /// offers quick actions for iterating on save-related features during development.
    /// </summary>
    public class SaveInspectorWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _json = "";
        private string _error;

        [MenuItem("Tools/GameJam/Save Inspector")]
        public static void Open()
        {
            var window = GetWindow<SaveInspectorWindow>("Save Inspector");
            window.Refresh();
        }

        private void OnEnable() => Refresh();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Save File", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(SavePath, EditorStyles.wordWrappedLabel);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh")) Refresh();
                if (GUILayout.Button("Open Containing Folder")) EditorUtility.RevealInFinder(SavePath);

                using (new EditorGUI.DisabledScope(!File.Exists(SavePath)))
                {
                    if (GUILayout.Button("Delete Save File") &&
                        EditorUtility.DisplayDialog("Delete Save File", $"Delete '{SavePath}'? This cannot be undone.", "Delete", "Cancel"))
                    {
                        File.Delete(SavePath);
                        Refresh();
                    }
                }
            }

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(_error))
            {
                EditorGUILayout.HelpBox(_error, MessageType.Warning);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_json, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        private void Refresh()
        {
            _error = null;

            if (!File.Exists(SavePath))
            {
                _json = "(no save file yet)";
                return;
            }

            try
            {
                string raw = File.ReadAllText(SavePath);
                SaveData data = JsonUtility.FromJson<SaveData>(raw);
                _json = JsonUtility.ToJson(data, prettyPrint: true);
            }
            catch (System.Exception ex)
            {
                _error = $"Failed to parse save file: {ex.Message}";
                _json = File.ReadAllText(SavePath);
            }
        }
    }
}
