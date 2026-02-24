using UnityEngine;

public class ItemTest : MonoBehaviour
{
    [SerializeField]
    private string itemName;
    [SerializeField]
    private int quantity;
    [SerializeField]
    private int maxStack;
    [SerializeField]
    private Sprite sprite;

    public string ItemName {  get { return itemName; } set { itemName = value; } }
    public int Quantity { get { return quantity; } set { quantity = value; } }
    public Sprite Sprite { get { return sprite; } set { sprite = value; } }
    public int MaxStack { get { return maxStack; } set { maxStack = value; } }

    [TextArea]
    [SerializeField]
    private string itemDescription;

    public string ItemDescription { get { return itemDescription; } set { itemDescription = value; } }

    private InventoryManagerTest inventory;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventory = GameObject.Find("InventortySelector").GetComponent<InventoryManagerTest>();
    }

    public void Initialize(string itemName, int quantity, Sprite sprite, string itemDescription, int maxStack)
    {
        ItemName = itemName;
        Quantity = quantity;
        Sprite = sprite;
        ItemDescription = itemDescription;
        MaxStack = maxStack;
    }

    private void OnTriggerStay2D(Collider2D other)
    {

        if (other.gameObject.tag == "Player")
        {

            if (Input.GetKey(KeyCode.F))
            {
                int exceccItems = inventory.AddItem(itemName, quantity, sprite, itemDescription, maxStack);
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

}
