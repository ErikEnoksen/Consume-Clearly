using Assets.Scripts.Quests;
using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryMenu;
    private bool inventoryActive;
    public InventoryItem[] inventoryItems;

    public ItemSO[] itemSOs;

    // Update is called once per frame
    void Update()
    {
        //Listens for when q is pressed and opens or closes the inventory
        if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Inventory")) && !inventoryActive) 
        {
            inventoryMenu.SetActive(true);
            inventoryActive = true;
        }
       else if(Input.GetKeyDown(KeybindManager.Instance.GetKey("Inventory")) && inventoryActive)
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
            for (int j = 0; j < itemSOs.Length; j++)
            {
                if (itemSOs[j].itemName == itemName && itemSOs[j].itemType == ItemType.Money)
                {
                    itemSOs[j].UseItem();
                    return 0;
                } 
                
            }
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

    public bool UseItem(string itemName)
    {
        for (int i = 0; i < itemSOs.Length; i++)
        {
            if (itemSOs[i].itemName == itemName)
            {
                bool usable = itemSOs[i].UseItem();
                return usable;
            }
        }
       
        return false; 

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

    public int LookForGift(string itemName, int affectionIncrease)
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (inventoryItems[i].itemName == itemName)
            {
                inventoryItems[i].RemoveItem(1);
                return affectionIncrease;
            }
        }
        Debug.Log("looked for gift");
        return 0;
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