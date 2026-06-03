// =============================================================================
// DialogueChoiceHandler.cs — Gameplay Side-Effects for Player Choices
// 
//
// PURPOSE:
//   Separates game logic from display logic. Dialogue.cs drives what the player
//   sees; this class handles what happens in the game world as a result.
//   Called by Dialogue.cs via HandleChoice() each time the player picks an option,
//   and via HandleDialogueEnded() when the conversation closes.
//
// RESPONSIBILITIES:
//   • Talk choices   — shift companion mood (Happy / Neutral / Angry) and
//                      grant the daily conversation friendship bonus
//   • GetQuest       — registers the quest with QuestController
//   • GiveGift       — placeholder; gifting UI is opened by Dialogue.cs,
//                      the actual gift logic is currently in InventoryManager
//   • LeaveConversation — no side effect, dialogue just closes
//
// QUEST TURN-IN GUARD:
//   HandleDialogueEnded() can also turn in a completed quest, but it skips
//   this if a quest was accepted in the same conversation (questWasAcceptedThisDialogue).
//   That case is handled by DialogueTrigger to avoid a double turn-in.
//
// CONTEXT OBJECT (DialogueChoiceContext):
//   A simple data bag passed into HandleChoice() so new fields can be added
//   without changing the method signature.
// =============================================================================

using UnityEngine;
using Assets.Scripts.Quests;

public class DialogueChoiceHandler : MonoBehaviour
{
    // --- Runtime State ---
    // Tracks whether a quest was accepted mid-conversation so HandleDialogueEnded
    // knows to skip its own turn-in attempt.
    private bool questWasAcceptedThisDialogue = false;

    // --- Choice Handler ---
    // Called immediately when the player selects a choice button.
    // Drives companion mood and quest state; Dialogue.cs handles the UI navigation after.
    public void HandleChoice(DialogueChoiceContext context)
    {
        var choice = context.Choice;
        var dialogueObject = context.DialogueObject;
        var friendship = context.Friendship;

        switch (choice.choiceType)
        {
            case DialogueChoiceType.Talk:
                Debug.Log("Talk choice selected.");
                if (friendship != null)
                {
                    // Map choice quality directly to companion mood.
                    switch(choice.choiceQuality)
                    {
                        case ChoiceQuality.Good:
                            friendship.MoodSwitching(CompanionFriendship.CompanionMood.Happy);
                            Debug.Log("Good choice!");
                            break;
                        case ChoiceQuality.Neutral:
                            friendship.MoodSwitching(CompanionFriendship.CompanionMood.Neutral);
                            Debug.Log("Neutral choice.");
                            break;
                        case ChoiceQuality.Bad:
                            friendship.MoodSwitching(CompanionFriendship.CompanionMood.Angry);
                            Debug.Log("Bad choice!");
                            break;
                    }
                    // One friendship bonus per day regardless of how many talk choices are made.
                    friendship.GiveDailyConversationBonus();
                }
                break;

            case DialogueChoiceType.GetQuest:
                Debug.Log("Get Quest choice selected.");
                if (dialogueObject != null && dialogueObject.quest != null)
                {
                    if (QuestController.Instance != null)
                    {
                        QuestController.Instance.AcceptQuest(dialogueObject.quest);
                        questWasAcceptedThisDialogue = true; // flag so HandleDialogueEnded skips turn-in
                        Debug.Log("Quest accepted: " + dialogueObject.quest.questName);
                    }
                    else
                    {
                        Debug.LogWarning("DialogueChoiceHandler: QuestController.Instance is null.");
                    }
                }
                else
                {
                    Debug.LogWarning("DialogueChoiceHandler: No quest assigned to this dialogue.");
                }
                break;

            case DialogueChoiceType.GiveGift:
                // Gifting UI is opened by Dialogue.cs before calling DisplayDialogue on the next asset.
                // This case is a hook for any additional gift logic needed here in the future.
                Debug.Log("Give Gift choice selected.");
                break;

            case DialogueChoiceType.LeaveConversation:
                Debug.Log("Leave Conversation choice selected.");
                break;
        }
    }

    // --- Dialogue End Handler ---
    // Called by Dialogue.cs when a conversation closes. Attempts a quest turn-in
    // if the completed dialogue was just shown, unless a quest was accepted this session.
    public void HandleDialogueEnded(DialogueObject dialogueObject)
    {
        // If a quest was accepted this conversation, DialogueTrigger handles turn-in instead.
        if (questWasAcceptedThisDialogue)
        {
            questWasAcceptedThisDialogue = false;
            return;
        }

        if (dialogueObject == null || dialogueObject.quest == null || QuestController.Instance == null)
            return;

        if (!QuestController.Instance.IsQuestActive(dialogueObject.quest.questID))
            return;

        if (!QuestController.Instance.IsQuestCompleted(dialogueObject.quest.questID))
            return;

        bool turnedIn = QuestController.Instance.TurnInQuest(dialogueObject.quest);
        Debug.Log(turnedIn
            ? "Quest turned in after dialogue end: " + dialogueObject.quest.questName
            : "Dialogue ended, but quest could not be turned in: " + dialogueObject.quest.questName);
    }

    // Placeholder — gift item data and affection logic will go here once the gift system is built.
    private void HandleGiveGift()
    {
        Debug.Log("Gift system should handle the gift data and affection here.");
    }
}

// --- Dialogue Choice Context ---
// Data bag passed into HandleChoice(). Add new fields here rather than changing the method signature.
public class DialogueChoiceContext
{
    public DialogueChoice Choice { get; set; }
    public DialogueObject DialogueObject { get; set; }
    public CompanionFriendship Friendship { get; set; }
    public Player.PlayerManager Player { get; set; }
}