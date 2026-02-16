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
}

[CreateAssetMenu(fileName = "DialogueObject", menuName = "Scriptable Objects/DialogueObject")]
public class DialogueObject : ScriptableObject
{
    public DialogueLine[] dialogueLines;
}
