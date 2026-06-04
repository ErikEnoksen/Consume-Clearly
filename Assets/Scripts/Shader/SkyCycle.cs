// =============================================================================
// SkyCyle.cs - Changes the background
//
// PURPOSE:
//   This script changes the settings of the background material to simulate a sky.
//   It does this by rotating and changing the exposure
// =============================================================================
using UnityEngine;

public class SkyCycle : MonoBehaviour
{
    [SerializeField]
    private Material skybox;
    [SerializeField]
    private float elapsedTime;
    [SerializeField]
    private float exposureModifier = 1.5f;
    [SerializeField]
    private float timeScale = 1.5f;
    [SerializeField]
    private float slowScale = 60;
    private float slowedTime;

    private static readonly int Rotation = Shader.PropertyToID("_Rotation");
    private static readonly int Exposure = Shader.PropertyToID("_Exposure");

    private void Start()
    {
        if (DayCycleManager.Instance != null)
        {
            float totalElapsed = (DayCycleManager.Instance.CurrentDay - 1) * DayCycleManager.Instance.dayLength
                                 + DayCycleManager.Instance.DayTimer;
            elapsedTime = totalElapsed;
            slowedTime = Mathf.Repeat(totalElapsed / slowScale, Mathf.PI);
        }
    }

    public void SetMorning()
    {
        slowedTime = 0;
    }

    void FixedUpdate()
    {
        elapsedTime += Time.deltaTime;
        if(slowedTime >= Mathf.PI)
        {
            slowedTime = 0;
        }
        else
        {
            slowedTime += Time.deltaTime/slowScale;
        }
        //rotates the sky at a set rate
        skybox.SetFloat(Rotation, elapsedTime * timeScale);
        //changes the exposure of the material to simulate light and darkness
        skybox.SetFloat(Exposure, Mathf.Clamp(Mathf.Abs(Mathf.Cos(slowedTime))* exposureModifier, (0.1f * exposureModifier), exposureModifier));
    }

    private void OnDisable()
    {
        skybox.SetFloat(Rotation, 0);
        skybox.SetFloat(Exposure, exposureModifier);
    }
}
