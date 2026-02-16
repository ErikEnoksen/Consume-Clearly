using System;
using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    
    [Header("Dialogue Settings:")]
    [Tooltip("The Dialogue that will play when interacting with npc")]
    [SerializeField] private DialogueObject dialogueToPlay;
    
    [Header("Interaction Setting")]
    [Tooltip("Key Press to interact with npc")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    
    [Tooltip("Reference to the dialogue Manager")]
    [SerializeField] private Dialogue dialogueManager;

    private bool isPlayerInRange = false;
    private bool isDialogueActive = false;
    
    // Update is called once per frame
    void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(interactKey) && !isDialogueActive)
        {
            StartConversation();
        }
    }

    private void StartConversation()
    {
        if (dialogueManager != null && dialogueToPlay != null)
        {
            isDialogueActive = true;
            dialogueManager.DisplayDialogue(dialogueToPlay);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            //Could show interaction prompt UI here
        }
    }

    private void onTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            isDialogueActive = false;
            // Optional: Hide interaction prompt UI here
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


}
