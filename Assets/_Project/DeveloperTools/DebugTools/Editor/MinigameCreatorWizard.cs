using System.IO;
using System.Text.RegularExpressions;
using GameJamUniverse.Core.Minigames;
using UnityEditor;
using UnityEngine;

namespace GameJamUniverse.DevTools.Editor
{
    /// <summary>
    /// Duplicates the _Template minigame into a new Minigames/&lt;Name&gt; folder, renaming the
    /// controller script, asmdef, scene and metadata asset so the result compiles immediately and
    /// is ready to register via <see cref="MinigameRegistryWindow"/>.
    /// </summary>
    public class MinigameCreatorWizard : EditorWindow
    {
        private const string TemplateFolder = "Assets/_Project/Minigames/_Template";
        private const string MinigamesRoot = "Assets/_Project/Minigames";
        private const string TemplateClassName = "TemplateMinigameController";
        private const string TemplateNamespace = "GameJamUniverse.Minigames.Template";

        private string _gameName = "MyNewGame";
        private string _displayName = "My New Game";

        [MenuItem("Tools/GameJam/Create New Minigame...")]
        public static void Open()
        {
            GetWindow<MinigameCreatorWizard>("Create Minigame");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Duplicates the _Template minigame into Minigames/<Name>, renaming the controller " +
                "script, asmdef, scene and metadata asset. Run Tools/GameJam/Game Registry afterwards " +
                "to add it to the Hub.", MessageType.Info);

            EditorGUILayout.Space();
            _gameName = EditorGUILayout.TextField("Folder / Class Name", _gameName);
            _displayName = EditorGUILayout.TextField("Display Name", _displayName);

            bool validName = !string.IsNullOrWhiteSpace(_gameName) && Regex.IsMatch(_gameName, "^[A-Za-z][A-Za-z0-9_]*$");
            if (!validName)
            {
                EditorGUILayout.HelpBox("Folder/Class Name must start with a letter and contain only letters, digits or underscores.", MessageType.Error);
            }

            using (new EditorGUI.DisabledScope(!validName))
            {
                if (GUILayout.Button("Create Minigame"))
                {
                    string displayName = string.IsNullOrWhiteSpace(_displayName) ? _gameName : _displayName.Trim();
                    Create(_gameName.Trim(), displayName);
                }
            }
        }

        private static void Create(string gameName, string displayName)
        {
            string targetFolder = $"{MinigamesRoot}/{gameName}";
            if (AssetDatabase.IsValidFolder(targetFolder))
            {
                Debug.LogError($"[MinigameCreatorWizard] Folder already exists: '{targetFolder}'.");
                return;
            }

            CopyDirectory(ToAbsolute(TemplateFolder), ToAbsolute(targetFolder));

            string newClassName = $"{gameName}MinigameController";
            string newNamespace = $"GameJamUniverse.Minigames.{gameName}";

            // Controller script: rewrite namespace/class name and rename the file.
            string oldScriptAbs = ToAbsolute($"{targetFolder}/Scripts/{TemplateClassName}.cs");
            string newScriptAbs = ToAbsolute($"{targetFolder}/Scripts/{newClassName}.cs");
            string scriptSource = File.ReadAllText(oldScriptAbs)
                .Replace(TemplateNamespace, newNamespace)
                .Replace(TemplateClassName, newClassName);
            File.WriteAllText(newScriptAbs, scriptSource);
            File.Delete(oldScriptAbs);

            // Asmdef: rewrite name/rootNamespace and rename the file.
            string oldAsmdefAbs = ToAbsolute($"{targetFolder}/Scripts/GameJamUniverse.Minigames.Template.asmdef");
            string newAsmdefAbs = ToAbsolute($"{targetFolder}/Scripts/GameJamUniverse.Minigames.{gameName}.asmdef");
            string asmdefSource = File.ReadAllText(oldAsmdefAbs).Replace(TemplateNamespace, newNamespace);
            File.WriteAllText(newAsmdefAbs, asmdefSource);
            File.Delete(oldAsmdefAbs);

            // Metadata asset: rename the file (fields are set below via the loaded asset).
            string oldMetaAbs = ToAbsolute($"{targetFolder}/Config/MinigameMetadata_Template.asset");
            string newMetaAbs = ToAbsolute($"{targetFolder}/Config/MinigameMetadata_{gameName}.asset");
            File.Move(oldMetaAbs, newMetaAbs);

            string oldScriptGuid = ReadGuid(ToAbsolute($"{TemplateFolder}/Scripts/{TemplateClassName}.cs.meta"));
            string oldMetaGuid = ReadGuid(ToAbsolute($"{TemplateFolder}/Config/MinigameMetadata_Template.asset.meta"));

            // First refresh: import the new script + metadata asset so Unity assigns them fresh GUIDs.
            AssetDatabase.Refresh();

            string newScriptGuid = ReadGuid(newScriptAbs + ".meta");
            string newMetaGuid = ReadGuid(newMetaAbs + ".meta");

            // Scene: rewrite the controller component's script/metadata references and rename the file.
            string oldSceneAbs = ToAbsolute($"{targetFolder}/Scenes/Template.unity");
            string newSceneAbs = ToAbsolute($"{targetFolder}/Scenes/{gameName}.unity");
            string sceneSource = File.ReadAllText(oldSceneAbs)
                .Replace(oldScriptGuid, newScriptGuid)
                .Replace(oldMetaGuid, newMetaGuid)
                .Replace($"{TemplateNamespace}::{TemplateNamespace}.{TemplateClassName}", $"{newNamespace}::{newNamespace}.{newClassName}");
            File.WriteAllText(newSceneAbs, sceneSource);
            File.Delete(oldSceneAbs);

            AssetDatabase.Refresh();

            var metadata = AssetDatabase.LoadAssetAtPath<MinigameMetadata>($"{targetFolder}/Config/MinigameMetadata_{gameName}.asset");
            if (metadata != null)
            {
                metadata.gameId = gameName;
                metadata.displayName = displayName;
                metadata.sceneName = gameName;
                EditorUtility.SetDirty(metadata);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[MinigameCreatorWizard] Created minigame '{gameName}' at '{targetFolder}'. " +
                      "Run Tools/GameJam/Game Registry and click 'Sync Registry' to add it to the Hub.");

            EditorGUIUtility.PingObject(metadata);
        }

        private static string ToAbsolute(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, assetPath).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string ReadGuid(string metaFileAbsPath)
        {
            foreach (string line in File.ReadAllLines(metaFileAbsPath))
            {
                if (line.StartsWith("guid:"))
                {
                    return line.Substring("guid:".Length).Trim();
                }
            }
            return null;
        }

        private static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName.EndsWith(".meta")) continue;
                if (fileName == ".gitkeep") continue;

                File.Copy(filePath, Path.Combine(targetDir, fileName), overwrite: false);
            }

            foreach (string dirPath in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dirPath);
                CopyDirectory(dirPath, Path.Combine(targetDir, dirName));
            }
        }
    }
}
