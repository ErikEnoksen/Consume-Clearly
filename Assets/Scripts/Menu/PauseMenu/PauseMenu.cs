using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;
    
    public GameObject Container;
    public GameObject SettingsContainer;

    void Update()
    {
        if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Pause")))
        {
            if (SettingsContainer.activeSelf) return;

            if (isPaused)
                Resume();
            else
                Pause();
        }
    }
    public void Resume()
    {
        Container.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }
    
    public void Pause()
    {
        Container.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }
    public void Settings()
    {
        Container.SetActive(false); 
        SettingsContainer.SetActive(true);
    }
    public void Quit()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    

}
