using UnityEngine;
public enum ItemType
{
    None,
    Food,
    Consumable,
    Money,
    Tool
}


[CreateAssetMenu]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public ItemType itemType = new ItemType();
    public int amountToChangeStat;

    public bool UseItem()
    {
        switch (itemType)
        {
            case ItemType.None:
                Debug.Log("Not usable");
                return false;
            case ItemType.Food:
                PlayerHunger playerHunger = GameObject.Find("Sliders").GetComponent<PlayerHunger>();
                playerHunger.ChangeHungerValue(amountToChangeStat);
                return true;
            case ItemType.Consumable:
                //Consumable behaviour
                return true;
            case ItemType.Money:
                MoneyManager moneyManager = GameObject.Find("MoneyManager").GetComponent<MoneyManager>();
                moneyManager.ChangeMoneyAmount(amountToChangeStat);
                return true;
            case ItemType.Tool:
                //Tool behaviour
                return true;
                
        }

        return false;
    }

}
