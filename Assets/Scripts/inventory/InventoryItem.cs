using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItem : MonoBehaviour, IPointerClickHandler
{
    public string itemID;
    public string itemName;
    public int quantity;
    public Sprite sprite;
    public bool isFull;
    [TextArea]
    public string itemDescription;
    public int maxStack;

    [SerializeField]
    private TMP_Text quantityText;
    [SerializeField]
    private Image itemImage;

    [SerializeField]
    private GameObject eKeySprite;

    public GameObject selectedShaders;
    public bool thisItemSelected;

    public Image infoImage;
    public TMP_Text itemDescriptionTitle;
    public TMP_Text itemDescriptionText;
    public GameObject foodButton;

    private InventoryManager inventoryManager;

    private void Start()
    {
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
        foodButton.SetActive(false);
    }


    //method for adding items to the inventory
    public int AddItem(Item item, int quantity)
    {
        if (isFull)
        {
            return item.Quantity;
        }
        itemID = item.Id;

        //updates the slot in the inventory to make the data visible in the inventory
        itemName = item.ItemName;
        sprite = item.Sprite;
        itemDescription = item.ItemDescription;
        maxStack = item.MaxStack;
        tag = item.tag;
        eKeySprite = item.EKeySprite;
        
        //SetActive makes the item and item count visible in the inventory
        if (itemImage != null) { itemImage.sprite = item.Sprite; itemImage.enabled = true; }

        //checks if the amount of items in the slot and sees if there is space for the rest
        this.quantity += quantity;
        if(this.quantity >= item.MaxStack)
        {
            if (quantityText != null) { quantityText.text = item.MaxStack.ToString(); quantityText.enabled = true; }
            isFull = true;

            //return excess items
            int excessItems = this.quantity - item.MaxStack;
            this.quantity = item.MaxStack;
            return excessItems;
        }

        //updates the view to the itemslot if the spot is not full yet
        if (quantityText != null) { quantityText.text = this.quantity.ToString(); quantityText.enabled = true; }
        return 0;
        
    }

    public int RemoveItem(int quantity)
    {
        if (quantity < this.quantity)
        {
            this.quantity -= quantity;
            if (quantityText != null) quantityText.text = this.quantity.ToString();
            return 0;
        }
        else if (quantity == this.quantity)
        {
            this.quantity = 0;
            EmptySlot();
            return 0;
        }
        else 
        {
            this.quantity = 0;
            EmptySlot();
            return quantity - this.quantity;
        }
}

    //listens for when the user clicks on an itemslot in the inventory and executes the relevant code
    public void OnPointerClick(PointerEventData eventData)
    {

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick();
        }
    }

    //highlights the selected itembox and deselects the previous selected spots
    public void OnLeftClick()
    {
        inventoryManager.DeselectAllSlots();
        selectedShaders.SetActive(true);
        infoImage.gameObject.SetActive(true);
        thisItemSelected = true;
        
        itemDescriptionTitle.text = itemName;
        inventoryManager.selectedItemName = itemName;
        itemDescriptionText.text = itemDescription;
        infoImage.sprite = itemImage.sprite;
        
        if (infoImage.sprite != null) infoImage.enabled = true;
        else infoImage.enabled = false; 
            
        foreach(ItemSO itemSO in inventoryManager.itemSOs)
        {
            if(itemSO.itemName == itemName)
            {
                if(itemSO.itemType == ItemType.Food)
                {
                    foodButton.SetActive(true);
                }
            }
        }
    }

    public void OnRightClick()
    {
        DropItem();
    }

    //creates a copy of the item in your inventory and creates it in the world as a dropped item
    public void DropItem()
    {
        if (quantity > 0)
        {
            GameObject itemToDrop = new GameObject(itemName);
            Item newItem = itemToDrop.AddComponent<Item>();

            eKeySprite.transform.SetParent(itemToDrop.transform);
            eKeySprite.transform.position = new Vector3(0, 1.2f, 0);

            newItem.Initialize(itemName, 1, sprite, itemDescription, maxStack, tag, eKeySprite);

            SpriteRenderer sr = itemToDrop.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;


            BoxCollider2D itemTrigger = itemToDrop.AddComponent<BoxCollider2D>();
            itemTrigger.isTrigger = true;
            itemTrigger.size = new Vector2(2f, 1f);

            itemToDrop.transform.position = GameObject.FindGameObjectWithTag("Player").transform.position - new Vector3(0f, 0.5f);

            quantity -= 1;
            quantityText.text = quantity.ToString();
            
            //removes the item from the slot if there are no more items
            if (quantity == 0)
            {
                EmptySlot();
            }
        }
    }

    public void RestoreSlot(Save.InventorySlotData data, Sprite sprite)
    {
        itemID = data.itemID;
        itemName = data.itemName;
        quantity = data.quantity;
        itemDescription = data.itemDescription;
        maxStack = data.maxStack;
        tag = data.itemTag;
        this.sprite = sprite;
        isFull = quantity >= maxStack;

        itemImage.sprite = sprite;
        itemImage.enabled = sprite != null;
        quantityText.text = quantity.ToString();
        quantityText.enabled = true;
    }

    //method that can be used when the count of an item reaches 0
    public void EmptySlot()
    {
        if (quantityText != null) { quantityText.enabled = false; quantityText.text = string.Empty; }
        if (inventoryManager != null) inventoryManager.selectedItemName = string.Empty;
        if (itemImage != null) { itemImage.enabled = false; itemImage.sprite = null; }
        if (itemDescriptionText != null) itemDescriptionText.text = null;
        if (itemDescriptionTitle != null) itemDescriptionTitle.text = null;
        if (infoImage != null) infoImage.sprite = null;

        itemName = null;
        itemID = null;
        itemDescription = null;
        sprite = null;
        tag = "Untagged";
        isFull = false;
    }
}
