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

    public void LoadState(int day, float timer)
    {
        CurrentDay = day;
        DayTimer = timer;
    }

    private void Update()
    {
        DayTimer += Time.deltaTime;
        if (DayTimer >= dayLength)
        {
            DayTimer = 0f;
            CurrentDay++;
            OnNewDay?.Invoke(CurrentDay);
        }
    }
}
