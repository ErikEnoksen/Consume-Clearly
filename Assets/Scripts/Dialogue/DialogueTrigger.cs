// =============================================================================
// DialogueTrigger.cs — NPC Dialogue Stage Controller
// 
//
// PURPOSE:
//   Attach this to an NPC to give it a multi-stage conversation system.
//   Each stage holds a set of DialogueObjects that get swapped in/out
//   depending on the player's quest progress with that NPC.
//
// HOW STAGES WORK:
//   Stages are played in order (index 0 → 1 → 2 ...).
//   Each stage can show up to three different lines of dialogue:
//     • Default dialogue       — shown before any quest involvement
//     • In-progress dialogue   — shown while the linked quest is active
//     • Completed dialogue     — shown once all quest objectives are done
//   When a stage's advance condition is met, the trigger moves to the next stage.
//
// ADVANCE CONDITIONS (StageAdvanceCondition enum):
//   • AfterConversations  — moves on after N talks (set conversationsToAdvance)
//   • OnQuestComplete     — moves on once the linked quest is no longer active
//   • Manual              — only advances when AdvanceStage() is called in code
//
// TRIGGER TYPES:
//   • KeyPress    — player walks into range shows Dialogue prompt then presses F
//   • AutoTrigger — fires once automatically the first time the player enters range
//
// QUEST TURN-IN:
//   After a conversation ends, if the player just heard the "completed" dialogue
//   and the quest is flagged complete, the quest is automatically turned in here.
//
// SAVE / LOAD:
//   Implements ISaveable. Saves the current stage index, conversation count,
//   and the companion's friendship state so progress survives scene reloads.
//
// DEPENDENCIES:
//   Requires a CompanionFriendship component on the same GameObject.
//   Also expects a Dialogue manager and CommunityMeter to exist in the scene.
// =============================================================================

using UnityEngine;
using Assets.Scripts.Quests;
using Save;

// --- Stage Advance Condition ---
// Defines the three ways a dialogue stage can decide it's time to move on.
public enum StageAdvanceCondition
{
    AfterConversations,   // advance after Number conversations on this stage
    OnQuestComplete,      // advance when the stage's quest is no longer active
    Manual                // advance only when something else calls AdvanceStage()
}

// --- Dialogue Stage Data ---
// One entry in the NPC's conversation timeline. Holds all the dialogue variants
// for a given moment in the story, plus the rule for when to move to the next stage.
[System.Serializable]
public class DialogueStage
{
    // The fallback dialogue — always shown if no quest is assigned or quest hasn't started.
    [Tooltip("The dialogue to play at this stage (default / before quest is accepted)")]
    public DialogueObject dialogue;

    [Header("Quest-State Dialogue (optional):")]
    // If a quest is set here, the stage will swap dialogue based on that quest's live state.
    [Tooltip("Which quest to check state for. If unset, always uses the default dialogue above.")]
    public Quest questToCheck;

    [Tooltip("Shown while the quest is active but objectives are not yet complete. Falls back to default dialogue if empty.")]
    public DialogueObject questInProgressDialogue;

    [Tooltip("Shown when all quest objectives are complete (items gathered). Falls back to questInProgressDialogue, then default.")]
    public DialogueObject questCompletedDialogue;

    [Header("Stage Advancement:")]
    [Tooltip("How does the player progress past this stage?")]
    public StageAdvanceCondition advanceCondition = StageAdvanceCondition.AfterConversations;

    [Tooltip("How many times this dialogue must be played before advancing (only used with AfterConversations)")]
    public int conversationsToAdvance = 1;
}

public class DialogueTrigger : MonoBehaviour, ISaveable
{
    // Whether the player must press a key or if dialogue fires automatically on approach.
    public enum TriggerType { KeyPress, AutoTrigger }

    // --- Inspector Fields ---
    [Header("Dialogue Settings:")]
    [SerializeField] private DialogueStage[] dialogueStages;

    [Header("Interaction Setting")]
    [SerializeField] private TriggerType triggerType = TriggerType.KeyPress;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    // UI prompt shown above the NPC when the player is in range example Dialogue Prompt .
    public GameObject pressF;

    [Header("Save System")]
    [SerializeField] private string uniqueId;

