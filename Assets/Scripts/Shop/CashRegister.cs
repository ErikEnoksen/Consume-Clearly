// =============================================================================
// CashRegister.cs — Opens the inventory with selling functionality
// 
//
// PURPOSE:
//   When in the trigger the player can press the interact button.
//   Upon pressing the button the inventory will open with a sell button.
// =============================================================================
using UnityEngine;

public class CashRegister : MonoBehaviour
{
    [SerializeField]
    GameObject eKeyImage;
    InventoryManager inventoryManager;

    void Start()
    {
        inventoryManager = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
        if(eKeyImage != null) eKeyImage.SetActive(false);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag.Equals("Player"))
        {
            if (eKeyImage != null) eKeyImage.SetActive(true);

            if (Input.GetKeyDown(KeybindManager.Instance.GetKey("Interact")))
            {
                inventoryManager.SellingMenu();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (eKeyImage != null) eKeyImage.SetActive(false);
    }
}
