using UnityEngine;

public class SkyCycle : MonoBehaviour
{
    [SerializeField]
    private Material skybox;
    [SerializeField]
    private float elapsedTime;
    [SerializeField]
    private float timeScale = 2.5f;
    [SerializeField]
    private float slowScale = 60;
    private float slowedTime;

    private static readonly int Rotation = Shader.PropertyToID("_Rotation");
    private static readonly int Exposure = Shader.PropertyToID("_Exposure");

    // Update is called once per frame
    void Update()
    {
        elapsedTime += Time.deltaTime;
        slowedTime += Time.deltaTime/slowScale;
        //rotates the sky at a set rate
        skybox.SetFloat(Rotation, elapsedTime * timeScale);
        //changes the exposure of the material to simulate light and darkness
        skybox.SetFloat(Exposure, Mathf.Clamp(Mathf.Abs(Mathf.Cos(slowedTime))* (float)1.5, 0.15f, 1.5f));
    }
}
