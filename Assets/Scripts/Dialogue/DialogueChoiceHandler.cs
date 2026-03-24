using UnityEngine;
using Assets.Scripts.Quests;

public class DialogueChoiceHandler : MonoBehaviour
{
    public void HandleChoice(DialogueChoiceType choiceType, DialogueObject dialogueObject)
    {
        switch (choiceType)
        {
            case DialogueChoiceType.Talk:
                Debug.Log("Talk choice selected.");
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
