using System.Collections.Generic;
using UnityEngine;
using Items;

public class CompanionGiveItem : MonoBehaviour
{
    public enum GiftTriggerType
    {
        OnDialogueEnd,
        OnFriendshipStateChanged,
        Weekly
    }

    [Header("Gifts")]
    public List<CompanionGift> gifts = new List<CompanionGift>();

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.T; // optional manual trigger

    private bool playerInRange = false;

    private InventoryManager inventoryManager;
    private CompanionFriendship friendship;

    private void Start()
    {
        inventoryManager = GameObject.Find("InventorySelector")?.GetComponent<InventoryManager>();
        if (inventoryManager == null)
        {
            Debug.LogWarning("No InventoryManager found in scene.");
        }

        friendship = GetComponent<CompanionFriendship>();

        if (friendship != null)
        {
            friendship.OnStateChanged += OnFriendshipStateChanged;
        }

        Dialogue.OnDialogueEnded += OnDialogueEnded;
    }

    private void OnDestroy()
    {
        Dialogue.OnDialogueEnded -= OnDialogueEnded;

        if (friendship != null)
        {
            friendship.OnStateChanged -= OnFriendshipStateChanged;
        }
    }

    private void Update()
    {
        // Optional manual trigger for testing
        if (playerInRange && inventoryManager != null && Input.GetKeyDown(interactKey))
        {
            TryGiveGift(GiftTriggerType.OnDialogueEnd);
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

    //give gift when dialogue ends
    private void OnDialogueEnded(DialogueObject endedDialogue)
    {
        TryGiveGift(GiftTriggerType.OnDialogueEnd);
    }

    //give gift when friendship state changes
    private void OnFriendshipStateChanged(CompanionFriendship.FriendshipState newState)
    {
        TryGiveGift(GiftTriggerType.OnFriendshipStateChanged);
    }

    // Core logic to check conditions and give gifts
    private void TryGiveGift(GiftTriggerType trigger)
    {
        if (!playerInRange || inventoryManager == null) return;

        foreach (var gift in gifts)
        {
            if (gift.triggerType != trigger)
                continue;

            if (gift.giveOnce && gift.hasBeenGiven)
                continue;
            // Check friendship state and checks off lower and the same state
            if (friendship != null && friendship.CurrentState <= gift.requiredState)
                continue;

            GiveItem(gift);
        }
    }

    // Handles the actual item giving logic
    private void GiveItem(CompanionGift gift)
    {
        if (gift.item == null) return;

        int leftover = inventoryManager.AddItem(gift.item);

        if (leftover == 0)
        {
            gift.hasBeenGiven = true;
            Debug.Log($"Companion gave: {gift.item.ItemName}");
        }
        else
        {
            Debug.Log("Not enough inventory space.");
        }
    }
}

// Serializable class to define gifts and their conditions
[System.Serializable]
public class CompanionGift
{
    public CompanionGiveItem.GiftTriggerType triggerType;

    public CompanionFriendship.FriendshipState requiredState;

    [Header("Item")]
    public Item item;
    public int quantity = 1;

    [Header("Behavior")]
    public bool giveOnce = true;

    [HideInInspector]
    public bool hasBeenGiven = false;
}