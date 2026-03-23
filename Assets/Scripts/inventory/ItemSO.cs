using UnityEngine;

[CreateAssetMenu]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public ItemType itemType = new ItemType();
    public int amountToChangeStat;

    public bool UseItem()
    {
        if(itemType == ItemType.Food)
        {
            PlayerHunger playerHunger = GameObject.Find("Sliders").GetComponent<PlayerHunger>();
            playerHunger.ChangeHungerValue(amountToChangeStat);
            return true;
        }
        else
        {
            Debug.Log("Not usable");
            return false;
        }
    }

    public enum ItemType
    {
        None,
        Food
    }
}
