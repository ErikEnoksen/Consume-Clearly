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

        public static void Save(SaveData data, string customFileName = null)
        {
            if (data == null || !data.IsSceneValidForSaving())
            {
                Debug.LogWarning("Save aborted: Scene is excluded from saving.");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(SaveFilePath(customFileName), json);
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
            else
            {
                Debug.LogWarning("Save file not found.");
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
        }
        public static void ClearAllData()
        {
            string path=Application.persistentDataPath;
            if (!Directory.Exists(path))
            {
                Debug.Log("No such file in path");
                return;
            }
            string [] files=Directory.GetFiles(path);

            foreach (var file in files)
            {
                ClearSaveData(file);
            }
        }
    }
}