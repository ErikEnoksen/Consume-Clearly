using Items;
using System;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField]
    private string itemID; 
    [SerializeField]
    private string itemName;
    [SerializeField]
    private int quantity = 1;
    [SerializeField]
    private int maxStack = 10;
    [SerializeField]
    private Sprite sprite;

    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(itemID))
                GenerateId();
            return itemID;
        }
    }
    public string ItemName {  get { return itemName; } set { itemName = value; } }
    public int Quantity { get { return quantity; } set { quantity = value; } }
    public Sprite Sprite { get { return sprite; } set { sprite = value; } }
    public int MaxStack { get { return maxStack; } set { maxStack = value; } }

    [TextArea]
    [SerializeField]
    private string itemDescription;

    public string ItemDescription { get { return itemDescription; } set { itemDescription = value; } }

    private InventoryManager inventory;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventory = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemID) || itemID == "1")
            GenerateId();
    }

    public void Initialize(string itemName, int quantity, Sprite sprite, string itemDescription, int maxStack, string itemTag)
    {
        ItemName = itemName;
        itemID = null; // force GenerateId() to regenerate from the new name on next access
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

            if (Input.GetKey(KeybindManager.Instance.GetKey("Interact")))
            {
                int exceccItems = inventory.AddItem(itemID, itemName, quantity, sprite, itemDescription, maxStack, gameObject.tag);
                if (exceccItems <= 0)
                {
                    Destroy(gameObject);
                }
                else
                {
                    quantity = exceccItems;
                }
            }
        }
    }

    private void GenerateId()
    {
        //Just name
        itemID = $"{(string.IsNullOrEmpty(ItemName) ? "item" : ItemName)}";
    }
}
