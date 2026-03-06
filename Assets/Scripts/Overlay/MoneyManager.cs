using TMPro;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    [SerializeField]
    private int moneyCount = 0;
    private bool moneyChanged = false;

    [SerializeField]
    private TMP_Text moneyDisplay;

    private void Awake()
    {
        moneyDisplay.text = $"${moneyCount}";
    }

    public bool ChangeMoneyAmount(int amount)
    {
        if (amount > 0)
        {
            AddMoney(amount);
        }
        else
        {
            RemoveMoney(Mathf.Abs(amount));
        }
        return moneyChanged;
    }

    private void AddMoney(int amount)
    {
        moneyCount += amount;
        moneyDisplay.text = $"${moneyCount}";
        moneyChanged = true;
    }

    private void RemoveMoney(int amount)
    {
        if(amount <= moneyCount)
        {
            moneyCount -= amount;
            moneyDisplay.text = $"${moneyCount}";
            moneyChanged = true;
        }
        else
        {
            Debug.Log("Not enough money");
            moneyChanged = false;
        }
    }

}
