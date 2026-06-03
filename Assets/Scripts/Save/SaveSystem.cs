using System.IO;
using UnityEngine;
using static Save.SaveData;

namespace Save
{
    public static class SaveSystem
    {
        private static string _customSaveDirectory;

        public static void SetCustomSaveDirectory(string customPath)
        {
            _customSaveDirectory = customPath;
        }

        public static void ClearCustomSaveDirectory()
        {
            _customSaveDirectory = null;
        }
    
        private static string SaveFilePath(string fileName = null)
        {
            fileName ??= "SaveData.json";

            string basePath = string.IsNullOrEmpty(_customSaveDirectory)
                ? Application.persistentDataPath
                : _customSaveDirectory;

            return Path.Combine(basePath, fileName);
        }

        private static string BackupFilePath(string fileName = null)
        {
            fileName ??= "SaveData.json";
            string ext = Path.GetExtension(fileName);
            string name = Path.GetFileNameWithoutExtension(fileName);
            string backupName = string.IsNullOrEmpty(ext) ? fileName + "_backup" : name + "_backup" + ext;
            return SaveFilePath(backupName);
        }

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

        public static bool IsSaveFileValid(string customFileName = null)
        {
            SaveData saveData = Load(customFileName);
            return saveData != null && !string.IsNullOrEmpty(saveData.CurrentScene);
        }

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