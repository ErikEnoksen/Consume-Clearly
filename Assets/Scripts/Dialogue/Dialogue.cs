using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    
    private int currentLineIndex;
    private bool isDialogueActive = false;
    private bool isTyping = false; 
    public static event System.Action<DialogueObject> OnDialogueEnded;
    public static event System.Action OnQuestRequested;
    public static event System.Action OnGiftRequested;

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
        currentLineIndex = 0;
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

        ApplyLineVisuals(currentDialogue.dialogueLines[0]);
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

    private void ShowChoices(DialogueChoice[] choices)
    {
        if (choicesPanel != null)
            choicesPanel.SetActive(true);

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
        {
            choicesPanel.SetActive(false);
        }

        DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];
        if (currentLine.choices == null || choiceIndex >= currentLine.choices.Length)
        {
            NextLine();
            return;
        }

        DialogueChoice chosenChoice = currentLine.choices[choiceIndex];

        if (choiceHandler != null)
        {
            choiceHandler.HandleChoice(chosenChoice.choiceType);
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
            case DialogueChoiceType.GiveGift:
            case DialogueChoiceType.LeaveConversation:
                if (chosenChoice.choiceType == DialogueChoiceType.LeaveConversation)
                {
                    EndDialogue();
                }
                else if (chosenChoice.nextDialogue != null)
                {
                    DisplayDialogue(chosenChoice.nextDialogue);
                }
                else
                {
                    EndDialogue();
                }
                break;
        }
    }

    void NextLine()
    {
        if (currentLineIndex < currentDialogue.dialogueLines.Length - 1)
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

        OnDialogueEnded?.Invoke(currentDialogue);
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}






















