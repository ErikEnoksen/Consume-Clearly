// This script manages the day cycle in the game. It keeps track of the current day and the time elapsed in the current day.
//
// Purpose:
// It updates the day timer and increments the day count when the timer exceeds the defined day length.
// It also provides an event that other parts of the game can subscribe to for when a new day starts.
// 
// Key Features:
// - Singleton pattern for easy access across the game.
// - Configurable day length.
// - Event system to notify other scripts of a new day.
// 
// FLOW:
// Awake() -> Initialize singleton instance
// Update() -> Increment day timer -> Check if day timer exceeds day length -> Reset timer and increment day -> Invoke OnNewDay event
// LoadState() -> Set current day and timer from saved state
// ===============================================================================================================================================================================

using System;
using UnityEngine;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }
    public int CurrentDay { get; private set; } = 1;
    public float DayTimer { get; private set; } = 0f;
    public event Action<int> OnNewDay;

    [Tooltip("How long is a day in seconds?")]
    public float dayLength = 180f;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    // This method can be called to load the day cycle state from a saved game
    public void LoadState(int day, float timer)
    {
        CurrentDay = day;
        DayTimer = timer;
    }

    private void Update()
    {
        // Increment the day timer
        DayTimer += Time.deltaTime;
        if (DayTimer >= dayLength)
        {
            DayTimer = 0f;
            CurrentDay++;
            OnNewDay?.Invoke(CurrentDay);
        }
    }
}
