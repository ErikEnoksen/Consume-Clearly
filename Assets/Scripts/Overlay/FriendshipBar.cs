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
    [SerializeField] private Image feedback; 
    [SerializeField] private List<MoodSprite> moodSprites;

    private CompanionFriendship currentCompanion;
    private Image handleImage;
    public float feedbackDuration = 3f;
    private float feedbackTimer = 0f;

    private void Start()
    {
        if (friendshipBar != null)
        {
            friendshipBar.gameObject.SetActive(false);
        }
        if (feedback == null)
        {
            Debug.LogError("FriendshipBar: Feedback Image reference is missing!");
            return;
        }

        // Subscribe to dialogue events (these now pass CompanionFriendship)
        Dialogue.OnDialogueEndedCompanion += OnDialogueEnded;
        Dialogue.OnDialogueStarted += OnDialogueStarted;
    }

    private void OnDialogueStarted(CompanionFriendship companion)
    {
        Debug.Log($"FriendshipBar: OnDialogueStarted called with companion: {companion?.gameObject.name}");
        if (companion != null)
        {
            // Unsubscribe from previous companion if there was one
            if (currentCompanion != null)
            {
                currentCompanion.OnFriendshipLevelChanged -= UpdateFriendshipLevel;
                currentCompanion.OnMoodChanged -= UpdateMoodSprite;
            }

            // Set new companion and subscribe to its events
            currentCompanion = companion;
            Debug.Log($"FriendshipBar: Now using companion: {currentCompanion.gameObject.name}");
            currentCompanion.OnFriendshipLevelChanged += UpdateFriendshipLevel;
            currentCompanion.OnMoodChanged += UpdateMoodSprite;

            // Update UI with current values
            UpdateFriendshipLevel(currentCompanion.CurrentFriendshipLevel);
            UpdateMoodSprite(currentCompanion.CurrentMood);

            // Show the friendship bar
            ShowFriendshipBar();
            feedback.gameObject.SetActive(false); 
        }
        else
        {
            Debug.LogWarning("Dialogue started with null companion reference!");
        }
    }

    private void OnDialogueEnded(CompanionFriendship companion)
    {
        // Hide the friendship bar
        HideFriendshipBar();

        // Unsubscribe from events
        if (currentCompanion != null)
        {
            currentCompanion.OnFriendshipLevelChanged -= UpdateFriendshipLevel;
            currentCompanion.OnMoodChanged -= UpdateMoodSprite;
            currentCompanion = null;
        }
    }

    private void ShowFriendshipBar()
    {
        if (friendshipBar != null)
        {
            friendshipBar.gameObject.SetActive(true);
        }
    }

    private void HideFriendshipBar()
    {
        if (friendshipBar != null)
        {
            friendshipBar.gameObject.SetActive(false);
        }
    }

    private void UpdateFriendshipLevel(int newLevel)
    {
        if (friendshipBar != null && currentCompanion != null)
        {
            friendshipBar.fillRect.GetComponent<Image>().color = currentCompanion.GetFriendshipColor();
            friendshipBar.maxValue = currentCompanion.MaxFriendshipLevel;
            friendshipBar.value = newLevel;
            FeedbackLoop();
        }
    }

    private void UpdateMoodSprite(CompanionFriendship.CompanionMood newMood)
    {
        // Get the handle image if not already cached
        if (handleImage == null && friendshipBar != null)
        {
            handleImage = friendshipBar.handleRect?.GetComponent<Image>();
        }

        if (handleImage == null)
        {
            Debug.LogError("FriendshipBar: Handle Image component not found.");
            return;
        }

        // Find and apply the sprite for the current mood
        foreach (var entry in moodSprites)
        {
            if (entry.mood == newMood)
            {
                handleImage.sprite = entry.sprite;
                break;
            }
        }
    }

    private void FeedbackLoop()
    {
        feedback.gameObject.SetActive(true);

        feedbackTimer = feedbackDuration;
    }

    public void Update()
    {
        if (feedback.gameObject.activeSelf)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0f)
            {
                feedback.gameObject.SetActive(false);
            }
        }
    }


    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (currentCompanion != null)
        {
            currentCompanion.OnFriendshipLevelChanged -= UpdateFriendshipLevel;
            currentCompanion.OnMoodChanged -= UpdateMoodSprite;
        }

        Dialogue.OnDialogueStarted -= OnDialogueStarted;
        Dialogue.OnDialogueEndedCompanion -= OnDialogueEnded;
    }
}