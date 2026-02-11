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

    public void AddItem(string itemName, int quantity, Sprite sprite)
    {
        //checks the slots of the inventory and selects the first empty one it finds to store the item
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            if(inventoryItems[i].isFull == false)
            {
                inventoryItems[i].AddItem(itemName, quantity, sprite);
                
                return;
            }
            //checks if the item alreadey exists in the inventory and stacks it
            else if(inventoryItems[i].itemName == itemName)
            {
                inventoryItems[i].StackItem(quantity);
                return;
            }
            else
            {
                Debug.Log("Inventory is full");
            }
        }

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
