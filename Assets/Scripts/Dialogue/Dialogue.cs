using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Assets.Scripts.Quests;

public class Dialogue : MonoBehaviour
{
    [Header("Dialogue Box Components:")]
    [SerializeField] private GameObject dialogueBox;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image dialogueBoxImage;
    
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

    private CompanionFriendship currentCompanion;
    private InventoryManager inventoryManager;

    private int currentLineIndex;
    private int currentDialogueEndIndex;
    private bool isDialogueActive = false;
    private bool isTyping = false; 
    public static event System.Action<DialogueObject> OnDialogueEnded;
    public static event System.Action<CompanionFriendship> OnDialogueStarted;
    public static event System.Action<CompanionFriendship> OnDialogueEndedCompanion;

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
            choiceHandler = FindObjectOfType<DialogueChoiceHandler>();
        }
        inventoryManager = FindFirstObjectByType<InventoryManager>();
    }
    
    void Update()
    {
        if (isDialogueActive && choicesPanel != null && !choicesPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (currentDialogue == null || currentDialogue.dialogueLines.Length == 0)
                return;

            if (isTyping)
            {
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

    public void DisplayDialogue(DialogueObject dialogueObject, CompanionFriendship companion = null)
    {
        currentDialogue = dialogueObject;
        currentCompanion = companion;
        StartDialogue();
    }

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

        dialogueText.color = line.textColor;
    }

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

    private void CheckForChoices()
    {
        DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];

        if (currentLine.choices != null && currentLine.choices.Length > 0)
        {
            ShowChoices(currentLine.choices);
        }
    }

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
                // ... add more as needed
            });
        }

        switch (chosenChoice.choiceType)
        {
            case DialogueChoiceType.Talk:
                if (chosenChoice.nextDialogue != null)
                {
                    DisplayDialogue(chosenChoice.nextDialogue);
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

                if (chosenChoice.nextDialogue != null)
                {
                    int giftValue = inventoryManager.LookForGift("Rope", 50);
                    Debug.Log(giftValue);
                    currentCompanion.IncreaseFriendship(giftValue);
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
            {
                return ClampLineIndex(dialogueObject.questCompletedIndex, lastLineIndex);
            }

            if (QuestController.Instance.IsQuestActive(questID))
            {
                return ClampLineIndex(dialogueObject.questInProgressIndex, lastLineIndex);
            }
        }

        return 0;
    }

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
            {
                return lastLineIndex;
            }

            if (QuestController.Instance.IsQuestActive(questID))
            {
                if (dialogueObject.questCompletedIndex > dialogueObject.questInProgressIndex)
                {
                    return ClampLineIndex(dialogueObject.questCompletedIndex - 1, lastLineIndex);
                }

                return lastLineIndex;
            }
        }

        if (dialogueObject.initialDialogueEndIndex > 0)
        {
            return ClampLineIndex(dialogueObject.initialDialogueEndIndex, lastLineIndex);
        }

        return lastLineIndex;
    }

    private int ClampLineIndex(int index, int lastLineIndex)
    {
        return Mathf.Clamp(index, 0, lastLineIndex);
    }

    void EndDialogue()
    {
        isDialogueActive = false;

        if (choicesPanel != null)
        {
            choicesPanel.SetActive(false);
        }

        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }

        if (choiceHandler != null)
        {
            choiceHandler.HandleDialogueEnded(currentDialogue);
        }

        OnDialogueEnded?.Invoke(currentDialogue);
        OnDialogueEndedCompanion?.Invoke(currentCompanion);
        currentCompanion = null;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}