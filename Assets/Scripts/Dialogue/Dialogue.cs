// =============================================================================
// Dialogue.cs — Scene Dialogue Runner
//
//
// PURPOSE:
//   The single scene-wide manager that actually displays conversations.
//   DialogueTrigger tells it which DialogueObject to show and which companion
//   is talking; this class handles everything after that: typing animation,
//   player clicks, choice buttons, and eventually closing the box.
//
// FLOW:
//   DisplayDialogue() → StartDialogue() → TypeLine() (coroutine)
//     → CheckForChoices() → ShowChoices() or wait for click
//       → NextLine() or OnChoiceSelected() → ... → EndDialogue()
//
// CLICK BEHAVIOUR:
//   • While text is typing  : click skips to the full line instantly
//   • After text is done    : click advances to the next line
//   • When choices are shown: clicks are handled by the choice buttons, not here
//
// QUEST BRANCHING:
//   GetStartLineIndex / GetEndLineIndex read the DialogueObject's quest index
//   markers to play only the segment relevant to the current quest state.
//   This means a single DialogueObject asset can cover all three states.
//
// EVENTS (static, listened to by other systems):
//   • OnDialogueStarted(companion)     — fired when a conversation opens
//   • OnDialogueEnded(dialogueObject)  — fired when it closes
//   • OnDialogueEndedCompanion(companion) — same close event, companion-keyed
//     (DialogueTrigger uses this last one to track per-NPC conversation counts)
// =============================================================================

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Assets.Scripts.Quests;

public class Dialogue : MonoBehaviour
{
    // --- UI References ---
    [Header("Dialogue Box Components:")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image dialogueBoxImage;
    [SerializeField] private Image characterBoxImage;

    [Header("Choice Buttons:")]
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private Button[] choiceButtons;

    [Header("Speed of Typing:")]
    [Tooltip("Speed of the Dialogue Text being typed out. (Lower value makes text type out faster)")]
    [SerializeField] private float dialogueSpeed;

    [Header("Dialogue Data:")]
    public DialogueObject currentDialogue;

    [Header("System Connections:")]
    [SerializeField] private DialogueChoiceHandler choiceHandler;

    // --- Runtime State ---
    private CompanionFriendship currentCompanion;
    private InventoryManager inventoryManager;

    private int currentLineIndex;
    private int currentDialogueEndIndex; // the last line index to play for this quest state
    private bool isDialogueActive = false;
    private bool isTyping = false;

    // --- Global Events ---
    // Other systems subscribe to these to react when a conversation starts or ends.
    public static event System.Action<DialogueObject> OnDialogueEnded;
    public static event System.Action<CompanionFriendship> OnDialogueStarted;
    public static event System.Action<CompanionFriendship> OnDialogueEndedCompanion;

    // --- Initialization ---
    // Hide UI elements on startup so they don't appear before any dialogue is triggered.
    void Start()
    {
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (choicesPanel != null)
        {
            choicesPanel.SetActive(false);
        }

        if (choiceHandler == null)
        {
            choiceHandler = GetComponent<DialogueChoiceHandler>();
        }
        inventoryManager = FindFirstObjectByType<InventoryManager>();
    }

    // --- Input Handling ---
    // Processes mouse clicks during active dialogue.
    // Blocked while choices are visible — button clicks handle that path instead.
    void Update()
    {
        if (isDialogueActive && choicesPanel != null && !choicesPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (currentDialogue == null || currentDialogue.dialogueLines.Length == 0)
                return;

            if (isTyping)
            {
                // Skip animation: show the full line immediately.
                StopAllCoroutines();
                dialogueText.text = currentDialogue.dialogueLines[currentLineIndex].text;
                isTyping = false;
                CheckForChoices();
            }
            else
            {
                NextLine();
            }
        }
    }

    // --- Dialogue Start ---
    // Resolves which line segment to play based on quest state, then kicks off typing.
    void StartDialogue()
    {
        if (currentDialogue == null || currentDialogue.dialogueLines == null ||
            currentDialogue.dialogueLines.Length == 0)
        {
            return;
        }
        Debug.Log($"Dialogue: Starting dialogue for companion: {currentCompanion?.gameObject.name}");

        OnDialogueStarted?.Invoke(currentCompanion);

        currentLineIndex = GetStartLineIndex(currentDialogue);
        currentDialogueEndIndex = GetEndLineIndex(currentDialogue);
        dialogueText.text = string.Empty;
        isDialogueActive = true;

        if (dialogueBox != null && !dialogueBox.activeSelf)
        {
            dialogueBox.SetActive(true);
        }

        if (choicesPanel != null)
        {
            choicesPanel.SetActive(false);
        }

        ApplyLineVisuals(currentDialogue.dialogueLines[currentLineIndex]);
        StartCoroutine(TypeLine());
    }

    // Public entry point — called by DialogueTrigger to hand off a conversation.
    public void DisplayDialogue(DialogueObject dialogueObject, CompanionFriendship companion = null)
    {
        currentDialogue = dialogueObject;
        currentCompanion = companion;
        StartDialogue();
    }

    // --- Visuals ---
    // Updates the dialogue box UI to match the current line's speaker and style settings.
    private void ApplyLineVisuals(DialogueLine line)
    {
        if (nameText != null)
        {
            nameText.text = line.speakerName;
            nameText.gameObject.SetActive(!string.IsNullOrEmpty(line.speakerName));
        }

        if (dialogueBoxImage != null)
        {
            dialogueBoxImage.color = line.dialogueBoxColor;

            if (line.dialogueBoxSprite != null)
            {
                dialogueBoxImage.sprite = line.dialogueBoxSprite;
                dialogueBoxImage.type = Image.Type.Sliced;
            }
        }

        if (characterBoxImage != null)
        {
            if (line.speakerPortrait != null)
            {
                characterBoxImage.sprite = line.speakerPortrait;
                characterBoxImage.gameObject.SetActive(true);
            }
            else
            {
                characterBoxImage.gameObject.SetActive(false);
            }
        }

        dialogueText.color = line.textColor;
    }

    // --- Typing Animation ---
    // Prints one character at a time. When done, immediately checks if choices should appear.
    IEnumerator TypeLine()
    {
        DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];
        string fullText = currentLine.text;

        dialogueText.text = string.Empty;
        isTyping = true;

        foreach (char c in fullText.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(dialogueSpeed);
        }

        isTyping = false;
        CheckForChoices();
    }

