using System;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    //swith between f and autotrigger dialogue
    public enum TriggerType
    {
        KeyPress,
        AutoTrigger
    }

    [Header("Dialogue Settings:")]
    [Tooltip("The Dialogue that will play when interacting with npc")]
    [SerializeField] private DialogueObject dialogueToPlay;
    
    [Header("Interaction Setting")]
    [Tooltip("Key Press to interact with npc")]
    [SerializeField] private TriggerType triggerType = TriggerType.KeyPress;
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    
    private Dialogue dialogueManager;
    private FriendshipBar friendshipBar;
    private CompanionFriendship friendship;
    private InventoryManager inventory; 
    private CommunityMeter communityMeter;

    private bool isPlayerInRange = false;
    private bool hasAutoTriggered = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dialogueManager = FindObjectOfType<Dialogue>();
        friendshipBar = FindObjectOfType<FriendshipBar>();
        friendship = GetComponent<CompanionFriendship>();
        inventory = FindObjectOfType<InventoryManager>();
            communityMeter = FindObjectOfType<CommunityMeter>();

    }
    
    // Update is called once per frame
    void Update()
    {
        if (dialogueManager == null)
        {
            return;
        }

        if (triggerType == TriggerType.KeyPress && isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            Debug.Log("F pressed, dialogue active: " + dialogueManager.IsDialogueActive());
            if (!dialogueManager.IsDialogueActive())
            {
                AudioManager.Instance.Play("Speaking");
                StartConversation();
            }
        }
    }

    private void StartConversation()
    {
        if (dialogueManager == null || dialogueToPlay == null) return;

        if (communityMeter != null)
        {
            communityMeter.IncreaseCommunityLevel(100); // Example: Increase community level by 10 points
        }

        // Show friendship UI (if available) and subscribe to the dialogue end event once.
        if (friendship != null && friendshipBar != null)
        {
            friendshipBar.ShowFriendshipBar(friendship, dialogueToPlay);
            //friendship.GiveDailyConversationBonus();


            if (inventory != null)
            {
                int affectionIncrease = inventory.LookForGift("Rope", 50);
                friendship.IncreaseFriendship(affectionIncrease);
            }
        }
        dialogueManager.DisplayDialogue(dialogueToPlay);
        if (friendship != null)
            Debug.Log("Started conversation with: " + friendship.CurrentState);
    }

    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;

            if (dialogueManager != null && triggerType == TriggerType.AutoTrigger && !dialogueManager.IsDialogueActive() && !hasAutoTriggered)
            {
                StartConversation();
                hasAutoTriggered = true;
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }
}
