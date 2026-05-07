using LevelObjects.Interactable;
using Player;
using Save;
using UnityEngine;
using UnityEngine.UI;

public class ShopMenu : Interactable
{
    [SerializeField] private GameObject shopUI;
    [SerializeField] Button button;
    [SerializeField] private Item item;

    private int price;
    private bool shopOpen = false;

    public int Price { set { price = value; } }
    public Item Item {  set  { item = value; } }

    private InventoryManager inventoryManager;
    private MoneyManager moneyManager;

    protected override void Awake()
    {
        base.Awake();

        shopUI.SetActive(shopOpen);
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
        moneyManager = GameObject.Find("MoneyManager").GetComponent<MoneyManager>();
    }

    public override void Interact()
    {
        var player = PlayerManager.Instance?.GetPlayer();
        var movement = player?.GetComponent<MovementScript>();

        if (!shopOpen)
        {
            shopUI.SetActive(true);
            shopOpen = true;
            movement?.FreezeMovement(true);
        }
        else if (shopOpen)
        {
            shopUI.SetActive(false);
            shopOpen = false;
            movement?.FreezeMovement(false);
        }
    }



    //private void OnTriggerStay2D(Collider2D collision)
    //{
    //    if(collision.gameObject.tag == "Player")
    //    {
    //        if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Interact")))
    //        {

    //            if (!isActive)
    //            { 
    //                shopUI.SetActive(true);
    //                isActive = true;
    //            }
    //            else if (isActive)
    //            {
    //                shopUI.SetActive(false);
    //                isActive = false;

    //            }
    //        }
    //    }
    //}

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

    public override void LoadState(InteractableObjectState state)
    {
        if (state == null || state.uniqueId != GetUniqueId()) return;

        shopOpen = state.isActive;        
    }

    public override InteractableObjectState SaveState()
    {
        return new InteractableObjectState
        {
            uniqueId = GetUniqueId(),
            isActive = shopOpen
        };
    }

}
