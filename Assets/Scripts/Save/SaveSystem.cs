// =============================================================================
// SaveSystem.cs — File I/O Layer for Save Data
// 
//
// PURPOSE:
//   Static utility class that handles reading and writing SaveData to disk.
//   GameManager never touches the file system directly — it always goes through
//   here. This keeps all path logic and error handling in one place.
//
// FILE FORMAT:
//   SaveData is serialized to JSON via Unity's JsonUtility and stored in
//   Application.persistentDataPath (platform-specific, e.g. AppData on Windows).
//   Default filename is "SaveData.json". Scene-specific saves use the scene
//   name as the filename (e.g. "Shop.json").
//
// BACKUP SYSTEM:
//   Before overwriting a save, the existing file is copied to a "_backup" file
//   (e.g. "SaveData_backup.json"). If the primary file is missing or corrupt,
//   Load() automatically falls back to the backup with a warning.
//   This protects against data loss from crashes mid-save.
//
// CUSTOM DIRECTORY:
//   SetCustomSaveDirectory() redirects all file paths to a different folder.
//   Used in tests or when a custom save slot location is needed.
//   Call ClearCustomSaveDirectory() to revert to the default.
//
// INVALID SCENE GUARD:
//   Save() refuses to write if SaveData.IsSceneValidForSaving() returns false,
//   preventing menu/credits scenes from overwriting real gameplay saves.
// =============================================================================

using System.IO;
using UnityEngine;
using static Save.SaveData;

namespace Save
{
    public static class SaveSystem
    {
        // Overrides the default save directory. Null means use Application.persistentDataPath.
        private static string _customSaveDirectory;

        public static void SetCustomSaveDirectory(string customPath)
        {
            _customSaveDirectory = customPath;
        }

        public static void ClearCustomSaveDirectory()
        {
            _customSaveDirectory = null;
        }

        // --- Path Helpers ---
        // Resolves the full path for a save file. Defaults to "SaveData.json" if no name given.
        private static string SaveFilePath(string fileName = null)
        {
            fileName ??= "SaveData.json";

            string basePath = string.IsNullOrEmpty(_customSaveDirectory)
                ? Application.persistentDataPath
                : _customSaveDirectory;

            return Path.Combine(basePath, fileName);
        }

        // Resolves the backup path — same file but with "_backup" appended before the extension.
        private static string BackupFilePath(string fileName = null)
        {
            fileName ??= "SaveData.json";
            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);
            string backupName = string.IsNullOrEmpty(ext) ? fileName + "_backup" : name + "_backup" + ext;
            return SaveFilePath(backupName);
        }

        // --- Save ---
        // Copies the existing save to backup first, then writes the new JSON.
        // Aborts silently if the scene is invalid (MainMenu, Credits, etc.).
        public static void Save(SaveData data, string customFileName = null)
        {
            if (data == null || !data.IsSceneValidForSaving())
            {
                Debug.LogWarning("Save aborted: Scene is excluded from saving.");
                return;
            }

            try
            {
                string primaryPath = SaveFilePath(customFileName);

                // Always back up before overwriting so a mid-save crash doesn't corrupt both files.
                if (File.Exists(primaryPath))
                    File.Copy(primaryPath, BackupFilePath(customFileName), overwrite: true);

                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(primaryPath, json);
                Debug.Log("Game saved successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save game: {ex.Message}");
            }
        }

        // --- Load ---
        // Tries the primary file first. If it's missing or corrupt, falls back to backup.
        // Returns null if neither file exists or both fail to parse.
        public static SaveData Load(string customFileName = null)
        {
            string path = SaveFilePath(customFileName);

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    SaveData data = JsonUtility.FromJson<SaveData>(json);

                    if (!data.IsSceneValidForSaving())
                    {
                        Debug.LogWarning("Loaded save data is associated with an invalid scene. Save ignored.");
                        return null;
                    }

                    Debug.Log("Game loaded successfully!");
                    return data;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load game: {ex.Message}");
                }
            }

            // Primary failed — try the backup.
            string backupPath = BackupFilePath(customFileName);
            if (File.Exists(backupPath))
            {
                try
                {
                    string json = File.ReadAllText(backupPath);
                    SaveData data = JsonUtility.FromJson<SaveData>(json);

                    if (!data.IsSceneValidForSaving())
                    {
                        Debug.LogWarning("Backup save data is associated with an invalid scene. Save ignored.");
                        return null;
                    }

                    Debug.LogWarning("Primary save missing or corrupted. Loaded from backup.");
                    return data;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load backup save: {ex.Message}");
                }
            }

            return null;
        }

        // Returns true if a loadable, valid save file exists — used by the main menu
        // to decide whether to show a "Continue" button.
        public static bool IsSaveFileValid(string customFileName = null)
        {
            SaveData saveData = Load(customFileName);
            return saveData != null && !string.IsNullOrEmpty(saveData.CurrentScene);
        }

        // --- Clear ---
        // Deletes both the primary and backup file for a given slot.
        public static void ClearSaveData(string customFileName = null)
        {
            string path = SaveFilePath(customFileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("Save data cleared.");
            }

            string backupPath = BackupFilePath(customFileName);
            if (File.Exists(backupPath))
                File.Delete(backupPath);
        }

        // Deletes every .json file in the persistent data directory — used for "New Game".
        public static void ClearAllData()
        {
            string path = Application.persistentDataPath;
            if (!Directory.Exists(path))
                return;

            foreach (var file in Directory.GetFiles(path, "*.json"))
            {
                try { File.Delete(file); }
                catch (System.Exception ex) { Debug.LogWarning($"Could not delete {file}: {ex.Message}"); }
            }
        }
    }
}