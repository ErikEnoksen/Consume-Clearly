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

    private bool isPlayerInRange = false;
    private bool isDialogueActive = false;
    private bool hasAutoTriggered = false;
    private bool dailyConversation = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        dialogueManager = FindObjectOfType<Dialogue>();
        friendshipBar = FindObjectOfType<FriendshipBar>();
        friendship = GetComponent<CompanionFriendship>();

    }
    
    // Update is called once per frame
    void Update()
    {
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

        // Show friendship UI (if available) and subscribe to the dialogue end event once.
        if (friendship != null && friendshipBar != null)
        {
            friendshipBar.ShowFriendshipBar(friendship, dialogueToPlay);
            if (!dailyConversation)
            {
                friendship.IncreaseFriendship(100);// Example: Increase friendship by 100 points
                dailyConversation = true;
            }
        }


        dialogueManager.DisplayDialogue(dialogueToPlay);
    }

    

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger entered by: " + other.gameObject.name + " Tag: " + other.tag);
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;

            if (triggerType == TriggerType.AutoTrigger && !dialogueManager.IsDialogueActive()&& hasAutoTriggered == false)
            {
                StartConversation();
                hasAutoTriggered = true;
            }
        }

        //if(triggerType == TriggerType.AutoTrigger && other.CompareTag("Player") && !dialogueManager.IsDialogueActive())
        //{
        //    StartConversation();
        //}
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            isDialogueActive = false;
            // Optional: Hide interaction prompt UI here
        }
    }



}
