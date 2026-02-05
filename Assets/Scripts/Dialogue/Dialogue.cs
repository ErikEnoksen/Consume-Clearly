using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Dialogue : MonoBehaviour
{
    [Header("Dialogue Box Components:")]
    [Tooltip("The Dialogue Box game object.")]
    [SerializeField] private GameObject dialogueBox;
    [Tooltip("The Dialogue Text being displayed.")]
    [SerializeField] private TMP_Text dialogueText;
    [Tooltip("The Name Text displayed in the top corner of the Dialogue Box. (Name of the person speaking)")]
    [SerializeField] private TMP_Text nameText;
    [Tooltip("Image within the Dialogue Box. (The Dialogue Box background)")]
    [SerializeField] private Image dialogueBoxImage;
    
    [Header("Speed of Typing:")]
    [Tooltip("Speed of the Dialogue Text being typed out. (Lower value makes text type out faster)")]
    [SerializeField] private float dialogueSpeed;
    
    [Header("Dialogue Data:")]
    [Tooltip("The Scriptable Object that holds dialogue data. (The Scriptable Object controls the Dialogue Box Components)")]
    public DialogueObject currentDialogue;

    private int currentLineIndex;

    void Start()
    {
        StartDialogue();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];
            string fullText = currentLine.text;

            if (dialogueText.text == fullText)
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                dialogueText.text = fullText;
            }
        }
    }

void StartDialogue()
    {
        currentLineIndex = 0;
        dialogueText.text = string.Empty;

        if (!dialogueBox.activeSelf)
        {
            dialogueBox.SetActive(true);
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
        }
        
        if (dialogueBoxImage != null && line.dialogueBoxSprite != null)
        {
            dialogueBoxImage.sprite = line.dialogueBoxSprite;
            dialogueBoxImage.type = Image.Type.Sliced; 
        }
        
        dialogueText.color = line.textColor;
    }

    IEnumerator TypeLine()
    {
        DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];
        string fullText = currentLine.text;

        dialogueText.text = string.Empty;

        foreach (char c in fullText.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(dialogueSpeed);
        }
    }

    void NextLine()
    {
        if (currentLineIndex < currentDialogue.dialogueLines.Length - 1)
        {
            currentLineIndex++;
            DialogueLine nextLine = currentDialogue.dialogueLines[currentLineIndex];

            ApplyLineVisuals(nextLine);
            StartCoroutine(TypeLine());
        }
        else
        {
            dialogueBox.SetActive(false);
        }
    }
}
