using UnityEngine;
using UnityEngine.UI;

public class PlayerHunger : MonoBehaviour
{
    [SerializeField]
    private Slider hungerBar;
    [SerializeField]
    private float hungerSpeed = 0.1f;
    private bool requiredHunger = false;


    private void LateUpdate()
    {
        hungerBar.value -= hungerSpeed * Time.deltaTime;
    }

    public float GetHungerValue() => hungerBar.value;

    public void SetHungerValue(float value) => hungerBar.value = value;

    public bool ChangeHungerValue(float value)
    {
        if (value > 0)
        {
            AddToHunger(value);    
        }
        else 
        {
            RemoveFromHunger(Mathf.Abs(value));
        }
        return requiredHunger;
    }

    private void AddToHunger(float value)
    {
        hungerBar.value += value;
        requiredHunger = true;
    }

    private void RemoveFromHunger(float value)
    {
        if (hungerBar.value >= value)
        {
            hungerBar.value -= value;
            requiredHunger = true;
        }
        else
        {
            Debug.Log("Not enough energy");
            requiredHunger = false; 
        }
    }

}
