using System;
using System.IO;
using UnityEngine;

namespace SpieleMarmelade.Core.SaveSystem
{
    /// <summary>
    /// Reads/writes the player's <see cref="SaveData"/> as JSON under
    /// <see cref="Application.persistentDataPath"/>. A single instance is owned by GameManager
    /// and shared by every system (achievements, statistics, challenges, per-minigame progress).
    /// </summary>
    public class SaveSystem
    {
        private const string SaveFileName = "save.json";

        private readonly string _savePath;

        public SaveData Current { get; private set; }

        public SaveSystem(string saveDirectory = null)
        {
            _savePath = Path.Combine(saveDirectory ?? Application.persistentDataPath, SaveFileName);
        }

        public string SavePath => _savePath;

        public SaveData Load()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    string json = File.ReadAllText(_savePath);
                    SaveData loaded = JsonUtility.FromJson<SaveData>(json);
                    Current = SaveMigrator.Migrate(loaded ?? new SaveData());
                    return Current;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to load save file at '{_savePath}': {ex}");
            }

            Current = new SaveData();
            return Current;
        }

        public void Save()
        {
            Save(Current);
        }

        public void Save(SaveData data)
        {
            Current = data ?? new SaveData();

            try
            {
                string directory = Path.GetDirectoryName(_savePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(Current, prettyPrint: true);
                string tempPath = _savePath + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(_savePath))
                {
                    File.Delete(_savePath);
                }

                File.Move(tempPath, _savePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Failed to write save file at '{_savePath}': {ex}");
            }
        }
    }
}
