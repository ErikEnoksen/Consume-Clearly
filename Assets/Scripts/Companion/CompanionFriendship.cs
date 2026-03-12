using System;
using UnityEngine;

public class CompanionFriendship : MonoBehaviour
{
    public event Action<int> OnFriendshipLevelChanged;
    public int MaxFriendshipLevel = 1000;
    public int CurrentFriendshipLevel = 0;

    public void IncreaseFriendship(int amount)
    {

        CurrentFriendshipLevel = Mathf.Min(CurrentFriendshipLevel + amount, MaxFriendshipLevel);
        OnFriendshipLevelChanged?.Invoke(CurrentFriendshipLevel);
    }

    public void DecreaseFriendship(int amount)
    {
        CurrentFriendshipLevel = Mathf.Max(CurrentFriendshipLevel - amount, 0);
        OnFriendshipLevelChanged?.Invoke(CurrentFriendshipLevel);
    }

}
