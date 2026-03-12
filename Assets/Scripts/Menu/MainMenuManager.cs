using Save;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MainMenu
{
    public class MainMenuManager : MonoBehaviour
    {
        private const string CREDITS_SCENE_NAME = "Credits";
        private const string GAME_SCENE_NAME = "IntroVid";

        private void Start()
        {
            SetupAllMenuButtons();
        }
    
        private void SetupAllMenuButtons()
        {
            var buttons = GetComponentsInChildren<Button>(true);
        
            foreach (var button in buttons)
            {
                var text = button.GetComponentInChildren<TMP_Text>(true);
                var effect = text.gameObject.AddComponent<MenuButtonEffect>();
            
                var buttonNameLower = button.name.ToLower();
                if (buttonNameLower.Contains("continue"))
                {
                    bool saveExists = SaveSystem.IsSaveFileValid();
                    effect.SetEnabled(saveExists);
                }
            }
        }
        public void ContinueGame()
        {
            SaveData saveData = SaveSystem.Load();
            if (saveData != null)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoadProgress();
                }
                else
                {
                    Debug.LogWarning("GameManager instance not found! Falling back to scene load.");
                }
                SceneManager.LoadScene(saveData.CurrentScene);
            }
            else
            {
                Debug.LogError("No save data found! Unable to continue.");
            }
        }

        private void StartGame()
        {
            SceneManager.LoadScene(GAME_SCENE_NAME);
        }
    
        public void NewGame()
        {
            SaveSystem.ClearSaveData(); // New method to delete any existing save.
            StartGame();
        }
    

    
        public void OpenSettings()
        {
            Debug.Log("Settings menu opened! Implementation pending.");
        }

        public void LoadCredits()
        {
            SceneManager.LoadScene(CREDITS_SCENE_NAME);
        }
    
        public void ExitGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
        }
    }
}