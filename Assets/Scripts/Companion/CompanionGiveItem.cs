using System.Linq;
using UnityEngine;
using Inventory;
using Items;


public class CompanionGiveItem : MonoBehaviour
{
    [Header("Item Data")]
    public Item itemObject;           // Assign item to be given here
    public int quantity = 1;

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.T; // kept for testing, but main trigger is now dialogue end event

    private bool playerInRange = false;
    private bool hasGivenItem = false;      // ensures item(s) are only given once

    private InventoryManager inventoryManager;


    private void Start()
    {

        // fallback to test manager (existing behavior)
        inventoryManager = GameObject.Find("InventorySelector")?.GetComponent<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogWarning("No InventoryManager found in scene.");
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
        if (!hasGivenItem && playerInRange && inventoryManager != null && Input.GetKeyDown(interactKey))
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

    private void OnDialogueEnded(DialogueObject endedDialogue)
    {
        // When any dialogue ends give item if player is still in range
        if (playerInRange && !hasGivenItem)
        {
            GiveItem();
        }
    }

    private void GiveItem()
    {
        if (hasGivenItem) return;

        if (inventoryManager != null)
        {
            if (itemObject == null)
            {
                Debug.LogWarning("No ItemObject assigned and cannot add ItemObject");
            }

            // assign item data from item object if available
            int leftover = inventoryManager.AddItem(
                itemObject != null ? itemObject.ItemName : "UnknownItem",
                quantity,
                itemObject != null ? itemObject.Sprite : null,
                itemObject != null ? itemObject.ItemDescription : "",
                itemObject != null ? itemObject.MaxStack : 1,
                itemObject != null ? itemObject.tag : "Untagged",
                itemObject != null ? itemObject.GiftValue : 20
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