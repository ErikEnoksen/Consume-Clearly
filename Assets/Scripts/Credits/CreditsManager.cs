using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Credits
{
    public class CreditsManager : MonoBehaviour
    {
        private const string MAIN_MENU_SCENE = "MainMenu";

        [SerializeField] private Animation creditsTextAnimation;
        [SerializeField] private TextMeshProUGUI creditsTextUI;
        [SerializeField] private TextAsset creditsTextAsset;

        private void Start()
        {
            if (creditsTextUI != null && creditsTextAsset != null)
            {
                creditsTextUI.text = creditsTextAsset.text;
            }

            if (creditsTextAnimation != null && creditsTextAnimation.clip != null)
            {
                creditsTextAnimation.Play();
            }
        }

        public void ReturnToMainMenu()
        {
            if (creditsTextAnimation != null)
            {
                creditsTextAnimation.Stop();
            }

            SceneManager.LoadScene(MAIN_MENU_SCENE);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("Escape key pressed - Returning to main menu");
                ReturnToMainMenu();
            }

            if (creditsTextAnimation != null && !creditsTextAnimation.isPlaying)
            {
                Debug.Log("Credits creditsTextAnimation completed - Automatically returning to main menu");
                ReturnToMainMenu();
            }
        }
    }
}