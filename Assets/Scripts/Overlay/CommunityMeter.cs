// This script manages the community meter, which tracks the player's progress in building a community of companions.
//
// Purpose:
// It increases or decreases based on player actions and the number of friends (companions) the player has.
// The community meter has different states (Fragmented, Growing, Connecting, Thriving) that provide various benefits.
// The script also handles overflow points when the player tries to increase the community level beyond the current cap based on their friend count.
//
// Key Features:
// - Tracks community level and overflow points
// - Determines community state based on friend count and community level
// - Provides daily increases based on the current community state
// - Listens for changes in companion friendship states to update the community state accordingly
// - Updates the UI slider to reflect the current community level and state
// 
// FLOW:
// 1. On start, it finds all companions in the scene and registers them.
// 2. When a companion's friendship state changes, it checks if they became a friend or lost friendship and updates the friend count and community state.
// 3. When the community level is increased, it checks for overflow and applies it if the player gains more points than the current cap allows.
// 4. On a new day, it applies a daily increase to the community level based on the current state.
// 5. The community state is evaluated whenever there are changes to the friend count or community level, and the UI is updated accordingly.
// 
// Start() -> FindAndRegisterAllCompanions() -> OnCompanionStateChanged() -> EvaluateCommunityState() -> UpdateCommunityMeter()
// IncreaseCommunityLevel() -> AddPointsWithOverflow() -> TryApplyOverflow()
// OnNewDay() -> GetDailyCommunityIncrease() -> AddPointsWithOverflow()
// ==============================================================================================================================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommunityMeter : MonoBehaviour
{
    [SerializeField] private Slider communityMeter;

    public int CommunityLevel = 0;
    public int OverflowPoints = 0;
    private enum CommunityState { Fragmented, Growing, Connecting, Thriving }
    private event Action<int> OnCommunityLevelChanged;
    private event Action<CommunityState> OnCommunityStateChanged;
    private CommunityState CurrentState;
    public int FriendCount = 0;

    private readonly int[] thresholds = { 0, 25, 50, 75, 100 }; // Example thresholds for each state
    private CircularSatisfactionMeter satisfactionMeter;

    // Store all companions and track their states
    private List<CompanionFriendship> allCompanions = new List<CompanionFriendship>();
    private Dictionary<CompanionFriendship, CompanionFriendship.FriendshipState> companionStates = new Dictionary<CompanionFriendship, CompanionFriendship.FriendshipState>();
    private HashSet<CompanionFriendship> countedFriends = new HashSet<CompanionFriendship>();
    private void Start()
    {
        // Find all companions in the scene
        FindAndRegisterAllCompanions();
        
        satisfactionMeter = GetComponent<CircularSatisfactionMeter>();
        // Initialize community state
        CurrentState = GetCommunityState();
        // Update the UI
        UpdateCommunityMeter();
        // Subscribe to day cycle events
        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.OnNewDay += OnNewDay;
    }

    private void FindAndRegisterAllCompanions()
    {
        // Find all CompanionFriendship components in the scene
        allCompanions.Clear();
        companionStates.Clear();
        countedFriends.Clear();
        FriendCount = 0;

        CompanionFriendship[] companions = FindObjectsByType<CompanionFriendship>(FindObjectsSortMode.None);
        foreach (var companion in companions)
        {
            RegisterCompanion(companion);
        }

        Debug.Log($"CommunityMeter registered {allCompanions.Count} companions");
    }

    private void RegisterCompanion(CompanionFriendship companion)
    {
        if (!allCompanions.Contains(companion))
        {
            allCompanions.Add(companion);
            companionStates[companion] = companion.CurrentState;

            // Subscribe to state changes
            companion.OnStateChanged += OnCompanionStateChanged;

            // Check if this companion is already a friend
            if (IsActualFriend(companion.CurrentState))
            {
                // Only count as friend if it's Friend or BestFriend state
                if (!countedFriends.Contains(companion))
                {
                    countedFriends.Add(companion);
                    FriendCount++;
                    Debug.Log($"Initial friend found: {companion.gameObject.name}.(State: {companion.CurrentState}, FriendCount: {FriendCount}");
                } 
            }
        }
    }

    private bool IsActualFriend(CompanionFriendship.FriendshipState state)
    {
        // Only count as friend if it's Friend or BestFriend state
        return state == CompanionFriendship.FriendshipState.Friend ||
               state == CompanionFriendship.FriendshipState.BestFriend;
    }

    private void OnCompanionStateChanged(CompanionFriendship.FriendshipState newState)
    {
        // Find which companion triggered this
        CompanionFriendship companion = null;
        foreach (var c in allCompanions)
        {
            if (c.CurrentState == newState && companionStates[c] != newState)
            {
                companion = c;
                break;
            }
        }

        if (companion == null) return;

        Debug.Log($"CommunityMeter: {companion.gameObject.name} state changed from {companionStates[companion]} to {newState}");

        // Check if it changed TO Friend or BestFriend
        bool wasFriend = companionStates[companion] == CompanionFriendship.FriendshipState.Friend ||
                        companionStates[companion] == CompanionFriendship.FriendshipState.BestFriend;
        bool isFriend = newState == CompanionFriendship.FriendshipState.Friend ||
                       newState == CompanionFriendship.FriendshipState.BestFriend;

        if (!wasFriend && isFriend)
        {
            if (!countedFriends.Contains(companion))
            {
                countedFriends.Add(companion);
                FriendCount++;
                Debug.Log($"New friend added! FriendCount: {FriendCount}");
                TryApplyOverflow(); // Try to apply overflow points
                EvaluateCommunityState();
            }
        }
        else if (wasFriend && !isFriend)
        {
            if (countedFriends.Contains(companion))
            {
                countedFriends.Remove(companion);
                FriendCount = Mathf.Max(0, FriendCount - 1);
                Debug.Log($"Friend lost! FriendCount: {FriendCount}");
                EvaluateCommunityState();
            }
        }

        // Update the stored state
        companionStates[companion] = newState;

        // Re-evaluate community state
        EvaluateCommunityState();
    }

    private CommunityState GetCommunityState()
    {
        // Growing: Need at least 1 friend AND level 25+
        if (FriendCount >= 1 && CommunityLevel >= thresholds[1])
        {
            // Connecting: Need at least 2 friends AND level 50+
            if (FriendCount >= 2 && CommunityLevel >= thresholds[2])
            {
                // Thriving: Need at least 3 friends AND level 75+
                if (FriendCount >= 3 && CommunityLevel >= thresholds[3])
                {
                    return CommunityState.Thriving;
                }
                return CommunityState.Connecting;
            }
            return CommunityState.Growing;
        }
        return CommunityState.Fragmented;

    }

    private int GetDailyCommunityIncrease()
    {
        switch (CurrentState)
        {
            case CommunityState.Growing:
                return 5;
            case CommunityState.Connecting:
                return 10;
            case CommunityState.Thriving:
                return 15;
            default:
                return 0;
        }
    }

    public void IncreaseCommunityLevel(int amount)
    {
        // Handels overflow when trying to increase community level beyond the current cap based on friend count
        AddPointsWithOverflow(amount);
        OnCommunityLevelChanged?.Invoke(CommunityLevel);
        EvaluateCommunityState();
        UpdateCommunityMeter();
        Debug.Log($"Community level increased by {amount}. Current level: {CommunityLevel}, FriendCount: {FriendCount}, CurrentState: {CurrentState}");
    }

    public void DecreaseCommunityLevel(int amount)
    {
        CommunityLevel = Mathf.Max(0, CommunityLevel - amount);
        UpdateCommunityMeter();
        OnCommunityLevelChanged?.Invoke(CommunityLevel);
        EvaluateCommunityState();
    }

    private void EvaluateCommunityState()
    {
        var newState = GetCommunityState();
        // Only trigger state change if it actually changed to avoid unnecessary updates
        if (newState != CurrentState)
        {
            CurrentState = newState;
            OnCommunityStateChanged?.Invoke(CurrentState);
            UpdateCommunityMeter();
            Debug.Log($"Community state changed to: {CurrentState}");
        }
    }

    private void UpdateCommunityMeter()
    {
        if (communityMeter == null) return;

        communityMeter.maxValue = thresholds[thresholds.Length - 1];
        communityMeter.value = CommunityLevel;

        // Update color based on state
        if (communityMeter.fillRect != null)
        {
            Image fillImage = communityMeter.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                switch (CurrentState)
                {
                    case CommunityState.Fragmented:
                        fillImage.color = Color.red;
                        break;
                    case CommunityState.Growing:
                        fillImage.color = Color.yellow;
                        break;
                    case CommunityState.Connecting:
                        fillImage.color = Color.green;
                        break;
                    case CommunityState.Thriving:
                        fillImage.color = Color.purple;
                        // Activates passive points of circular satisfaction meter for reaching the thriving state
                        satisfactionMeter.ActivatePassivePoint();
                        break;
                }
            }
        }
    }

    private void AddPointsWithOverflow(int amount)
    {
        if (amount <= 0) return;

        int currentMax = GetMaxLevelForCurrentFriendCount();
        int spaceAvailable = currentMax - CommunityLevel;

        Debug.Log($"AddPoints: Amount={amount}, Current Level={CommunityLevel}, Max={currentMax}, Space={spaceAvailable}, Overflow={OverflowPoints}");

        if (spaceAvailable >= amount)
        {
            // All points fit within current cap
            CommunityLevel += amount;
            Debug.Log($"Added all {amount} points. New level: {CommunityLevel}");
        }
        else
        {
            // Only add what fits
            if (spaceAvailable > 0)
            {
                CommunityLevel += spaceAvailable;
                amount -= spaceAvailable;
                Debug.Log($"Added {spaceAvailable} points to reach cap. Remaining: {amount}");
            }

            // Store remaining as overflow
            OverflowPoints += amount;
            Debug.Log($"Stored {amount} as overflow. Total overflow: {OverflowPoints}");
        }

        // Clamp to max just in case
        CommunityLevel = Mathf.Min(CommunityLevel, thresholds[thresholds.Length - 1]);
        UpdateCommunityMeter();
    }

    private int GetMaxLevelForCurrentFriendCount()
    {
        switch (FriendCount)
        {
            case 0:
                return thresholds[1]; // 25
            case 1:
                return thresholds[2]; // 50
            case 2:
                return thresholds[3]; // 75
            default:
                return thresholds[4]; // 100
        }
    }

    // Call this when friend count increases to try to apply overflow points
    private void TryApplyOverflow()
    {
        if (OverflowPoints > 0)
        {
            Debug.Log($"Applying overflow points: {OverflowPoints}");
            int overflow = OverflowPoints;
            OverflowPoints = 0;
            IncreaseCommunityLevel(overflow);
        }
    }

    private void OnNewDay(int dayNumber)
    {
        Debug.Log($"New day: {dayNumber}. Checking for daily community increase.{CurrentState}");
        int dailyIncrease = GetDailyCommunityIncrease();
        if (dailyIncrease > 0)
        {
            Debug.Log($"Daily community increase: {dailyIncrease}");
            AddPointsWithOverflow(dailyIncrease);
        }
    }

    public void LoadCommunityState(int level, int overflowPoints)
    {
        CommunityLevel = level;
        OverflowPoints = overflowPoints;
        EvaluateCommunityState();
        UpdateCommunityMeter();
    }

    private void OnDestroy()
    {
        // Clean up subscriptions
        foreach (var companion in allCompanions)
        {
            if (companion != null)
            {
                companion.OnStateChanged -= OnCompanionStateChanged;
            }
        }

        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.OnNewDay -= OnNewDay;
    }
}