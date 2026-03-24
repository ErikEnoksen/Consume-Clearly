using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FriendshipBar : MonoBehaviour
{
    [System.Serializable]
    private class MoodSprite
    {
        public CompanionFriendship.CompanionMood mood;
        public Sprite sprite;
    }

    [SerializeField] private Slider friendshipBar;
    [SerializeField] private List<MoodSprite> moodSprites;

    private CompanionFriendship friendship;
    private Action<DialogueObject> dialogueEndedHandler;
    private DialogueObject matchedDialogue;
    private Image handleImage;
    private void Awake()
    {
        handleImage = friendshipBar.handleRect.GetComponent<Image>();
    }

    void Start()
    {
        if (friendshipBar == null)
        {
            Debug.LogError("FriendshipBar: No Slider component assigned.");
            return;
        }
        friendshipBar.gameObject.SetActive(false);
    }

    // Now accepts optional DialogueObject to match. If null -> hides on any dialogue end.
    public void ShowFriendshipBar(CompanionFriendship companion, DialogueObject dialogueToMatch = null)
    {
        if (companion == null || friendshipBar == null) return;

        friendship = companion;
        friendshipBar.maxValue = companion.MaxFriendshipLevel;
        friendshipBar.value = companion.CurrentFriendshipLevel;//set to current level immediately to 0

        friendship.OnFriendshipLevelChanged += UpdateFriendshipBar;//subscribe to the new companion

        // Create and subscribe a handler that hides the bar when the appropriate dialogue ends
        if (dialogueToMatch != null)
        {
            matchedDialogue = dialogueToMatch;
            dialogueEndedHandler = (ended) =>
            {
                if (ended == matchedDialogue)
                {
                    HideFriendshipBar();
                }
            };
        }

        Dialogue.OnDialogueEnded += dialogueEndedHandler;

        friendshipBar.gameObject.SetActive(true);
    }

    public void HideFriendshipBar()
    {

        // Unsubscribe companion event
        if (friendship != null)
        {
            friendship.OnFriendshipLevelChanged -= UpdateFriendshipBar;
            friendship = null;
        }

        // Unsubscribe dialogue end handler
        if (dialogueEndedHandler != null)
        {
            Dialogue.OnDialogueEnded -= dialogueEndedHandler;
            dialogueEndedHandler = null;
            matchedDialogue = null;
        }

        if (friendshipBar != null)
            friendshipBar.gameObject.SetActive(false);
    }



    private void UpdateFriendshipBar(int current)
    {
        if (friendshipBar == null) return;
        friendshipBar.value = current;

        foreach (var entry in moodSprites)
        {
            if (entry.mood == friendship.CurrentMood)
            {
                handleImage.sprite = entry.sprite;
                break;
            }
        }

    }

}
