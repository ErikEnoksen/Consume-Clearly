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
        public GameObject SettingsContainer;
        public GameObject MainMenuButtons;

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
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadProgress();
            }
            else
            {
                Debug.LogError("GameManager instance not found! Cannot continue game.");
            }
        }

        private void StartGame()
        {
            SceneManager.LoadScene(GAME_SCENE_NAME);
        }
    
        public void NewGame()
        {
            SaveSystem.ClearAllData();
            StartGame();
        }
    

    
        public void OpenSettings()
        {
            MainMenuButtons.SetActive(false);
            SettingsContainer.SetActive(true);
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