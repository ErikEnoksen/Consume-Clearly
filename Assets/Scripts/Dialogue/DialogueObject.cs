using UnityEngine;
using UnityEngine.UI;

public enum DialogueChoiceType
{
    Talk,
    GetQuest,
    GiveGift,
    LeaveConversation
}

public enum ChoiceQuality
{
    Good,
    Neutral,
    Bad
}
[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public DialogueChoiceType choiceType = DialogueChoiceType.Talk;
    public ChoiceQuality choiceQuality = ChoiceQuality.Neutral;
    public DialogueObject nextDialogue;
}
[System.Serializable]
public class DialogueLine
{
    [TextArea] public string text;
    public string speakerName = "";
    public Color dialogueBoxColor = Color.white;
    public Color textColor = Color.black;
    public Sprite dialogueBoxSprite;
    public Sprite speakerPortrait;

    [Header("Player Choices:")]
    public DialogueChoice[] choices; // If empty, auto-continues. If filled, shows choice buttons.
}

[CreateAssetMenu(fileName = "DialogueObject", menuName = "Scriptable Objects/DialogueObject")]
public class DialogueObject : ScriptableObject
{
    public DialogueLine[] dialogueLines;
    [Header("Quest Settings:")]
    public Quest quest;
    public int questInProgressIndex;
    public int questCompletedIndex;
    public int initialDialogueEndIndex;
}