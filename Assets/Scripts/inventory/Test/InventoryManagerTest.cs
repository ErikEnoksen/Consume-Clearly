using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryManagerTest : MonoBehaviour
{
    public GameObject inventoryMenu;
    private bool inventoryActive;
    public InventoryItemTest[] inventoryItems;

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

    public int AddItem(string itemName, int quantity, Sprite sprite, string itemDescription)
    {
        //checks the slots of the inventory and selects the first empty one it finds to store the item
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if(inventoryItems[i].isFull == false && inventoryItems[i].itemName == itemName || inventoryItems[i].quantity == 0)
            {
                int exceccItems = inventoryItems[i].AddItem(itemName, quantity, sprite, itemDescription);
                if (exceccItems > 0)
                {
                    exceccItems = AddItem(itemName, exceccItems, sprite, itemDescription);
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
