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

    public int AddItem(string itemName, int quantity, Sprite sprite, string itemDescription, int maxStack)
    {
        //checks the slots of the inventory and selects the first empty one it finds to store the item
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if (!inventoryItems[i].isFull &&
                (inventoryItems[i].itemName == itemName || inventoryItems[i].quantity == 0)) 
            {
                int exceccItems = inventoryItems[i].AddItem(itemName, quantity, sprite, itemDescription, maxStack);
                if (exceccItems > 0)
                {
                    exceccItems = AddItem(itemName, exceccItems, sprite, itemDescription, maxStack);
                }
                return exceccItems;
            }
        }

        return quantity;
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
