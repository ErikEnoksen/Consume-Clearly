using UnityEngine;
using UnityEngine.UI;

public class CircularSatisfactionMeter : MonoBehaviour
{
    [SerializeField] public Slider circularSatisfactionSlider;

    public int maxSatisfactionValue = 100;
    public int currentSatisfactionValue = 50;
    public int minSatisfactionValue = 0;

    private float passivePointTimer;
    public float passivePointInterval = 1f;

    private bool isPassivePointActive;

    private void Awake()
    {

        if (circularSatisfactionSlider == null)
        {
            Debug.LogError("Missing CircularSatisfactionMeter UI references!");
            enabled = false;
        }
    }

    private void Start()
    {
        circularSatisfactionSlider.maxValue = maxSatisfactionValue;
        circularSatisfactionSlider.minValue = minSatisfactionValue;
        circularSatisfactionSlider.value = currentSatisfactionValue;

        SatisfactionColor();
    }

    public void IncreaseSatisfactionValue(int value)
    {
        currentSatisfactionValue =
            Mathf.Min(maxSatisfactionValue,
            currentSatisfactionValue + value);

        circularSatisfactionSlider.value = currentSatisfactionValue;

        SatisfactionColor();
    }

    public void DecreaseSatisfactionValue(int value)
    {
        currentSatisfactionValue =
            Mathf.Max(minSatisfactionValue,
            currentSatisfactionValue - value);

        circularSatisfactionSlider.value = currentSatisfactionValue;

        SatisfactionColor();
    }

    private void LateUpdate()
    {
        if (!isPassivePointActive)
            return;

        passivePointTimer -= Time.deltaTime;

        if (passivePointTimer <= 0f)
        {
            IncreaseSatisfactionValue(1);
            passivePointTimer = passivePointInterval;
        }
    }

    public void ActivatePassivePoint()
    {
        isPassivePointActive = true;
        passivePointTimer = passivePointInterval;
    }

    private void SatisfactionColor()
    {
        Image sliderFill = circularSatisfactionSlider.fillRect.GetComponent<Image>();
        if (sliderFill != null)
        {
            if (currentSatisfactionValue <= 25)
            {
                sliderFill.color = Color.red;
            }
            else if (currentSatisfactionValue < 75)
            {
                sliderFill.color = Color.cyan;
            }
            else
            {
                sliderFill.color = Color.green;
            }
        }
    }
}