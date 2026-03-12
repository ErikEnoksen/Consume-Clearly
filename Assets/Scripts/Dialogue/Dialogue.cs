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

    private bool singleLineDialogue = false;
    private int currentLineIndex;
    private bool isDialogueActive = false;
    private bool isTyping = false;
    public static event System.Action<DialogueObject> OnDialogueEnded;

    void Start()
    {
        if (dialogueBox != null)
            dialogueBox.SetActive(false);

        if (choicesPanel != null)
            choicesPanel.SetActive(false);
    }
    
    void Update()
    {
      
        if (isDialogueActive && !choicesPanel.activeSelf && Input.GetMouseButtonDown(0))
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
        currentLineIndex = 0;
        singleLineDialogue = false;

        if (currentDialogue.quest != null)
        {
            var controller = QuestController.Instance;

            if (controller != null)
            {
                var activeQuest = controller.ActiveQuests
                    .Find(q => q.QuestID == currentDialogue.quest.questID);

                if (activeQuest != null)
                {
                    if (activeQuest.IsCompleted)
                    {
                        currentLineIndex = currentDialogue.questCompletedIndex;
                        singleLineDialogue = true;
                    }
                    else
                    {
                        currentLineIndex = currentDialogue.questInProgressIndex;
                        singleLineDialogue = true;
                    }
                }
            }
        }

        dialogueText.text = "";
        isDialogueActive = true;

        if (!dialogueBox.activeSelf)
            dialogueBox.SetActive(true);

        choicesPanel.SetActive(false);

        ApplyLineVisuals(currentDialogue.dialogueLines[currentLineIndex]);
        StartCoroutine(TypeLine());
    }

    public void DisplayDialogue(DialogueObject dialogueObject)
    {
        currentDialogue = dialogueObject;
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

    private void ShowChoices(string[] choices)
    {
        choicesPanel.SetActive(true);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].GetComponentInChildren<TMP_Text>().text = choices[i];

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
        choicesPanel.SetActive(false);

        DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];

        // QUEST GIVING
        if (currentLine.givesQuest != null &&
            choiceIndex < currentLine.givesQuest.Length &&
            currentLine.givesQuest[choiceIndex])
        {
            if (currentDialogue.quest != null)
            {
                QuestController.Instance.AcceptQuest(currentDialogue.quest);
                Debug.Log("Quest accepted: " + currentDialogue.quest.questName);
            }
        }

        // Continue dialogue
        if (currentLine.nextDialogues != null &&
            choiceIndex < currentLine.nextDialogues.Length &&
            currentLine.nextDialogues[choiceIndex] != null)
        {
            DisplayDialogue(currentLine.nextDialogues[choiceIndex]);
        }
        else
        {
            NextLine();
        }
    }

    void NextLine()
    {
        // If this dialogue is a single-line quest state
        if (singleLineDialogue)
        {
            EndDialogue();
            return;
        }

        // Stop at the end of the initial conversation
        if (currentDialogue.quest != null &&
            currentLineIndex >= currentDialogue.initialDialogueEndIndex)
        {
            EndDialogue();
            return;
        }

        if (currentLineIndex < currentDialogue.dialogueLines.Length - 1)
        {
            currentLineIndex++;
            choicesPanel.SetActive(false);
            ApplyLineVisuals(currentDialogue.dialogueLines[currentLineIndex]);
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        choicesPanel.SetActive(false);
        dialogueBox.SetActive(false);
        OnDialogueEnded?.Invoke(currentDialogue);
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}