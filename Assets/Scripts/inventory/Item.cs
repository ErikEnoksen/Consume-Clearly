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
    [SerializeField]
    private int giftValue;

    [SerializeField]
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
    public int GiftValue { get { return giftValue; } set { giftValue = value; } }

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

    public void Initialize(string itemName, int quantity, Sprite sprite, string itemDescription, int maxStack, int giftValue)
    {
        ItemName = itemName;
        Quantity = quantity;
        Sprite = sprite;
        ItemDescription = itemDescription;
        MaxStack = maxStack;
        GiftValue = giftValue;
    }

    private void OnTriggerStay2D(Collider2D other)
    {

        if (other.gameObject.tag == "Player")
        {

            if (Input.GetKey(KeyCode.F))
            {
                int exceccItems = inventory.AddItem(itemName, quantity, sprite, itemDescription, maxStack, gameObject.tag, giftValue);
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
        // Keep IDs short but unique: name + GUID fragment
        itemID = $"{(string.IsNullOrEmpty(ItemName) ? "item" : ItemName)}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
    }
}
