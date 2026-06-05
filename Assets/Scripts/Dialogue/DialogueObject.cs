// =============================================================================
// DialogueObject.cs — Dialogue Data Container (ScriptableObject)
//
// PURPOSE:
//   The raw data asset for a single conversation. Create one per NPC interaction
//   via Assets → Create → Scriptable Objects → DialogueObject.
//   Dialogue.cs reads this at runtime to know what to display.
//
// STRUCTURE:
//   A DialogueObject is a flat array of DialogueLines played top-to-bottom.
//   Lines can optionally end with player choices (DialogueChoice[]).
//   If a line has no choices, the player just clicks to continue.
//
// QUEST BRANCHING (index-based):
//   When a quest is linked, the dialogue array is split into segments using
//   three index markers:
//     • 0 → initialDialogueEndIndex        : shown before the quest is accepted
//     • questInProgressIndex → questCompletedIndex-1 : shown while quest is active
//     • questCompletedIndex → end           : shown once all objectives are done
//   Dialogue.cs uses these to set the start/end line for a given conversation.
//
// CHOICE TYPES (DialogueChoiceType):
//   • Talk              — normal conversation, affects companion mood
//   • GetQuest          — triggers quest acceptance via QuestController
//   • GiveGift          — opens the gifting menu
//   • LeaveConversation — ends the dialogue immediately
// =============================================================================

using UnityEngine;
using UnityEngine.UI;

// --- Choice Type ---
// What category of action this choice triggers when selected.
public enum DialogueChoiceType
{
    Talk,
    GetQuest,
    GiveGift,
    LeaveConversation
}

// --- Choice Quality ---
// Used by Talk choices to shift the companion's mood after the player responds.
public enum ChoiceQuality
{
    Good,
    Neutral,
    Bad
}

// --- Dialogue Choice ---
// One option shown to the player at the end of a dialogue line.
// Can branch to a new DialogueObject or continue in-line.
[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public DialogueChoiceType choiceType = DialogueChoiceType.Talk;
    public ChoiceQuality choiceQuality = ChoiceQuality.Neutral;
    public DialogueObject nextDialogue; // if set, jumps to this dialogue on selection
}

// --- Dialogue Line ---
// One page of text in the conversation. Visuals (colors, portrait, name) are
// set per-line so different speakers or moods can look distinct.
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
    public DialogueChoice[] choices; // if empty, auto-continues on click; if filled, shows choice buttons
}

// --- Dialogue Object ---
// The full conversation asset. Assign this to a DialogueTrigger stage in the Inspector.
[CreateAssetMenu(fileName = "DialogueObject", menuName = "Scriptable Objects/DialogueObject")]
public class DialogueObject : ScriptableObject
{
    public DialogueLine[] dialogueLines;

    // Quest branching — leave quest null for a simple linear conversation.
    [Header("Quest Settings:")]
    public Quest quest;
    public int questInProgressIndex;  // line index where the in-progress segment starts
    public int questCompletedIndex;   // line index where the completed segment starts
    public int initialDialogueEndIndex; // last line to show before the quest is accepted (0 = show all)
}