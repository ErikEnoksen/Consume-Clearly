using UnityEngine;

public class ShopItem : MonoBehaviour
{
    [SerializeField]
    private string itemName;
    [SerializeField]
    private int maxStack = 10;
    [SerializeField]
    private Sprite sprite;
    [SerializeField]
    private int price;

    [TextArea]
    [SerializeField]
    private string itemDescription;

    private InventoryManager inventoryManager;
    private MoneyManager moneyManager;

    private void Start()
    {
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
        moneyManager = GameObject.Find("MoneyManager").GetComponent <MoneyManager>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                BuyItem(itemName, sprite, itemDescription, maxStack, tag);
            }
        }
    }

    public void BuyItem(string itemName, Sprite sprite, string itemDescription, int maxStack, string tag)
    {
        if (moneyManager.ChangeMoneyAmount(-price))
        {
            inventoryManager.AddItem(itemName, 1, sprite, itemDescription, maxStack, tag);
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }

}
