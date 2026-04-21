using UnityEngine;
public enum ItemType
{
    None,
    Food,
    Consumable,
    Money,
    Gift
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
                
        }

        return false;
    }

    public bool UseItem(CompanionFriendship companion)
    {
        if (itemType == ItemType.Gift)
        {
            companion.IncreaseFriendship(amountToChangeStat);
            return true;
        }
        else
        {
            return false;
        }

    }

}
