using TMPro;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    [SerializeField]
    private int moneyCount = 0;

    [SerializeField]
    private TMP_Text moneyDisplay;

    private void Awake()
    {
        moneyDisplay.text = $"${moneyCount.ToString()}";
    }

    public void ChangeMoneyAmount(int amount)
    {
        moneyCount += amount;
        moneyDisplay.text = $"${moneyCount.ToString()}";

    }

}
