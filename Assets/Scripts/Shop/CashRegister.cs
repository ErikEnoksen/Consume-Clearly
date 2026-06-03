using UnityEngine;
using UnityEngine.UI;

public class CashRegister : MonoBehaviour
{
    [SerializeField]
    GameObject eKeyImage;
    InventoryManager inventoryManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
