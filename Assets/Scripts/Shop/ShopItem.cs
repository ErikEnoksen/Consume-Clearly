using UnityEngine;

public class ShopItem : MonoBehaviour
{
    [SerializeField]
    private Item item;
    [SerializeField]
    private int price;

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
                BuyItem(item);
            }
        }
    }

    public void BuyItem(Item item)
    {
        if (moneyManager.ChangeMoneyAmount(-price))
        {
            inventoryManager.AddItem(item.ItemName, item.ItemName, 1, item.Sprite, item.ItemDescription, item.MaxStack, item.gameObject.tag);
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }

}
