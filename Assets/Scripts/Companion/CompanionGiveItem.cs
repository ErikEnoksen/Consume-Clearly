using System.Linq;
using UnityEngine;
using Inventory;
using Items;

public class CompanionGiveItem : MonoBehaviour
{
    [Header("Item Data")]
    public ItemObject itemObject;           // Assign item to be given here
    public int quantity = 1;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.T; // kept for testing, but main trigger is now dialogue end event

    private bool playerInRange = false;
    private bool hasGivenItem = false;      // ensures item(s) are only given once

    private InventoryManagerTest inventoryManagerTest;


    private void Start()
    {

        // fallback to test manager (existing behavior)
        inventoryManagerTest = GameObject.Find("InventortySelector")?.GetComponent<InventoryManagerTest>();
        if (inventoryManagerTest == null)
        {
            Debug.LogWarning("No InventoryManagerTest found in scene.");
        }

        // Subscribe to dialogue end event
        Dialogue.OnDialogueEnded += OnDialogueEnded;
    }

    private void OnDestroy()
    {
        Dialogue.OnDialogueEnded -= OnDialogueEnded;
    }

    private void Update()
    {
        // keep old manual input behavior if desired (optional)
        if (!hasGivenItem && playerInRange && inventoryManagerTest != null && Input.GetKeyDown(interactKey))
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

    private void OnDialogueEnded()
    {
        // When any dialogue ends, give item if player is still in range
        if (playerInRange && !hasGivenItem)
        {
            GiveItem();
        }
    }

    private void GiveItem()
    {
        if (hasGivenItem) return;

        if (inventoryManagerTest != null)
        {
            if (itemObject == null)
            {
                Debug.LogWarning("No ItemObject assigned and cannot add ItemObject");
            }

            // assign item data from item object if available
            int leftover = inventoryManagerTest.AddItem(
                itemObject != null ? itemObject.name : "UnknownItem",
                quantity,
                itemObject != null ? itemObject.ItemImage : null,
                itemObject != null ? itemObject.Description : "",
                itemObject != null ? itemObject.MaxStackSize : 1
            );

            if (leftover == 0)
            {
                hasGivenItem = true;
                Debug.Log("Companion gave item successfully");
            }
            else
            {
                Debug.Log("Not enough inventory space.");
            }

            return;
        }

        Debug.LogWarning("No inventory system found to give item.");
    }
}