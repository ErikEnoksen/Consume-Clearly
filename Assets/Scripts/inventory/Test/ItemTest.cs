using UnityEngine;

public class ItemTest : MonoBehaviour
{
    [SerializeField]
    private string itemName;
    [SerializeField]
    private int quantity;
    [SerializeField] 
    private int maxCount;
    [SerializeField]
    private Sprite sprite;

    private InventoryManagerTest inventory;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventory = GameObject.Find("InventortySelector").GetComponent<InventoryManagerTest>();
    }

    //checks if the player bumped into the item and stores it in the inventory
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            inventory.AddItem(itemName, quantity, sprite);
            Destroy(gameObject);
        }
    }

}
