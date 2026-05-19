using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Save;


public class introManager : MonoBehaviour
{
    private const string Tutorial = "TutorialLevel";

    [SerializeField] private VideoPlayer videoPlayer;

    void Start()
    {
        videoPlayer.loopPointReached += LoadRoaming;
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("intro skipped");
            videoPlayer.frame=1032;
        }
    }

    void LoadRoaming(VideoPlayer vp)
    {
        Debug.Log("Intro finished");
        SaveSystem.ClearAllData();
        SceneManager.LoadScene(Tutorial);
    }
}
