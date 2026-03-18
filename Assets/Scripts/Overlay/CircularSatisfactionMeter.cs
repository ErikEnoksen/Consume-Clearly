using UnityEngine;
using UnityEngine.UI;

public class CircularSatisfactionMeter : MonoBehaviour
{
    [SerializeField]
    private Slider slider;
    [SerializeField]
    private Image sliderFill;

    public void ChangeSatisfactionValue(float satisfactionValue)
    {
        slider.value += satisfactionValue;

        SatisfactionColor();
    }

    private void SatisfactionColor()
    {
        if(slider.value < 75 &&  slider.value > 0)
        {
            sliderFill.color = Color.deepSkyBlue;
        }
        else if (slider.value <= 0)
        {
            sliderFill.color = Color.red;
        }
        else if (slider.value >= 75)
        {
            sliderFill.color = Color.green;
        }
    }

}
