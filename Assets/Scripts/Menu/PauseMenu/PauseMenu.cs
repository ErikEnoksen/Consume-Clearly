using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;
    
    public GameObject Container;
    public GameObject keybindMenu;
    
    public GameObject SettingsContainer;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Pause")) && keybindMenu.activeSelf == false)
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
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    

}
