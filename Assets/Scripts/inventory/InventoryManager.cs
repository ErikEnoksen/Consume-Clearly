using System.Collections.Generic;
using Assets.Scripts.Quests;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public GameObject inventoryMenu;
    private bool inventoryActive;
    public bool giftingEnabled = false;
    public InventoryItem[] inventoryItems;
    public string selectedItemName;

    private CompanionFriendship companionFriendship;
    public ItemSO[] itemSOs;
    private Dictionary<string, Sprite> _spriteCache;

    public Button giftButton;

    // Update is called once per frame
    void Update()
    {
        //Listens for when q is pressed and opens or closes the inventory
        if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Inventory")))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        if (!inventoryActive)
        {
            inventoryMenu.SetActive(true);
            inventoryActive = true;
        }
        else if (inventoryActive)
        {
            inventoryMenu.SetActive(false);
            inventoryActive = false;
            giftingEnabled = false;
            giftButton.gameObject.SetActive(false);
            selectedItemName = string.Empty;
            DeselectAllSlots();
        }
    }

    public void GiftingMenu(CompanionFriendship companion)
    {
        inventoryMenu.SetActive(true);
        inventoryActive = true;
        giftingEnabled = true;
        companionFriendship = companion;
        giftButton.gameObject.SetActive(true);
    }

    public int AddItem(Item item, int quantity)
    {
        Debug.Log(item.Quantity);
        //checks the slots of the inventory and selects the first empty one it finds to store the item
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            for (int j = 0; j < itemSOs.Length; j++)
            {
                if (itemSOs[j].itemName == item.ItemName && itemSOs[j].itemType == ItemType.Money)
                {
                    itemSOs[j].UseItem();
                    return 0;
                } 
                
            }
             if (!inventoryItems[i].isFull &&
            (inventoryItems[i].itemName == item.ItemName || inventoryItems[i].quantity == 0)) 
            {
                int exceccItems = inventoryItems[i].AddItem(item, quantity);
                if (QuestController.Instance != null)
                {
                    int pickedUp = item.Quantity - exceccItems;
                    QuestController.Instance.UpdateObjectiveProgress(item.Id, objectiveType.CollectItem ,pickedUp);
                }
                if (exceccItems > 0)
                {
                    exceccItems = AddItem(item, quantity);
                }
                return exceccItems;
            }
        }

        return item.Quantity;
    }

    public bool UseItem(string itemName, bool button)
    {
        
        for (int i = 0; i < itemSOs.Length; i++)
        {
            if (itemSOs[i].itemName == itemName)
            {
                if ((itemSOs[i].itemType == ItemType.Gift && giftingEnabled) && button)
                {
                    bool usable = itemSOs[i].UseItem(companionFriendship);
                    return usable;
                }
                if ((itemSOs[i].itemType == ItemType.Gift && !giftingEnabled) || (itemSOs[i].itemType != ItemType.Gift && giftingEnabled))
                {
                    return false;
                }
                else
                {
                    bool usable = itemSOs[i].UseItem();
                    return usable;
                }
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
        
    public void GiftItem()
    { 
        bool usable = UseItem(selectedItemName, true);
        if (usable)
        {
            RemoveItem(selectedItemName, 1);
            ToggleInventory();
        }
    }

    public void EatFood()
    {
        bool usable = UseItem(selectedItemName, false);
        if (usable)
        {
            RemoveItem(selectedItemName, 1);
        }
    }

    public List<Save.InventorySlotData> SaveInventory()
    {
        var slots = new List<Save.InventorySlotData>();
        foreach (var slot in inventoryItems)
        {
            if (slot.quantity > 0)
            {
                slots.Add(new Save.InventorySlotData
                {
                    itemName = slot.itemName,
                    itemID = slot.itemID,
                    quantity = slot.quantity,
                    itemDescription = slot.itemDescription,
                    maxStack = slot.maxStack,
                    itemTag = slot.tag,
                    spriteName = slot.sprite != null ? slot.sprite.name : string.Empty,
                    cachedSprite = slot.sprite
                });
            }
        }
        return slots;
    }

    public void LoadInventory(List<Save.InventorySlotData> slots)
    {
        foreach (var slot in inventoryItems)
            slot.EmptySlot();

        int slotIndex = 0;
        foreach (var data in slots)
        {
            if (slotIndex >= inventoryItems.Length) break;

            Sprite sprite = data.cachedSprite;

            if (sprite == null && !string.IsNullOrEmpty(data.spriteName))
            {
                if (_spriteCache == null)
                {
                    _spriteCache = new Dictionary<string, Sprite>();
                    foreach (var s in Resources.FindObjectsOfTypeAll<Sprite>())
                        _spriteCache.TryAdd(s.name, s);
                }
                _spriteCache.TryGetValue(data.spriteName, out sprite);
            }

            inventoryItems[slotIndex].RestoreSlot(data, sprite);
            slotIndex++;
        }
    }

    //undoes the borders on the selected itemslot
    public void DeselectAllSlots()
    {
        for (int i = 0; i < inventoryItems.Length; i++)
        {
            inventoryItems[i].selectedShaders.SetActive(false);
            inventoryItems[i].thisItemSelected = false;
            inventoryItems[i].foodButton.SetActive(false);
        }
    }
}