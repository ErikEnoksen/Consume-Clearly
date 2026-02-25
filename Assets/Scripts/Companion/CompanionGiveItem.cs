using UnityEngine;

public class CompanionGiveItem : MonoBehaviour
{
    [Header("Item Data")]
    public string itemName;
    public int quantity = 1;
    public Sprite sprite;
    [TextArea] public string itemDescription;
    public int maxStack = 1;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.T;

    private bool playerInRange = false;
    private bool hasGivenItem = false;

    private InventoryManagerTest inventoryManager;

    private void Start()
    {
        inventoryManager = GameObject
            .Find("InventorySelector")
            .GetComponent<InventoryManagerTest>();
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            GiveItem();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    private void GiveItem()
    {
        if (hasGivenItem) return;

        int leftover = inventoryManager.AddItem(
            itemName,
            quantity,
            sprite,
            itemDescription,
            maxStack
        );

        if (leftover == 0)
        {
            hasGivenItem = true;
            Debug.Log("Companion gave item successfully.");

            // Optional: disable companion after giving
            // gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Not enough inventory space!");
        }
    }
}