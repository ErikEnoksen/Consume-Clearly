using System.IO;
using NUnit.Framework;
using UnityEngine;
using Save;

namespace Tests.EditMode
{
    public class SaveSystem_EditModeTests
    {
        private const string TestSaveFileName = "Test_SaveData.json"; // Temporary test file
        private string testSaveDirectory; // Custom directory for tests

        [SetUp]
        public void SetUp()
        {
            // Setup custom save directory
            testSaveDirectory = Path.Combine(Application.persistentDataPath, "TestSaves");
            if (!Directory.Exists(testSaveDirectory))
            {
                Directory.CreateDirectory(testSaveDirectory);
            }

            // Set the custom save directory for SaveSystem
            SaveSystem.SetCustomSaveDirectory(testSaveDirectory);
            SaveSystem.ClearSaveData(TestSaveFileName); // Ensure no data exists before the test
        }

        [TearDown]
        public void TearDown()
        {
            // Clear test save data
            SaveSystem.ClearSaveData(TestSaveFileName);

            // Remove the test directory
            if (Directory.Exists(testSaveDirectory))
            {
                Directory.Delete(testSaveDirectory, true);
            }

            // Reset SaveSystem to use the default directory
            SaveSystem.ClearCustomSaveDirectory();
        }

        [Test]
        public void SaveSystem_SavesDataCorrectly()
        {
            // Arrange
            var saveData = new SaveData
            {
                GameTime = 50.0f,
                CurrentScene = "test",
                PlayerPosition = new Vector3(0f, 5f, 10f)
            };

            // Act
            SaveSystem.Save(saveData, TestSaveFileName);
            var loadedData = SaveSystem.Load(TestSaveFileName);

            // Assert
            Assert.IsNotNull(loadedData, "Loaded data should not be null.");
            Assert.AreEqual(saveData.GameTime, loadedData.GameTime, "GameTime is not saved correctly.");
            Assert.AreEqual(saveData.CurrentScene, loadedData.CurrentScene, "CurrentScene is not saved correctly.");
            Assert.AreEqual(saveData.PlayerPosition, loadedData.PlayerPosition, "PlayerPosition is not saved correctly.");
        }

        [Test]
        public void SaveSystem_AbortsSavingInvalidScenes()
        {
            // Arrange
            var saveData = new SaveData
            {
                GameTime = 60.0f,
                CurrentScene = "MainMenu", // Invalid scene
                PlayerPosition = new Vector3(1f, 1f, 1f)
            };

            // Act
            SaveSystem.Save(saveData, TestSaveFileName);
            var loadedData = SaveSystem.Load(TestSaveFileName);

            // Assert
            Assert.IsNull(loadedData, "SaveSystem should not save data for invalid scenes like MainMenu.");
        }

        [Test]
        public void SaveSystem_IsSaveFileValid_WhenFileExists()
        {
            // Arrange
            var saveData = new SaveData
            {
                GameTime = 30.0f,
                CurrentScene = "TestScene",
                PlayerPosition = new Vector3(10f, 15f, 20f)
            };

            SaveSystem.Save(saveData, TestSaveFileName);

            // Act
            var isValid = SaveSystem.IsSaveFileValid(TestSaveFileName);

            // Assert
            Assert.IsTrue(isValid, "IsSaveFileValid should return true when a valid save file exists.");
        }

        [Test]
        public void SaveSystem_ClearSaveFile()
        {
            // Arrange
            var saveData = new SaveData
            {
                GameTime = 10.0f,
                CurrentScene = "Level1",
                PlayerPosition = Vector3.zero
            };

            SaveSystem.Save(saveData, TestSaveFileName);

            // Act
            SaveSystem.ClearSaveData(TestSaveFileName);
            var loadedData = SaveSystem.Load(TestSaveFileName);

            // Assert
            Assert.IsNull(loadedData, "Save data should be null after clearing save file.");
        }

        [Test]
        public void SaveData_ExcludesInvalidScenes()
        {
            // Arrange
            var saveData1 = new SaveData { CurrentScene = "Credits" };
            var saveData2 = new SaveData { CurrentScene = "MainMenu" };
            var saveData3 = new SaveData { CurrentScene = "GameplayScene" };

            // Act & Assert
            Assert.IsFalse(saveData1.IsSceneValidForSaving(), "Credits scene should be excluded from saving.");
            Assert.IsFalse(saveData2.IsSceneValidForSaving(), "MainMenu scene should be excluded from saving.");
            Assert.IsTrue(saveData3.IsSceneValidForSaving(), "Gameplay scene should be valid for saving.");
        }

        [Test]
        public void SaveSystem_BackupCreatedOnSecondSave()
        {
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", Money = 100 }, TestSaveFileName);
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", Money = 200 }, TestSaveFileName);

            Assert.AreEqual(200, SaveSystem.Load(TestSaveFileName).Money, "Primary should hold second save.");

            File.Delete(Path.Combine(testSaveDirectory, TestSaveFileName));
            var backup = SaveSystem.Load(TestSaveFileName);

            Assert.IsNotNull(backup, "Backup should exist after second save.");
            Assert.AreEqual(100, backup.Money, "Backup should hold first save.");
        }

        [Test]
        public void SaveSystem_FallsBackToBackupWhenPrimaryMissing()
        {
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", Money = 50 }, TestSaveFileName);
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", Money = 99 }, TestSaveFileName);

            File.Delete(Path.Combine(testSaveDirectory, TestSaveFileName));
            var loaded = SaveSystem.Load(TestSaveFileName);

            Assert.IsNotNull(loaded, "Should load from backup when primary is missing.");
            Assert.AreEqual(50, loaded.Money);
        }

        [Test]
        public void SaveSystem_ClearSaveData_AlsoClearsBackup()
        {
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", Money = 1 }, TestSaveFileName);
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", Money = 2 }, TestSaveFileName);

            SaveSystem.ClearSaveData(TestSaveFileName);

            Assert.IsNull(SaveSystem.Load(TestSaveFileName), "Load should return null after clearing both primary and backup.");
        }
    }
}