    // --- Scene References (resolved at Start) ---
    private Dialogue dialogueManager;
    private CompanionFriendship friendship;
    private CommunityMeter communityMeter;
    private InstanceIdentifier instanceIdentifier;

    // --- Runtime State ---
    private bool isPlayerInRange = false;
    private bool hasAutoTriggered = false;   // prevents AutoTrigger from firing more than once

    private int currentStageIndex = 0;
    private int conversationsOnCurrentStage = 0;
    private DialogueObject lastPlayedDialogue; // tracked so we know which variant the player just saw

    // --- ID Generation ---
    // Ensures every NPC gets a stable unique ID for save/load even if none was set in the Inspector.
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueId))
            uniqueId = System.Guid.NewGuid().ToString();
    }

    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueId))
            uniqueId = System.Guid.NewGuid().ToString();
    }

    // --- Initialization ---
    // Grab scene references and subscribe to the global dialogue-ended event.
    // The pressF prompt starts hidden; it only shows when the player enters range.
    void Start()
    {
        dialogueManager = FindAnyObjectByType<Dialogue>();
        friendship = GetComponent<CompanionFriendship>();
        communityMeter = FindAnyObjectByType<CommunityMeter>();
        instanceIdentifier = GetComponent<InstanceIdentifier>();

        Dialogue.OnDialogueEndedCompanion += OnConversationEnded;

        if (friendship != null)
        {
            Debug.Log($"DialogueTrigger on {gameObject.name} found companion: {friendship.gameObject.name}");
        }
        else
        {
            Debug.LogError($"DialogueTrigger on {gameObject.name} has no CompanionFriendship component!");
        }

        if (pressF != null) pressF.SetActive(false);
    }

    // Unsubscribe from the event to avoid null-reference errors after the object is gone.
    void OnDestroy()
    {
        Dialogue.OnDialogueEndedCompanion -= OnConversationEnded;
    }

    // --- Input Polling ---
    // AutoTrigger is handled in OnTriggerEnter2D; this only runs for key-press NPCs.
    void Update()
    {
        if (dialogueManager == null) return;

        if (triggerType == TriggerType.KeyPress && isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            // Don't interrupt a conversation that's already playing.
            if (!dialogueManager.IsDialogueActive())
            {
                AudioManager.Instance.Play("Speaking");
                StartConversation();
            }
        }
    }

    // --- Conversation End Callback ---
    // Fired by the global Dialogue event after any companion conversation finishes.
    // We filter by companion so each NPC only reacts to its own conversations.
    private void OnConversationEnded(CompanionFriendship companion)
    {
        if (companion != friendship) return;

        conversationsOnCurrentStage++;

        // Auto turn-in: if the player just saw the quest-completed dialogue and
        // the quest is flagged done, hand it in without requiring another interaction.
        if (dialogueStages != null && currentStageIndex < dialogueStages.Length)
        {
            DialogueStage stage = dialogueStages[currentStageIndex];
            if (stage.questToCheck != null
                && stage.questCompletedDialogue != null
                && lastPlayedDialogue == stage.questCompletedDialogue
                && QuestController.Instance != null
                && QuestController.Instance.IsQuestCompleted(stage.questToCheck.questID))
            {
                QuestController.Instance.TurnInQuest(stage.questToCheck);
            }
        }

        TryAdvanceStage();
    }

    // --- Stage Advancement Logic ---
    // Called after every conversation. Checks the current stage's advance condition
    // and increments the stage index if the requirement is met.
    private void TryAdvanceStage()
    {
        if (dialogueStages == null || dialogueStages.Length == 0) return;
        if (currentStageIndex >= dialogueStages.Length) return;

        DialogueStage stage = dialogueStages[currentStageIndex];
        bool canAdvance = false;

        switch (stage.advanceCondition)
        {
            case StageAdvanceCondition.AfterConversations:
                canAdvance = conversationsOnCurrentStage >= stage.conversationsToAdvance;
                break;

            case StageAdvanceCondition.OnQuestComplete:
                // Falls back to the dialogue's own quest if no explicit questToCheck is set.
                Quest questForAdvance = stage.questToCheck ?? stage.dialogue?.quest;
                canAdvance = questForAdvance != null
                          && QuestController.Instance != null
                          && !QuestController.Instance.IsQuestActive(questForAdvance.questID);
                break;

            case StageAdvanceCondition.Manual:
                // Nothing triggers this — external code must call AdvanceStage() directly.
                canAdvance = false;
                break;
        }

        if (canAdvance && currentStageIndex < dialogueStages.Length - 1)
        {
            currentStageIndex++;
            conversationsOnCurrentStage = 0;
        }
    }

    // Public entry point for Manual advance — call this from cutscenes, events, etc.
    public void AdvanceStage()
    {
        if (dialogueStages == null || dialogueStages.Length == 0) return;
        if (currentStageIndex < dialogueStages.Length - 1)
        {
            currentStageIndex++;
            conversationsOnCurrentStage = 0;
        }
    }

    // --- Dialogue Selection ---
    // Picks which DialogueObject to play based on the current stage and live quest state.
    // Priority order: questCompleted → questInProgress → default.
    private DialogueObject GetCurrentDialogue()
    {
        if (dialogueStages == null || dialogueStages.Length == 0) return null;
        int index = Mathf.Clamp(currentStageIndex, 0, dialogueStages.Length - 1);
        DialogueStage stage = dialogueStages[index];
        if (stage == null) return null;

        if (stage.questToCheck != null && QuestController.Instance != null)
        {
            string questID = stage.questToCheck.questID;

            if (QuestController.Instance.IsQuestCompleted(questID))
                return stage.questCompletedDialogue ?? stage.questInProgressDialogue ?? stage.dialogue;

            if (QuestController.Instance.IsQuestActive(questID))
                return stage.questInProgressDialogue ?? stage.dialogue;
        }

        return stage.dialogue;
    }

    // --- Start Conversation ---
    // Resolves the correct dialogue, hands it to the Dialogue manager, and hides the prompt UI.
    private void StartConversation()
    {
        DialogueObject dialogueToPlay = GetCurrentDialogue();
        if (dialogueManager == null || dialogueToPlay == null) return;

        lastPlayedDialogue = dialogueToPlay;
        dialogueManager.DisplayDialogue(dialogueToPlay, friendship);

        if (pressF != null) pressF.SetActive(false);
    }

    // --- ISaveable Implementation ---
    public string GetUniqueId() => uniqueId;

    // Packages all runtime progress into a snapshot for the save system.
    public InteractableObjectState SaveState()
    {
        var state = new InteractableObjectState
        {
            uniqueId = uniqueId,
            isActive = hasAutoTriggered,
            dialogueStageIndex = currentStageIndex,
            conversationsOnStage = conversationsOnCurrentStage
        };
        if (friendship != null)
        {
            state.friendshipLevel = friendship.CurrentFriendshipLevel;
            state.friendshipMood = (int)friendship.CurrentMood;
            state.dailyConversationGiven = friendship.DailyConversationGiven;
        }
        return state;
    }

    // Restores a previously saved snapshot — clamped so a removed stage doesn't cause an out-of-bounds.
    public void LoadState(InteractableObjectState state)
    {
        int maxIndex = (dialogueStages != null && dialogueStages.Length > 0) ? dialogueStages.Length - 1 : 0;
        currentStageIndex = Mathf.Clamp(state.dialogueStageIndex, 0, maxIndex);
        conversationsOnCurrentStage = state.conversationsOnStage;
        hasAutoTriggered = state.isActive;
        if (friendship != null)
            friendship.LoadFriendshipState(state.friendshipLevel, state.friendshipMood, state.dailyConversationGiven);
    }

    // --- Trigger Area ---
    // Shows the Dialogue prompt when the player enters, hides it when they leave.
    // Also fires AutoTrigger conversations on first entry.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = true;
        if (pressF != null) pressF.SetActive(true);

        if (dialogueManager != null
            && triggerType == TriggerType.AutoTrigger
            && !dialogueManager.IsDialogueActive()
            && !hasAutoTriggered)
        {
            StartConversation();
            hasAutoTriggered = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        isPlayerInRange = false;
        if (pressF != null) pressF.SetActive(false);
    }
}