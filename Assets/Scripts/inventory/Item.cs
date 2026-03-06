using UnityEngine;
using Items;

public class Item : MonoBehaviour
{
    [SerializeField]
    private ItemObject itemObject; // assign the ScriptableObject for TNT prefab

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

    public string ItemId => itemObject != null ? itemObject.Id : itemName; // fallback to name
}
