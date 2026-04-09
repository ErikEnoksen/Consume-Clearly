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

    public GameObject pressF;

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

        if(friendship != null)
        {
            Debug.Log($"DialogueTrigger on {gameObject.name} found companion: {friendship.gameObject.name}");
        }
        else
        {
            Debug.LogError($"DialogueTrigger on {gameObject.name} has no CompanionFriendship component!");
        }

        if (pressF != null)
        {
            pressF.SetActive(false);
        }
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

        dialogueManager.DisplayDialogue(dialogueToPlay, friendship);
        if (friendship != null)
            Debug.Log("Started conversation with: " + friendship.CurrentState);

        if (pressF != null)
        {
            pressF.SetActive(false);
        }
    }

    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
                if (pressF != null)
                {
                    pressF.SetActive(true);
            }

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

            if (pressF != null)
            {
                pressF.SetActive(false);
            }
        }
    }
}
