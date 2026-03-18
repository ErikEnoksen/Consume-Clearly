using Assets.Scripts.Quests;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryMenu;
    private bool inventoryActive;
    public InventoryItem[] inventoryItems;

    // Update is called once per frame
    void Update()
    {
        //Listens for when q is pressed and opens or closes the inventory
       if(Input.GetKeyDown(KeyCode.Q) && !inventoryActive) 
        {
            inventoryMenu.SetActive(true);
            inventoryActive = true;
        }
       else if(Input.GetKeyDown(KeyCode.Q) && inventoryActive)
        {
            inventoryMenu.SetActive(false);
            inventoryActive = false;
        }
    }

    public int AddItem(string itemID,string itemName, int quantity, Sprite sprite, string itemDescription, int maxStack, string tag)
    {
        //checks the slots of the inventory and selects the first empty one it finds to store the item
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (!inventoryItems[i].isFull &&
                (inventoryItems[i].itemName == itemName || inventoryItems[i].quantity == 0)) 
            {
                int exceccItems = inventoryItems[i].AddItem(itemID, itemName, quantity, sprite, itemDescription, maxStack, tag);
                if (QuestController.Instance != null)
                {
                    int pickedUp = quantity - exceccItems;
                    QuestController.Instance.UpdateObjectiveProgress(itemID, pickedUp);
                }
                if (exceccItems > 0)
                {
                    exceccItems = AddItem(itemID, itemName, exceccItems, sprite, itemDescription, maxStack, tag);
                }
                return exceccItems;
            }
        }

        return quantity;
    }

    public bool RemoveItem(string itemID, int quantity)
    {
        int remaining = quantity;

        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i].itemID == itemID && inventoryItems[i].quantity > 0)
            {
                int removeAmount = Mathf.Min(remaining, inventoryItems[i].quantity);

                int excess = inventoryItems[i].RemoveItem(remaining);

                if (excess > 0)
                    inventoryItems[i].RemoveItem(excess);

                remaining -= removeAmount;

                if (remaining <= 0)
                    return true;
            }
        }

        return false;
    }

    public void LookForGift(string itemName, int affectionIncrease)
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i].itemName == itemName)
            {
                inventoryItems[i].GiveGift(affectionIncrease);
            }
        }
        Debug.Log("looked for gift");
    }

    //undoes the borders on the selected itemslot
    public void DeselectAllSlots()
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            inventoryItems[i].selectedShaders.SetActive(false);
            inventoryItems[i].thisItemSelected = false;
        }
    }
}
