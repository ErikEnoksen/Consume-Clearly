using System;
using UnityEngine;

public class CompanionFriendship : MonoBehaviour
{
    public event Action<int> OnFriendshipLevelChanged;
    public event Action<FriendshipState> OnStateChanged;

    public int MaxFriendshipLevel = 1000;
    public int CurrentFriendshipLevel = 0;
    public enum CompanionMood { Happy, Neutral, Angry }
    public CompanionMood CurrentMood;

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
            else if (CurrentFriendshipLevel < 850 && CurrentMood != CompanionMood.Angry)
                return FriendshipState.Friend;
            else
                return FriendshipState.BestFriend;
        }
    }

    private void Start()
    {
        previousState = CurrentState;
    }
    private void CheckStateChange()
    {
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
    }

    public void DecreaseFriendship(int amount)
    {
        CurrentFriendshipLevel = Mathf.Max(CurrentFriendshipLevel - amount, 0);
        OnFriendshipLevelChanged?.Invoke(CurrentFriendshipLevel);
    }

    private void MoodSwitching()
    {
        //dialogue can change mood of the companion
        if (CurrentMood == CompanionMood.Angry)
        {
            if (CurrentState == FriendshipState.Stranger)
            {
                //go to neutral when circular sytisfaction is increased
                //cant talk to companion when angry and stranger
            }
            if (CurrentState == FriendshipState.Acquaintance)
            {
                //cant receive quests when angry and acquaintance
            }
        }
    }

}
