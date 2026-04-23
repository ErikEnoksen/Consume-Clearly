using System;
using UnityEngine;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance { get; private set; }
    public int CurrentDay { get; private set; } = 1;
    public event Action<int> OnNewDay;

    [Tooltip("How long is a day in seconds?")]
    public float dayLength = 180f; // 3 minutes per day
    private float dayTimer = 0f;

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

    private void Update()
    {
        dayTimer += Time.deltaTime;
        if (dayTimer >= dayLength)
        {
            dayTimer = 0f;
            CurrentDay++;
            OnNewDay?.Invoke(CurrentDay);
        }
    }
}
