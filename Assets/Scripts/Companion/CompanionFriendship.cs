// This script manages the friendship level and mood of a companion character in a game.
// Purpose:
// It includes functionality for increasing and decreasing friendship levels, changing moods, and providing daily conversation bonuses.
// The script also handles state changes based on the current friendship level and mood, and it integrates with a day cycle manager to reset daily bonuses and apply penalties for missed conversations.
//
// Key Features:
// - Friendship levels with defined states (Stranger, Acquaintance, Friend, Best Friend)
// - Mood states (Happy, Neutral, Angry) that affect the rate of friendship increase
// - Daily conversation bonuses and penalties
// - Events for when friendship levels and moods change, allowing other parts of the game to react accordingly
//
// FLOW:
// 1. The script initializes and subscribes to the day cycle manager's new day event.
// 2. The player can interact with the companion to increase or decrease friendship levels, which also checks for state changes.
// 3. The companion's mood can be switched, affecting the multiplier for friendship increases.
// 
// Start() -> IncreaseFriendship() / DecreaseFriendship() -> Apply multiplier -> CheckStateChange() -> OnStateChanged event
// GiveDailyConversationBonus() /^\ 
// ResetDaily() -> DecreaseFriendship() if daily bonus not given -> Reset daily bonus and mood


using System;
using System.Collections.Generic;
using UnityEngine;

public class CompanionFriendship : MonoBehaviour
{
    private CommunityMeter communityMeter;
    public event Action<int> OnFriendshipLevelChanged;
    public event Action<FriendshipState> OnStateChanged;

    public int MaxFriendshipLevel = 1000;
    public int CurrentFriendshipLevel = 0;
    public int DailyConversationReward = 25;
    public int MissedConversationSubstraction = 10;

    public bool DailyConversationGiven { get; private set; } = false;
    public enum CompanionMood { Happy, Neutral, Angry }
    public event Action<CompanionMood> OnMoodChanged;
    private CompanionMood _currentMood = CompanionMood.Neutral;
    public CompanionMood CurrentMood
    {
        get => _currentMood;
        set {             if (_currentMood != value)
            {
                _currentMood = value;
                OnMoodChanged?.Invoke(_currentMood);
                Debug.Log("Mood changed to: " + _currentMood);
            }
        }
    }

    public double multiplier
    {
        get
        {
            switch (CurrentMood)
            {
                case CompanionMood.Neutral:
                    return 1;
                case CompanionMood.Happy:
                    return 1.25;
                case CompanionMood.Angry:
                    return 0.75;
                default:
                    return 1;
            }
        }
    }

    private FriendshipState previousState;
    public enum FriendshipState { Stranger, Acquaintance, Friend, BestFriend }
    public FriendshipState CurrentState
    {
        get
        {
            if (CurrentFriendshipLevel < 100)
                return FriendshipState.Stranger;
            else if (CurrentFriendshipLevel < 400)
                return FriendshipState.Acquaintance;
            else if (CurrentFriendshipLevel < 850)
                return FriendshipState.Friend;
            else
                return FriendshipState.BestFriend;
        }
    }
    private void Start()
    {
        previousState = CurrentState;
        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.OnNewDay += ResetDaily;
    }

    private void CheckStateChange()
    {
        Debug.Log($"Checking state change. Current friendship level: {CurrentFriendshipLevel}, Current mood: {CurrentMood}, Current state: {CurrentState}, Previous state: {previousState}");
        var newState = CurrentState;

        if (newState != previousState)
        {
            OnStateChanged?.Invoke(newState);
            previousState = newState;
        }
    }

    public void IncreaseFriendship(int amount)
    {
        int increasedAmount = Mathf.RoundToInt(amount * (float)multiplier);
        CurrentFriendshipLevel = Mathf.Min(CurrentFriendshipLevel + increasedAmount, MaxFriendshipLevel);
        CheckStateChange();
        OnFriendshipLevelChanged?.Invoke(CurrentFriendshipLevel);
        Debug.Log($"Increased friendship by {increasedAmount} (base: {amount}, multiplier: {multiplier}). Current level: {CurrentFriendshipLevel}, {CurrentState}");
    }

    public void DecreaseFriendship(int amount)
    {
        CurrentFriendshipLevel = Mathf.Max(CurrentFriendshipLevel - amount, 0);
        OnFriendshipLevelChanged?.Invoke(CurrentFriendshipLevel);
    }

    public void MoodSwitching(CompanionMood mood)
    {
        Debug.Log(CurrentMood + " -> " + mood);
        if (CurrentMood != mood)
        {
            CurrentMood = mood;
        }
    }

    public void GiveDailyConversationBonus()
    {
        if (!DailyConversationGiven)
        {
            IncreaseFriendship(DailyConversationReward);
            DailyConversationGiven = true;
            // Gives a daily talk bonus to the community meter if the companion is at least a friend
            if (CurrentState == FriendshipState.Friend || CurrentState == FriendshipState.BestFriend)
            {
                communityMeter?.IncreaseCommunityLevel(DailyConversationReward);
            }
            Debug.Log("Daily conversation bonus given.");
        }
    }

    private void OnDestroy()
    {
        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.OnNewDay -= ResetDaily;
    }

    private void ResetDaily(int day)
    {
        if (!DailyConversationGiven)
        {
            // Penalize the player for missing the daily conversation
            DecreaseFriendship(MissedConversationSubstraction);
        }
        DailyConversationGiven = false;
        MoodSwitching(CompanionMood.Neutral);
    }

    public void LoadFriendshipState(int level, int mood, bool dailyGiven)
    {
        CurrentFriendshipLevel = level;
        _currentMood = (CompanionMood)mood;
        DailyConversationGiven = dailyGiven;
        previousState = CurrentState;
        OnFriendshipLevelChanged?.Invoke(CurrentFriendshipLevel);
        OnMoodChanged?.Invoke(_currentMood);
    }

    public Color GetFriendshipColor()
    {
            if (CurrentState == FriendshipState.Stranger)
                {
                    return Color.darkRed; // Stranger - Red
        }
                else if (CurrentState == FriendshipState.Acquaintance)
                {
                    return Color.yellow; // Acquaintance - Yellow
        }
                else if (CurrentState == FriendshipState.Friend)
                {
                    return Color.pink; // Friend - Green
        }
                else
                {
                   return Color.red; // Best Friend - Blue
        }
    }
}
