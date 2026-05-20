using LevelObjects.Interactable;
using Player;
using Save;
using UnityEngine;
using UnityEngine.UI;

public class ShopMenu : Interactable
{
    [SerializeField] 
    private GameObject shopUI;
    [SerializeField] 
    Button button;
    private Item item;

    private int price;
    private bool shopOpen = false;

    public int Price { set { price = value; } }
    public Item Item {  set  { item = value; } }
    public int CSsubstractAmount = 10;

    private InventoryManager inventoryManager;
    private MoneyManager moneyManager;
    private CircularSatisfactionMeter satisfactionMeter;

    protected override void Awake()
    {
        base.Awake();
        satisfactionMeter = GameObject.Find("CircularSatisfactionMeter").GetComponent<CircularSatisfactionMeter>();
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

    public void BuyItem(Item item, int price)
    {
        if (moneyManager.ChangeMoneyAmount(-price))
        {
            inventoryManager.AddItem(item, 1);
            // Decreases circular satisfaction when buying 
            satisfactionMeter.DecreaseSatisfactionValue(CSsubstractAmount);
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
