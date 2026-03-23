using System;
using UnityEngine;
using UnityEngine.UI;

public class CommunityMeter : MonoBehaviour
{
    [SerializeField] private Slider communityMeter;

    public int CommunityLevel = 0;
    public int OverflowPoints = 0;
    private enum CommunityState { Fragmented, Growing, Connecting, Thriving }
    private event Action<int> OnCommunityLevelChanged;
    private CompanionFriendship friendship;
    private event Action<CommunityState> OnCommunityStateChanged;
    private CommunityState CurrentState;
    public int FriendCount = 0; 

    private readonly int[] thresholds = { 0,25, 50, 75 }; // Example thresholds for each state
    private void Awake()
    {
        friendship = FindObjectOfType<CompanionFriendship>();
        if (friendship != null)
        {
            friendship.OnStateChanged += HandleStateChange;
        }
        CurrentState = GetCommunityState();
        UpdateCommunityMeter();

        if (DayCycleManager.Instance != null)
            DayCycleManager.Instance.OnNewDay += OnNewDay;
    }

    private CommunityState GetCommunityState() 
    {
        if (FriendCount >= 3 && CommunityLevel >= thresholds[3])
            return CommunityState.Thriving;
        else if (FriendCount >= 2 && CommunityLevel >= thresholds[2])
            return CommunityState.Connecting;
        else if (FriendCount >= 1 && CommunityLevel >= thresholds[1])
            return CommunityState.Growing;
        else
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
        AddPointsWithOverflow(amount);
        OnCommunityLevelChanged?.Invoke(CommunityLevel);
        EvaluateCommunityState();
    }

    public void DecreaseCommunityLevel(int amount)
    {
        CommunityLevel = Mathf.Max(0, CommunityLevel - amount);
        UpdateCommunityMeter();
        OnCommunityLevelChanged?.Invoke(CommunityLevel);
        EvaluateCommunityState();
    }

    private void HandleStateChange(CompanionFriendship.FriendshipState newState)
    {
        if (newState == CompanionFriendship.FriendshipState.Friend)
        {
            FriendCount++;
            TryApplyOverflow(); // Try to apply any overflow points when a new friend is added
        }
        else if (newState != CompanionFriendship.FriendshipState.Friend)
        {
            FriendCount = Mathf.Max(0, FriendCount - 1);
        }

        EvaluateCommunityState();
    }

    private void EvaluateCommunityState()
    {
        var newState = GetCommunityState();

        if (newState != CurrentState)
        {
            CurrentState = newState;
            OnCommunityStateChanged?.Invoke(CurrentState);
            UpdateCommunityMeter();
        }
    }

    private void UpdateCommunityMeter()
    {
        if (communityMeter == null) return;
        communityMeter.value = CommunityLevel;
        if (CurrentState == CommunityState.Fragmented)
        {
            communityMeter.fillRect.GetComponent<Image>().color = Color.red;
        }
        else if (CurrentState == CommunityState.Growing)
        {

            communityMeter.fillRect.GetComponent<Image>().color = Color.yellow;
        }
        else if (CurrentState == CommunityState.Connecting)
        {
            communityMeter.fillRect.GetComponent<Image>().color = Color.green;
        }
        else if (CurrentState == CommunityState.Thriving)
        {
            communityMeter.fillRect.GetComponent<Image>().color = Color.purple;
        }
    }

    private void AddPointsWithOverflow(int amount)
    {
        int stage = (int)GetCommunityState();
        int nextThreshold = thresholds[Math.Min(stage + 1, thresholds.Length - 1)];
        int pointsToAdd = amount;

        // Always add points up to the cap for the current stage
        int maxForStage = nextThreshold - CommunityLevel;
        int addNow = Mathf.Min(pointsToAdd, maxForStage);

        CommunityLevel += addNow;
        pointsToAdd -= addNow;

        // If we can't advance to the next stage, store the rest as overflow
        if (!CanAdvanceToNextStage(stage))
        {
            OverflowPoints += pointsToAdd;
        }
        else
        {
            // If we can advance, try to apply the rest recursively
            if (pointsToAdd > 0 && stage < thresholds.Length - 1)
            {
                stage++;
                AddPointsWithOverflow(pointsToAdd);
            }
        }

        UpdateCommunityMeter();
    }

    private bool CanAdvanceToNextStage(int currentStage)
    {
        // Stage 0: Fragmented -> Growing (needs 1 friend)
        // Stage 1: Growing -> Connecting (needs 2 friends)
        // Stage 2: Connecting -> Thriving (needs 3 friends)
        switch (currentStage)
        {
            case 0: return FriendCount >= 1;
            case 1: return FriendCount >= 2;
            case 2: return FriendCount >= 3;
            default: return false;
        }
    }

    // Call this when friend count increases to try to apply overflow points
    private void TryApplyOverflow()
    {
        if (OverflowPoints > 0)
        {
            int overflow = OverflowPoints;
            OverflowPoints = 0;
            IncreaseCommunityLevel(overflow);
        }
    }

    private void OnNewDay(int dayNumber)
    {
        int dailyIncrease = GetDailyCommunityIncrease();
        if (dailyIncrease > 0)
        {
            IncreaseCommunityLevel(dailyIncrease);
        }
    }
}
