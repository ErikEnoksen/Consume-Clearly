using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    [TextArea] public string text;
    public string speakerName = "";
    public Color dialogueBoxColor = Color.white;
    public Color textColor = Color.black;
    public Sprite dialogueBoxSprite;
    
    [Header("Player Choices (optional):")]
    public string[] choices; // If empty, auto-continues. If filled, shows choice buttons.
    
    [Tooltip("Matching DialogueObject for each choice. Leave null to just continue to next line.")]
    public DialogueObject[] nextDialogues;

    public bool[] givesQuest;
}

[CreateAssetMenu(fileName = "DialogueObject", menuName = "Scriptable Objects/DialogueObject")]
public class DialogueObject : ScriptableObject
{
    public DialogueLine[] dialogueLines;

    public int questInProgressIndex; // what line gets triggered if talking again while quest is active
    public int questCompletedIndex; // what line gets triggered if quest is completed
    public int initialDialogueEndIndex; // end dialogue at this index of first time speaking
    public Quest quest;
}