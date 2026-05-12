using UnityEngine;
using Assets.Scripts.Quests;
using Save;

public enum StageAdvanceCondition
{
    AfterConversations,   // advance after Number conversations on this stage
    OnQuestComplete,      // advance when the stage's quest is no longer active
    Manual                // advance only when something else calls AdvanceStage()
}

[System.Serializable]
public class DialogueStage
{
    [Tooltip("The dialogue to play at this stage")]
    public DialogueObject dialogue;

    [Tooltip("How does the player progress past this stage?")]
    public StageAdvanceCondition advanceCondition = StageAdvanceCondition.AfterConversations;

    [Tooltip("How many times this dialogue must be played before advancing (only used with AfterConversations)")]
    public int conversationsToAdvance = 1;
}

public class DialogueTrigger : MonoBehaviour, ISaveable
{
    public enum TriggerType { KeyPress, AutoTrigger }

    [Header("Dialogue Settings:")]
    [SerializeField] private DialogueStage[] dialogueStages;

    [Header("Interaction Setting")]
    [SerializeField] private TriggerType triggerType = TriggerType.KeyPress;
    [SerializeField] private KeyCode interactKey = KeyCode.F;

    public GameObject pressF;

    private Dialogue dialogueManager;
    private CompanionFriendship friendship;
    private CommunityMeter communityMeter;
    private InstanceIdentifier instanceIdentifier;

    private bool isPlayerInRange = false;
    private bool hasAutoTriggered = false;

    private int currentStageIndex = 0;
    private int conversationsOnCurrentStage = 0;

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

    void OnDestroy()
    {
        Dialogue.OnDialogueEndedCompanion -= OnConversationEnded;
    }

    void Update()
    {
        if (dialogueManager == null) return;

        if (triggerType == TriggerType.KeyPress && isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            if (!dialogueManager.IsDialogueActive())
            {
                AudioManager.Instance.Play("Speaking");
                StartConversation();
            }
        }
    }

    private void OnConversationEnded(CompanionFriendship companion)
    {
        if (companion != friendship) return;

        conversationsOnCurrentStage++;
        TryAdvanceStage();
    }

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
                canAdvance = stage.dialogue != null
                          && stage.dialogue.quest != null
                          && QuestController.Instance != null
                          && !QuestController.Instance.IsQuestActive(stage.dialogue.quest.questID);
                break;

            case StageAdvanceCondition.Manual:
                canAdvance = false;
                break;
        }

        if (canAdvance && currentStageIndex < dialogueStages.Length - 1)
        {
            currentStageIndex++;
            conversationsOnCurrentStage = 0;
        }
    }

    public void AdvanceStage()
    {
        if (dialogueStages == null || dialogueStages.Length == 0) return;
        if (currentStageIndex < dialogueStages.Length - 1)
        {
            currentStageIndex++;
            conversationsOnCurrentStage = 0;
        }
    }

    private DialogueObject GetCurrentDialogue()
    {
        if (dialogueStages == null || dialogueStages.Length == 0) return null;
        int index = Mathf.Clamp(currentStageIndex, 0, dialogueStages.Length - 1);
        return dialogueStages[index]?.dialogue;
    }

    private void StartConversation()
    {
        DialogueObject dialogueToPlay = GetCurrentDialogue();
        if (dialogueManager == null || dialogueToPlay == null) return;

        dialogueManager.DisplayDialogue(dialogueToPlay, friendship);

        if (pressF != null) pressF.SetActive(false);
    }

    public string GetUniqueId()
    {
        return instanceIdentifier != null ? instanceIdentifier.Id : null;
    }

    public InteractableObjectState SaveState()
    {
        if (instanceIdentifier == null) return null;
        var state = new InteractableObjectState
        {
            uniqueId = instanceIdentifier.Id,
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

    public void LoadState(InteractableObjectState state)
    {
        int maxIndex = (dialogueStages != null && dialogueStages.Length > 0) ? dialogueStages.Length - 1 : 0;
        currentStageIndex = Mathf.Clamp(state.dialogueStageIndex, 0, maxIndex);
        conversationsOnCurrentStage = state.conversationsOnStage;
        if (friendship != null)
            friendship.LoadFriendshipState(state.friendshipLevel, state.friendshipMood, state.dailyConversationGiven);
    }

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