using UnityEngine;
using Items;

public class Item : MonoBehaviour
{
    [SerializeField]
    private ItemObject itemObject;

    [SerializeField]
    private string itemName;
    [SerializeField]
    private int quantity = 1;
    [SerializeField]
    private int maxStack = 10;
    [SerializeField]
    private Sprite sprite;

    public string ItemName { get { return itemName; } set { itemName = value; } }
    public int Quantity { get { return quantity; } set { quantity = value; } }
    public Sprite Sprite { get { return sprite; } set { sprite = value; } }
    public int MaxStack { get { return maxStack; } set { maxStack = value; } }
    public string ItemId => itemObject != null ? itemObject.Id : itemName;

    [TextArea]
    [SerializeField]
    private string itemDescription;

    public string ItemDescription { get { return itemDescription; } set { itemDescription = value; } }

    private InventoryManager inventory;

    void Start()
    {
        inventory = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
    }

    public void Initialize(string itemName, int quantity, Sprite sprite, string itemDescription, int maxStack, string itemTag)
    {
        ItemName = itemName;
        Quantity = quantity;
        Sprite = sprite;
        ItemDescription = itemDescription;
        MaxStack = maxStack;
        tag = itemTag;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (Input.GetKey(KeyCode.F))
            {
                int excessItems = inventory.AddItem(ItemId, itemName, quantity, sprite, itemDescription, maxStack, gameObject.tag);
                if (excessItems <= 0)
                {
                    Destroy(gameObject);
                }
                else
                {
                    quantity = excessItems;
                }
            }
        }
    }
}