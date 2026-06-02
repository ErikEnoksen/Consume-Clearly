using UnityEngine;

public class CashRegister : MonoBehaviour
{
    InventoryManager inventoryManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag.Equals("Player"))
        {
            if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Interact")))
            {
                inventoryManager.SellingMenu();
            }

        }
    }
}
