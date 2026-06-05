// This script manages the friendship bar UI, updating it based on the current companion's friendship level and mood.
// 
// Purpose:
// It listens for dialogue events to show or hide the friendship bar,
// and it updates the bar's fill amount and color based on the companion's current friendship level and mood.
// It also provides feedback when the friendship level changes.
//
// Key Features:
// - Listens for dialogue start and end events to manage the visibility of the friendship bar.
// - Updates the friendship bar's fill amount and color based on the companion's current friendship level.
// - Changes the handle sprite based on the companion's mood.
// - Provides visual feedback when the friendship level changes.
//
// FLOW:
// 1. On dialogue start, it subscribes to the companion's friendship level and mood change events, and shows the friendship bar.
// 2. On dialogue end, it unsubscribes from the events and hides the friendship bar.
// 3. When the friendship level changes, it updates the bar's fill amount and color, and triggers feedback.
// 4. When the mood changes, it updates the handle sprite to reflect the new mood.
// 
// Start() -> OnDialogueStarted() -> Subscribe to companion events -> ShowFriendshipBar()
// OnDialogueEnded() -> Unsubscribe from events -> HideFriendshipBar()
// UpdateFriendshipLevel() -> Update bar fill and color -> FeedbackLoop()
//==============================================================================================================================================================================

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
        // Update the fill amount and color of the friendship bar based on the new level
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
        // Show feedback (e.g., a flash or animation) when the friendship level changes
        feedback.gameObject.SetActive(true);

        feedbackTimer = feedbackDuration;
    }

    public void Update()
    {
        // Handle feedback timer to hide feedback after duration
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