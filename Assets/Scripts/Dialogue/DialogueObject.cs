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
    
    // For future branching:
    // public DialogueObject[] nextDialogues; //If we want different dialouges when choosing dialouge option
}

[CreateAssetMenu(fileName = "DialogueObject", menuName = "Scriptable Objects/DialogueObject")]
public class DialogueObject : ScriptableObject
{
    public DialogueLine[] dialogueLines;
}