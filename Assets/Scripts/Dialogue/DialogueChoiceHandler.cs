using UnityEngine;

public class DialogueChoiceHandler : MonoBehaviour
{
    // [Header("System References")]
    // [SerializeField] private InventoryManager inventoryManager;
    //
    // private void Awake()
    // {
    //     if (inventoryManager == null)
    //         inventoryManager = FindObjectOfType<InventoryManager>();
    // }
    
    public void HandleChoice(DialogueChoiceType choiceType)
    {
        switch (choiceType)
        {
            case DialogueChoiceType.Talk:
                Debug.Log("Talk choice selected.");
                break;

            case DialogueChoiceType.GetQuest:
                Debug.Log("Get Quest choice selected.");
                break;

            case DialogueChoiceType.GiveGift:
                Debug.Log("Give Gift choice selected.");
                HandleGiveGift();
                break;

            case DialogueChoiceType.LeaveConversation:
                Debug.Log("Leave Conversation choice selected.");
                break;
        }
    }

    private void HandleGiveGift()
    {
        Debug.Log("Gift system should handle the gift data and affection here.");
    }
}
