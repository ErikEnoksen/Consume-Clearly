using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemTest : MonoBehaviour, IPointerClickHandler
{
    public string itemName;
    public int quantity;
    public Sprite sprite;
    public bool isFull;
    public string itemDescription;

    [SerializeField]
    private int maxStack;

    [SerializeField]
    private TMP_Text quantityText;
    [SerializeField]
    private Image itemImage;

    public GameObject selectedShaders;
    public bool thisItemSelected;

    public Image infoImage;
    public TMP_Text itemDescriptionTitle;
    public TMP_Text itemDescriptionText;

    
    private InventoryManagerTest inventoryManager;

    private void Start()
    {
        inventoryManager = GameObject.Find("InventortySelector").GetComponent<InventoryManagerTest>();
    }


    //method for adding items to the inventory
    public int AddItem(string itemName, int quantity, Sprite sprite, string itemDescription)
    {
        if (isFull)
        {
            return quantity;
        }

        //updates the slot in the inventory to make the data visible in the inventory
        this.itemName = itemName;
        this.sprite = sprite;
        this.itemDescription = itemDescription;
        
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
            this.quantity = excessItems;
            return excessItems;
        }

        //updates the view to the itemslot if the spot is not full yet
        quantityText.text = this.quantity.ToString();
        quantityText.enabled = true;
        return 0;
        
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

    public void EmptySlot()
    {
        quantity = 0;
        quantityText.enabled = false;
        quantityText.text = string.Empty;
        
        itemImage.enabled = false;
        itemImage.sprite = null;
        itemName = string.Empty;
        itemDescription = string.Empty;

        isFull = false;
    }

    
    public void OnLeftClick()
    {
        inventoryManager.DeselectAllSlots();
        selectedShaders.SetActive(true);
        infoImage.gameObject.SetActive(true);
        thisItemSelected = true;
        itemDescriptionTitle.text = itemName;
        itemDescriptionText.text = itemDescription;
        infoImage.sprite = itemImage.sprite;
        if (infoImage.sprite != null)
        {
            infoImage.enabled = true;
        }
        else
        {
            infoImage.enabled = false;
        }
    }

    public void OnRightClick()
    {
        EmptySlot();
    }
}
