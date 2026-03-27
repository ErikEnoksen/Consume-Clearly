using UnityEngine;
using Assets.Scripts.Quests;

public class DialogueChoiceHandler : MonoBehaviour
{
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
                Debug.Log("Give Gift choice selected.");
                // Later:
                // open gift selection UI
                // get selected item from inventory
                // pass item data to the gift system
                // the gift system handles affection and NPC reaction
                break;

            case DialogueChoiceType.LeaveConversation:
                Debug.Log("Leave Conversation choice selected.");
                break;
        }
    }

    public void HandleDialogueEnded(DialogueObject dialogueObject)
    {
        if (dialogueObject == null || dialogueObject.quest == null || QuestController.Instance == null)
        {
            return;
        }

        if (!QuestController.Instance.IsQuestActive(dialogueObject.quest.questID))
        {
            return;
        }

        if (!QuestController.Instance.IsQuestCompleted(dialogueObject.quest.questID))
        {
            return;
        }

        bool turnedIn = QuestController.Instance.TurnInQuest(dialogueObject.quest);
        Debug.Log(turnedIn
            ? "Quest turned in after dialogue end: " + dialogueObject.quest.questName
            : "Dialogue ended, but quest could not be turned in: " + dialogueObject.quest.questName);
    }

    private void HandleGiveGift()
    {
        Debug.Log("Gift system should handle the gift data and affection here.");
    }
}

public class DialogueChoiceContext
{
    public DialogueChoice Choice { get; set; }
    public DialogueObject DialogueObject { get; set; }
    public CompanionFriendship Friendship { get; set; }
    public Player.PlayerManager Player { get; set; }
}