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
    public string itemDescription;
    private int maxStack;
    //replace with friendship mechanics later
    private int affectionLevel;

    [SerializeField]
    private TMP_Text quantityText;
    [SerializeField]
    private Image itemImage;

    public GameObject selectedShaders;
    public bool thisItemSelected;

    public Image infoImage;
    public TMP_Text itemDescriptionTitle;
    public TMP_Text itemDescriptionText;

    
    private InventoryManager inventoryManager;

    private void Start()
    {
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
    }

    public void GiveGift(int affectionIncrease)
    {
        if(CompareTag("Gift"))
        {
            affectionLevel += affectionIncrease;
            RemoveItem(1);
            Debug.Log(affectionLevel);
        }
        else
        {
            Debug.Log("Not a gift");
        }
    }


    //method for adding items to the inventory
    public int AddItem(string itemID, string itemName, int quantity, Sprite sprite, string itemDescription, int maxStack, string tag)
    {
        if (isFull)
        {
            return quantity;
        }
        this.itemID = itemID;

        //updates the slot in the inventory to make the data visible in the inventory
        this.itemName = itemName;
        this.sprite = sprite;
        this.itemDescription = itemDescription;
        this.maxStack = maxStack;
        this.tag = tag;
        
        //SetActive makes the item and item count visible in the inventory
        itemImage.sprite = sprite;
        itemImage.enabled = true;
        
        //checks if the amount of items in the slot and sees if there is space for the rest
        this.quantity += quantity;
        if(this.quantity > maxStack)
        {
            quantityText.text = maxStack.ToString();
            quantityText.enabled = true;
            isFull = true;
            
            //return excess items
            int excessItems = this.quantity - maxStack;
            this.quantity = maxStack;
            return excessItems;
        }

        //updates the view to the itemslot if the spot is not full yet
        quantityText.text = this.quantity.ToString();
        quantityText.enabled = true;
        return 0;
        
    }

    public int RemoveItem(int quantity)
    {
        if (quantity < this.quantity)
        {
            this.quantity -= quantity;
            quantityText.text = this.quantity.ToString();
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
        if (thisItemSelected)
        {
            bool usable = inventoryManager.UseItem(itemName);

            if (usable)
            {
                quantity -= 1;
                quantityText.text = quantity.ToString();
            
                if(quantity == 0)
                {
                    EmptySlot();
                }
            }
        }
        else
        {
            inventoryManager.DeselectAllSlots();
            selectedShaders.SetActive(true);
            infoImage.gameObject.SetActive(true);
            thisItemSelected = true;
        
            itemDescriptionTitle.text = itemName;
            itemDescriptionText.text = itemDescription;
            infoImage.sprite = itemImage.sprite;
        
            if (infoImage.sprite != null) infoImage.enabled = true;
            else infoImage.enabled = false; 
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

            newItem.Initialize(itemName, 1, sprite, itemDescription, maxStack, tag);

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

    //method that can be used when the count of an item reaches 0
    private void EmptySlot()
    {
        quantityText.enabled = false;
        quantityText.text = string.Empty;
        
        itemImage.enabled = false;
        itemImage.sprite = null;
        itemName = null;
        itemDescription = null;
        tag = "Untagged";

        isFull = false;

        itemDescriptionText.text = null;
        itemDescriptionTitle.text = null;
        infoImage.sprite = null;
    }
}
