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
                    QuestController.Instance.UpdateObjectiveProgress(itemID, quantity);
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
