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

    [SerializeField]
    private TMP_Text quantityText;
    [SerializeField]
    private Image itemImage;

    public GameObject selectedShaders;
    public bool thisItemSelected;

    public Image infoImage;
    
    private InventoryManagerTest inventoryManager;

    private void Start()
    {
        inventoryManager = GameObject.Find("InventortySelector").GetComponent<InventoryManagerTest>();
    }


    //method for adding items to the inventory
    public void AddItem(string itemName, int quantity, Sprite sprite)
    {
        this.itemName = itemName;
        this.quantity = quantity;
        this.sprite = sprite;
        isFull = true;

        //SetActive makes the item and item count visible in the inventory
        quantityText.text = quantity.ToString();
        quantityText.gameObject.SetActive(true);
        itemImage.sprite = sprite;
        itemImage.gameObject.SetActive(true);
    }

    //method to allow item stacking in the inventory
    public void StackItem(int quantity)
    {
        this.quantity += quantity;
        quantityText.text = this.quantity.ToString();
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

    
    public void OnLeftClick()
    {
        inventoryManager.DeselectAllSlots();
        selectedShaders.SetActive(true);
        thisItemSelected = true;
        if (sprite != null)
        {
            infoImage.sprite = sprite;
            infoImage.gameObject.SetActive(true);
        }
    }

    public void OnRightClick()
    {

    }


}
