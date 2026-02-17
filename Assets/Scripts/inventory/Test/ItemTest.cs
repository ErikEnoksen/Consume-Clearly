using UnityEngine;
using UnityEngine.UI;

public class ItemTest : MonoBehaviour
{
    [SerializeField]
    private string itemName;
    [SerializeField]
    private int quantity;
    [SerializeField]
    private Sprite sprite;

    [TextArea]
    [SerializeField]
    private string itemDescription;

    private InventoryManagerTest inventory;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventory = GameObject.Find("InventortySelector").GetComponent<InventoryManagerTest>();

    }


    private void OnTriggerStay2D(Collider2D other)
    {

        if (other.gameObject.tag == "Player")
        {

            if (Input.GetKey(KeyCode.F))
            {
                int exceccItems = inventory.AddItem(itemName, quantity, sprite, itemDescription);
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