    // --- Choice Detection ---
    // After a line finishes typing, check if the player needs to make a choice.
    private void CheckForChoices()
    {
        DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];

        if (currentLine.choices != null && currentLine.choices.Length > 0)
        {
            ShowChoices(currentLine.choices);
        }
    }

    // --- Choice Display ---
    // Activates the choices panel and binds each button to its choice.
    // Unused buttons are hidden so stale text from a previous line doesn't show.
    private void ShowChoices(DialogueChoice[] choices)
    {
        if (choicesPanel != null)
        {
            choicesPanel.SetActive(true);
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TMP_Text>().text = choices[i].choiceText;

                int index = i;
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(index));
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // --- Choice Selection ---
    // Delegates gameplay side-effects to DialogueChoiceHandler, then drives
    // the conversation forward based on which type of choice was made.
    private void OnChoiceSelected(int choiceIndex)
    {
        if (choicesPanel != null)
            choicesPanel.SetActive(false);

        DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];
        if (currentLine.choices == null || choiceIndex >= currentLine.choices.Length)
        {
            NextLine();
            return;
        }

        DialogueChoice chosenChoice = currentLine.choices[choiceIndex];

        // The handler is responsible for gameplay reactions:
        // quest acceptance, gift logic, future special actions.
        if (choiceHandler != null)
        {
            choiceHandler.HandleChoice(new DialogueChoiceContext
            {
                Choice = chosenChoice,
                DialogueObject = currentDialogue,
                Friendship = currentCompanion,
                Player = Player.PlayerManager.Instance
            });
        }

        switch (chosenChoice.choiceType)
        {
            case DialogueChoiceType.Talk:
                // Branch to a new dialogue or just continue the current one.
                if (chosenChoice.nextDialogue != null)
                {
                    DisplayDialogue(chosenChoice.nextDialogue, currentCompanion);
                }
                else
                {
                    NextLine();
                }
                break;

            case DialogueChoiceType.GetQuest:
                if (chosenChoice.nextDialogue != null)
                {
                    DisplayDialogue(chosenChoice.nextDialogue);
                }
                else
                {
                    EndDialogue();
                }
                break;

            case DialogueChoiceType.GiveGift:
                // Opens the gifting menu and then continues to the next dialogue.
                if (chosenChoice.nextDialogue != null)
                {
                    inventoryManager.GiftingMenu(currentCompanion);
                    DisplayDialogue(chosenChoice.nextDialogue);
                }
                else
                {
                    EndDialogue();
                }
                break;

            case DialogueChoiceType.LeaveConversation:
                EndDialogue();
                break;
        }
    }

    // --- Line Advance ---
    // Moves to the next line if we haven't hit the end index, otherwise closes the dialogue.
    void NextLine()
    {
        if (currentLineIndex < currentDialogueEndIndex)
        {
            currentLineIndex++;
            if (choicesPanel != null)
            {
                choicesPanel.SetActive(false);
            }

            ApplyLineVisuals(currentDialogue.dialogueLines[currentLineIndex]);
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    // --- Quest Segment: Start Index ---
    // Reads the DialogueObject's index markers to find the correct first line
    // for the current quest state. Returns 0 if no quest is linked.
    private int GetStartLineIndex(DialogueObject dialogueObject)
    {
        if (dialogueObject == null || dialogueObject.dialogueLines == null || dialogueObject.dialogueLines.Length == 0)
        {
            return 0;
        }

        int lastLineIndex = dialogueObject.dialogueLines.Length - 1;

        if (dialogueObject.quest != null && QuestController.Instance != null)
        {
            string questID = dialogueObject.quest.questID;

            if (QuestController.Instance.IsQuestCompleted(questID))
                return ClampLineIndex(dialogueObject.questCompletedIndex, lastLineIndex);

            if (QuestController.Instance.IsQuestActive(questID))
                return ClampLineIndex(dialogueObject.questInProgressIndex, lastLineIndex);
        }

        return 0;
    }

    // --- Quest Segment: End Index ---
    // Finds the last line the player should see for the current quest state.
    // The in-progress segment ends just before the completed segment begins.
    private int GetEndLineIndex(DialogueObject dialogueObject)
    {
        if (dialogueObject == null || dialogueObject.dialogueLines == null || dialogueObject.dialogueLines.Length == 0)
        {
            return 0;
        }

        int lastLineIndex = dialogueObject.dialogueLines.Length - 1;

        if (dialogueObject.quest != null && QuestController.Instance != null)
        {
            string questID = dialogueObject.quest.questID;

            if (QuestController.Instance.IsQuestCompleted(questID))
                return lastLineIndex;

            if (QuestController.Instance.IsQuestActive(questID))
            {
                if (dialogueObject.questCompletedIndex > dialogueObject.questInProgressIndex)
                    return ClampLineIndex(dialogueObject.questCompletedIndex - 1, lastLineIndex);

                return lastLineIndex;
            }
        }

        // No quest or quest not started — cap at initialDialogueEndIndex if set.
        if (dialogueObject.initialDialogueEndIndex > 0)
            return ClampLineIndex(dialogueObject.initialDialogueEndIndex, lastLineIndex);

        return lastLineIndex;
    }

    private int ClampLineIndex(int index, int lastLineIndex)
    {
        return Mathf.Clamp(index, 0, lastLineIndex);
    }

    // --- End Dialogue ---
    // Hides the UI, notifies the choice handler (for any cleanup), then fires the global events.
    void EndDialogue()
    {
        isDialogueActive = false;

        if (choicesPanel != null)
            choicesPanel.SetActive(false);

        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        if (choiceHandler != null)
            choiceHandler.HandleDialogueEnded(currentDialogue);

        OnDialogueEnded?.Invoke(currentDialogue);
        OnDialogueEndedCompanion?.Invoke(currentCompanion);
        currentCompanion = null;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}