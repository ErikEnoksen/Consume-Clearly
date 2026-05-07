using UnityEngine;
using UnityEngine.UI;

public class ShopMenu : MonoBehaviour
{
    [SerializeField] private GameObject shopUI;
    [SerializeField] Button button;
    [SerializeField] private Item item;
    private int price;
    private bool isActive = false;

    public int Price { set { price = value; } }
    public Item Item {  set  { item = value; } }

    private Image image;

    private ShopItem shopItem; 
    private InventoryManager inventoryManager;
    private MoneyManager moneyManager;

    private void Awake()
    {
        shopUI.SetActive(isActive);
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
        moneyManager = GameObject.Find("MoneyManager").GetComponent<MoneyManager>();
        image = GameObject.Find("ItemImage").GetComponent<Image>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Interact")))
            {

                if (!isActive)
                { 
                    shopUI.SetActive(true);
                    isActive = true;
                }
                else if (isActive)
                {
                    shopUI.SetActive(false);
                    isActive = false;

                }
            }
        }
    }

    public void BuyItem(Item item, int price)
    {
        if (moneyManager.ChangeMoneyAmount(-price))
        {
            inventoryManager.AddItem(item);
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }

    public void BuyThisItem()
    {
        BuyItem(item, price);
    }


}
