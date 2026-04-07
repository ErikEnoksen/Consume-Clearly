using System;
using System.Collections.Generic;
using UnityEngine;

public class CompanionFriendship : MonoBehaviour
{
    public event Action<int> OnFriendshipLevelChanged;
    public event Action<FriendshipState> OnStateChanged;

    public int MaxFriendshipLevel = 1000;
    public int CurrentFriendshipLevel = 0;
    public int DailyConversationReward = 25;
    public int MissedConversationSubstraction = 10;

    private bool DailyConversationGiven = false;
    public enum CompanionMood { Happy, Neutral, Angry }
    public event Action<CompanionMood> OnMoodChanged;
    private CompanionMood _currentMood;
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
            Debug.Log("Daily conversation bonus given.");
        }
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

}
